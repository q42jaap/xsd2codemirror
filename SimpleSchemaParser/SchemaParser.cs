using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Xml.Schema;
using System.IO;
using System.Collections;
using System.Xml;

namespace SimpleSchemaParser
{

  /// <summary>
  /// The SchemaParser can parse a schema into simple element definitions. This class
  /// uses <see cref="System.Xml.Schema.XmlSchemaSet"/> to iterate over all the toplevel element
  /// and recursively parses them.
  /// 
  /// The parser doesn't support xml elements occuring in multiple contexts. The first occurence of the
  /// element will be output, other occurences will be ignored.
  /// </summary>
  public class SchemaParser
  {
    private XmlSchemaSet schemaSet;

    private string schemaPath;

    private ILogger log = NullLogger.Instance;
    public ILogger Logger { get { return log; } set { log = value ?? NullLogger.Instance; } }

    public string TargetNamespace { get; set; }

    public SchemaParser(string schemaPath)
    {
      this.schemaPath = schemaPath;
    }

    /// <summary>
    /// Compiles the schema. Includes are assumed to be relative to the schemaPath provided. If they cannot be read,
    /// an exception will only be thrown if an element from the included resource is used.
    /// A missing file itself is ignored by the XmlSchemaSet class.
    /// </summary>
    public void Compile()
    {
      try
      {
        schemaSet = new XmlSchemaSet();
        FileInfo xsdPath = new FileInfo(schemaPath);
        schemaSet.Add(TargetNamespace, new Uri(xsdPath.FullName).LocalPath);
        log.WriteLine("Schema read...");
        schemaSet.Compile();
        log.WriteLine("Schema compiled...");
      }
      catch (Exception e)
      {
        log.WriteLine("Could not compile schema: {0}: {1}", e.GetType().Name, e.Message);
        throw new InvalidOperationException(string.Format("Could not compile schema: {0}: {1}", e.GetType().Name, e.Message), e);
      }
    }

    public IEnumerable<SimpleXmlElement> GetXmlElements()
    {
      if (schemaSet == null)
        throw new InvalidOperationException("Schema is not compiled yet.");

      foreach (XmlSchemaElement particle in schemaSet.GlobalElements.Values)
      {
        ParseElement((XmlSchemaElement)particle, true);
      }

      List<RecursiveChildren> allDependencies = null;
      for (var i = 0; i < 100; i++)
      {
        allDependencies = elements.Values.Select(temp => temp.children).Distinct().Where(ch => ch != null && ch.dependencies.Count > 0).OrderBy(ch => ch.dependencies.Count).ToList();

        if (!allDependencies.Any())
          break;

        foreach (var ch in allDependencies)
        {
          foreach (var dep in ch.dependencies.ToList())
          {
            if (dep.Value.NoDependenciesLeft)
            {
              ch.children = ch.children.Concat(dep.Value.children).Distinct().ToList();
              ch.dependencies.Remove(dep.Key);
            }
          }
        }
      }
      if (allDependencies.Any())
      {
        throw new InvalidOperationException("There is a cycle in the schema, can't figure it out: " + string.Join(", ", allDependencies.Select(dep => GetParticleDesc(dep.group)).ToArray()));
      }

      foreach (var temp in elements)
      {
        if (temp.Value.children != null)
          temp.Value.element.Children = temp.Value.children.children.Select(qn => new SimpleXmlElementRef { Name = qn.Name, Namespace = qn.Namespace }).ToList();
      }

      return elements.Values.Select(temp => temp.element);
    }

    private class TempXmlElement
    {
      public SimpleXmlElement element;
      public RecursiveChildren children;
    }

    private Dictionary<XmlSchemaElement, TempXmlElement> elements = new Dictionary<XmlSchemaElement, TempXmlElement>();

    XmlQualifiedName ParseElement(XmlSchemaElement element, bool isTopLevel = false)
    {
      log.WriteLine("Found element {0}", GetParticleDesc(element));

      if (element.RefName != null && !element.RefName.IsEmpty)
      {
        return element.RefName;
      }

      TempXmlElement tempXmlElement;
      if (elements.TryGetValue(element, out tempXmlElement))
      {
        // TODO detect real equals or conflict, if conflict merge
        return element.QualifiedName;
      }

      tempXmlElement = new TempXmlElement();
      tempXmlElement.element = new SimpleXmlElement();
      tempXmlElement.element.Name = element.QualifiedName.Name;
      tempXmlElement.element.Namespace = element.QualifiedName.Namespace;
      tempXmlElement.element.IsTopLevelElement = isTopLevel;

      elements.Add(element, tempXmlElement);

      List<SimpleXmlAttribute> attributes = new List<SimpleXmlAttribute>();

      // if the element is a simple type, it cannot have attributes of children, thus ignore it
      // if the element is a complex type, we'll parse it.
      if (element.ElementSchemaType is XmlSchemaComplexType)
      {
        XmlSchemaComplexType type = (XmlSchemaComplexType)element.ElementSchemaType;
        using (log.Indent())
        {
          log.WriteLine("Attributes");
          using (log.Indent())
          {
            foreach (XmlSchemaAttribute attribute in type.AttributeUses.Values)
            {
              attributes.Add(ParseAttribute(attribute));
              log.WriteLine("{0}", attribute.QualifiedName.Name);
            }
          }

          XmlSchemaParticle particle = type.ContentTypeParticle;
          if (particle != null)
          {
            log.WriteLine("Child Particle {0}", GetParticleDesc(particle));
            using (log.Indent())
            {
              if (particle is XmlSchemaGroupRef)
              {
                tempXmlElement.children = ParseGoupRef((XmlSchemaGroupRef)particle);
              }
              else if (particle is XmlSchemaGroupBase)
              {
                tempXmlElement.children = ParseGroupBase((XmlSchemaGroupBase)particle);
              }
              else if (particle.GetType().Name == "EmptyParticle")
              {
              }
              else
              {
                throw new NotImplementedException(particle.GetType().Name);
              }
            }
          }
        }
      }

      tempXmlElement.element.Attributes = attributes;

      // TODO merge attrs and children on conflicts

      return element.QualifiedName;

    }

