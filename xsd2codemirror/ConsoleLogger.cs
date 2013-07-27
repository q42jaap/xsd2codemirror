using SimpleSchemaParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace xsd2codemirror
{
  internal class ConsoleLogger : ILogger
  {
    private class ConsoleLoggerOutdenter : IDisposable
    {
      private readonly ConsoleLogger log;
      internal ConsoleLoggerOutdenter(ConsoleLogger log)
      {
        this.log = log;
      }

      void IDisposable.Dispose()
      {
        log.Outdent();
      }
    }

    public ConsoleLogger()
    {
      this.outdenter = new ConsoleLoggerOutdenter(this);
    }

    private string indent = "";
    private bool endOfLine = true;
    private readonly IDisposable outdenter;

    private ILogger WriteIndent()
    {
      Console.Write(indent);
      endOfLine = false;
      return this;
    }

    public ILogger Write(string p, params object[] args)
    {
      if (endOfLine)
        WriteIndent();
      Console.Write(p, args);
      return this;
    }

    public ILogger WriteLine(string p, params object[] args)
    {
      Write(p, args);
      WriteLine();
      return this;
    }

    public ILogger WriteLine()
    {
      Console.WriteLine();
      endOfLine = true;
      return this;
    }

    public IDisposable Indent()
    {
      indent += "  ";
      return outdenter;
    }

    private void Outdent()
    {
      indent = indent.Substring(0, indent.Length - 2);
    }
  }

}
