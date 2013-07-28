using SimpleSchemaParser;
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

      if (argsList.Contains("-v") || argsList.Contains("-verbose"))
      {
        verbose = true;
        argsList.RemoveAll(s => s == "-v" || s == "-verbose");
      }
      Dictionary<string, string> namespacePrefixes = new Dictionary<string, string>();
      int index;
      while ((index = argsList.IndexOf("-prefix")) != -1)
      {
        argsList.RemoveAt(index);
        if (argsList.Count <= index)
        {
          Usage();
          return;
        }
        var prefix = argsList[index];
        argsList.RemoveAt(index);
        if (argsList.Count <= index)
        {
          Usage();
          return;
        }
        var @namespace = argsList[index];
        argsList.RemoveAt(index);
        namespacePrefixes[@namespace] = prefix;
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
        foreach (var nsPr in namespacePrefixes) {
          serializer.SetPrefix(nsPr.Key, nsPr.Value);
        }
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