    private SimpleXmlAttribute ParseAttribute(XmlSchemaAttribute attribute)
    {
      var simpleAttribute = new SimpleXmlAttribute();
      simpleAttribute.Name = attribute.QualifiedName.Name;
      simpleAttribute.Namespace = attribute.QualifiedName.Namespace;

      var type = attribute.AttributeSchemaType;
      var possibleValues = new List<string>();
      if (type != null && type.Content is XmlSchemaSimpleTypeRestriction)
      {
        var restriction = (XmlSchemaSimpleTypeRestriction)type.Content;
        foreach (var facet in restriction.Facets)
        {
          if (facet is XmlSchemaEnumerationFacet)
          {
            possibleValues.Add(((XmlSchemaEnumerationFacet)facet).Value);
          }
        }
      }
      if (possibleValues.Count > 0)
        simpleAttribute.PossibleValues = possibleValues;
      return simpleAttribute;
    }

    private string GetParticleDesc(XmlSchemaParticle particle)
    {
      var desc = particle.GetType().Name.Replace("XmlSchema", "");
      if (particle is XmlSchemaElement)
      {
        desc += "(" + ((XmlSchemaElement)particle).QualifiedName + ")";
      }
      if (particle.SourceUri == null)
      {
        if (particle.Id != null)
          return string.Format("{0}:id:{1}", desc, particle.Id);
        return string.Format("{0}:{1}:{2}", desc, particle.LineNumber, particle.LinePosition);
      }
      else
      {
        string[] segments = new Uri(particle.SourceUri).Segments;
        return string.Format("{0}:{1}:{2}:{3}", segments[segments.Length - 1], desc, particle.LineNumber, particle.LinePosition);
      }
    }

    /// <summary>
    /// Parses xs:group ref="..." elements in the schema
    /// </summary>
    /// <param name="groupRef"></param>
    /// <returns></returns>
    private RecursiveChildren ParseGoupRef(XmlSchemaGroupRef groupRef)
    {
      log.WriteLine("Parsing groupRef {0}", groupRef.RefName);
      using (log.Indent())
      {
        return ParseGroupBase(groupRef.Particle);
      }
    }

    private Dictionary<XmlSchemaGroupBase, RecursiveChildren> groupCache = new Dictionary<XmlSchemaGroupBase, RecursiveChildren>();

    private class RecursiveChildren
    {
      public readonly XmlSchemaGroupBase group;
      public RecursiveChildren(XmlSchemaGroupBase group)
      {
        this.group = group;
      }

      public Dictionary<XmlSchemaGroupBase, RecursiveChildren> dependencies = new Dictionary<XmlSchemaGroupBase, RecursiveChildren>();
      public List<XmlQualifiedName> children = new List<XmlQualifiedName>();
      public bool NoDependenciesLeft { get { return dependencies.Count == 0; } }
    }

    /// <summary>
    /// Parses xs:sequence and xs:choice elements in the schema
    /// </summary>
    /// <param name="group"></param>
    /// <returns>A list of direct children elements references</returns>
    private RecursiveChildren ParseGroupBase(XmlSchemaGroupBase group)
    {
      RecursiveChildren result;
      if (groupCache.TryGetValue(group, out result))
      {
        log.WriteLine("Used cache: {0}", GetParticleDesc(group));
        return result;
      }

      log.WriteLine("Parsing group {0}", GetParticleDesc(group));
      result = new RecursiveChildren(group);
      groupCache.Add(group, result);
      foreach (XmlSchemaParticle particle in group.Items)
      {
        using (log.Indent())
        {
          if (particle is XmlSchemaGroupBase)
          {
            XmlSchemaGroupBase subGroup = (XmlSchemaGroupBase)particle;
            result.dependencies.Add(subGroup, ParseGroupBase(subGroup));
          }
          else if (particle is XmlSchemaElement)
          {
            result.children.Add(ParseElement((XmlSchemaElement)particle));
          }
          else if (particle is XmlSchemaAny)
          {
          }
          else
          {
            throw new NotImplementedException(particle.GetType().Name);
          }
        }
      }
      return result;
    }
  }
}
