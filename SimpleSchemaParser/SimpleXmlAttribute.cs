using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSchemaParser
{
  public class SimpleXmlAttribute
  {
    public string Namespace { get; set; }
    public string Name { get; set; }
    public IEnumerable<string> PossibleValues { get; set; }

  }
}
