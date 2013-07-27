﻿using SimpleSchemaParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace xsd2codemirror
{
  public static class Program
  {
    public static void Usage()
    {
      Console.WriteLine("Usage:");
      Console.WriteLine("xsd2codemirror.exe [-v] path-to-xsd");
    }

    public static void Main(string[] args)
    {
      List<string> argsList = new List<string>(args);
      bool verbose = false;

      if (argsList.Contains("-v"))
      {
        verbose = true;
        argsList.RemoveAll(s => s == "-v");
      }
      if (argsList.Count != 1)
      {
        Usage();
        return;
      }

      try
      {
        var parser = new SchemaParser(argsList[0]);
        if (verbose)
          parser.Logger = new ConsoleLogger();
        parser.Compile();
        var elements = parser.GetXmlElements();
        var serializer = new CodeMirrorSchemaInfoSerializer(elements);
        serializer.Pretty = true;
        var json = serializer.ToJsonString();
        Console.WriteLine(json);
      }
      catch (Exception e)
      {
        Console.Error.WriteLine(e.GetType().Name);
        Console.Error.WriteLine(e.Message);
        Console.Error.WriteLine(e.StackTrace);
        System.Environment.Exit(1);
      }
    }
  }
}
