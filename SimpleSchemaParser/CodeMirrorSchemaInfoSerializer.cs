using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleSchemaParser
{
  public class CodeMirrorSchemaInfoSerializer
  {
    private readonly IEnumerable<SimpleXmlElement> elements;
    private JsonTextWriter writer;
    public bool Pretty { get; set; }
    
    /*
     * {
        "!top": ["top"],
        top: {
          attrs: {
            lang: ["en", "de", "fr", "nl"],
            freeform: null
          },
          children: ["animal", "plant"]
        },
        animal: {
          attrs: {
            name: null,
            isduck: ["yes", "no"]
          },
          children: ["wings", "feet", "body", "head", "tail"]
        },
        plant: {
          attrs: {name: null},
          children: ["leaves", "stem", "flowers"]
        },
        wings: dummy, feet: dummy, body: dummy, head: dummy, tail: dummy,
        leaves: dummy, stem: dummy, flowers: dummy
      }
     */

    public CodeMirrorSchemaInfoSerializer(IEnumerable<SimpleXmlElement> elements)
    {
      this.elements = elements;
    }

    public void SetPrefix(string @namespace, string prefix)
    {
      NamespacePrefixes[@namespace] = prefix;
    }

    private Dictionary<string, string> NamespacePrefixes = new Dictionary<string, string>();

    public string ToJsonString()
    {
      using (var buffer = new StringWriter())
      {
        using (writer = new JsonTextWriter(buffer))
        {
          writer.Formatting = Pretty ? Formatting.Indented : Formatting.None;
          writer.WriteStartObject();
          WriteTopElements(elements.Where(e => e.IsTopLevelElement));
          foreach (var element in elements)
          {
            WriteElement(element);
          }
          writer.WriteEndObject();
        }
        return buffer.ToString();
      }
    }

    private string ToElementName(SimpleXmlElement element)
    {
      if (string.IsNullOrEmpty(element.Namespace))
        return element.Name;
      return string.Format("{0}:{1}", GetPrefix(element.Namespace), element.Name);
    }

    private string ToElementName(SimpleXmlElementRef elementRef)
    {
      if (string.IsNullOrEmpty(elementRef.Namespace))
        return elementRef.Name;
      return string.Format("{0}:{1}", GetPrefix(elementRef.Namespace), elementRef.Name);
    }

    private int nsCounter = 0;
    private string GetPrefix(string ns)
    {
      string prefix;
      if (!NamespacePrefixes.TryGetValue(ns, out prefix))
      {
        prefix = "cmns" + nsCounter;
        nsCounter++;
        NamespacePrefixes.Add(ns, prefix);
      }

      return prefix;
    }

    private void WriteTopElements(IEnumerable<SimpleXmlElement> elements)
    {
      if (!elements.Any()) return;
      writer.WritePropertyName("!top");
      writer.WriteStartArray();
      foreach (var element in elements)
      {
        writer.WriteValue(ToElementName(element));
      }
      writer.WriteEndArray();
    }

    private void WriteElement(SimpleXmlElement element)
    {
      writer.WritePropertyName(ToElementName(element));
      writer.WriteStartObject();
      if (element.Attributes != null && element.Attributes.Any())
      {
        writer.WritePropertyName("attrs");
        writer.WriteStartObject();
        foreach (var attribute in element.Attributes)
        {
          writer.WritePropertyName(attribute.Name);
          if (attribute.PossibleValues == null || !attribute.PossibleValues.Any())
          {
            writer.WriteNull();
          }
          else
          {
            writer.WriteStartArray();
            foreach (var value in attribute.PossibleValues)
            {
              writer.WriteValue(value);
            }
            writer.WriteEndArray();
          }
        }
        writer.WriteEndObject();
      }

      if (element.Children != null && element.Children.Any())
      {
        writer.WritePropertyName("children");
        writer.WriteStartArray();
        foreach (var child in element.Children)
        {
          writer.WriteValue(ToElementName(child));
        }
        writer.WriteEndArray();
      }
      writer.WriteEndObject();
    }

  }
}
