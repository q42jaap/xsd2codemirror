using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSchemaParser
{
  public class NullLogger : ILogger
  {
    private static IDisposable outdenter = new NullOutdenter();
    public static readonly NullLogger Instance = new NullLogger();

    private NullLogger()
    {
    }
    private class NullOutdenter : IDisposable
    {
      public void Dispose() { }
    }

    public ILogger Write(string p, params object[] args)
    {
      return this;
    }

    public ILogger WriteLine(string p, params object[] args)
    {
      return this;
    }

    public ILogger WriteLine()
    {
      return this;
    }

    public IDisposable Indent()
    {
      return outdenter;
    }
  }
}
