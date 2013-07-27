using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSchemaParser
{
  public interface ILogger
  {
    ILogger Write(string p, params object[] args);
    ILogger WriteLine(string p, params object[] args);
    ILogger WriteLine();
    IDisposable Indent();
  }

}
