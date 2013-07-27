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
      return elements.Values;
    }

    private Dictionary<string, SimpleXmlElement> elements = new Dictionary<string, SimpleXmlElement>();

    string ParseElement(XmlSchemaElement element, bool isTopLevel = false)
    {
      log.WriteLine("Found element {0}", element.QualifiedName);

      if (element.RefName != null && !element.RefName.IsEmpty)
      {
        return element.RefName.ToString();
      }

      var elementRef = element.QualifiedName.ToString();
      SimpleXmlElement simpleXmlElement;
      if (elements.TryGetValue(elementRef, out simpleXmlElement))
      {
        // TODO detect real equals or conflict, if conflict merge
        return elementRef;
      }

      simpleXmlElement = new SimpleXmlElement();
      simpleXmlElement.Name = element.QualifiedName.Name;
      simpleXmlElement.Namespace = element.QualifiedName.Namespace;
      simpleXmlElement.IsTopLevelElement = isTopLevel;

      List<string> children = new List<string>();
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
                children.AddRange(ParseGoupRef((XmlSchemaGroupRef)particle));
              }
              else if (particle is XmlSchemaGroupBase)
              {
                children.AddRange(ParseGroupBase((XmlSchemaGroupBase)particle));
              }
            }
          }
        }
      }

      simpleXmlElement.Children = children;
      simpleXmlElement.Attributes = attributes;

      // TODO merge attrs and children on conflicts
      elements.Add(elementRef, simpleXmlElement);

      return elementRef;

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
    private List<string> ParseGoupRef(XmlSchemaGroupRef groupRef)
    {
      log.WriteLine("Parsing groupRef {0}", groupRef.RefName);
      using (log.Indent())
      {
        return ParseGroupBase(groupRef.Particle);
      }
    }

    /// <summary>
    /// Parses xs:sequence and xs:choice elements in the schema
    /// </summary>
    /// <param name="group"></param>
    /// <returns>A list of direct children elements references</returns>
    private List<string> ParseGroupBase(XmlSchemaGroupBase group)
    {
      log.WriteLine("Parsing group ({0})", group.GetType().Name.Replace("XmlSchema", ""));

      var elementRefs = new List<string>();
      foreach (XmlSchemaParticle particle in group.Items)
      {
        using (log.Indent())
        {
          if (particle is XmlSchemaGroupBase)
            elementRefs.AddRange(ParseGroupBase((XmlSchemaGroupBase)particle));
          else if (particle is XmlSchemaElement)
            elementRefs.Add(ParseElement((XmlSchemaElement)particle));
        }
      }
      return elementRefs;
    }
  }
}
