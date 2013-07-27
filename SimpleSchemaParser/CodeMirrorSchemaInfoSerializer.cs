using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleSchemaParser
{
  public static class CodeMirrorSchemaInfoSerializer
  {

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

    public static string ToJsonString(IEnumerable<SimpleXmlElement> elements, bool pretty = true)
    {
      using (var buffer = new StringWriter())
      {
        using (var writer = new JsonTextWriter(buffer))
        {
          writer.Formatting = pretty ? Formatting.Indented : Formatting.None;
          writer.WriteStartObject();
          WriteTopElements(writer, elements.Where(e => e.IsTopLevelElement));
          foreach (var element in elements)
          {
            WriteElement(writer, element);
          }
          writer.WriteEndObject();
        }
        return buffer.ToString();
      }
    }

    private static void WriteTopElements(JsonTextWriter writer, IEnumerable<SimpleXmlElement> elements)
    {
      if (!elements.Any()) return;
      writer.WritePropertyName("!top");
      writer.WriteStartArray();
      foreach (var element in elements)
      {
        writer.WriteValue(element.Name);
      }
      writer.WriteEndArray();
    }

    private static void WriteElement(JsonTextWriter writer, SimpleXmlElement element)
    {
      writer.WritePropertyName(element.Name);
      writer.WriteStartObject();
      if (element.Attributes != null && element.Attributes.Any())
      {
        writer.WritePropertyName("attrs");
        writer.WriteStartObject();
        foreach (var attribute in element.Attributes) {
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
          writer.WriteValue(child);
        }
        writer.WriteEndArray();
      }
      writer.WriteEndObject();

    }

  }
}
