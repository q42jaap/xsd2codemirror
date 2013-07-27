using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSchemaParser
{
  public class SimpleXmlElement
  {
    public string Namespace { get; set; }
    public string Name { get; set; }
    public IEnumerable<SimpleXmlAttribute> Attributes { get; set; }
    public IEnumerable<string> Children { get; set; }
    public bool IsTopLevelElement { get; set; }

  }
}
