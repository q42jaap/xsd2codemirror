xsd2codemirror
==============

A command line tool to convert an xsd schema to CodeMirror's schemaInfo.

See http://codemirror.net/demo/xmlcomplete.html for an example how to
configure CodeMirror to use a simple schemaInfo for Ctrl-Space completion.


Usage
=====
    Usage:
    xsd2codemirror.exe [-v] path-to-xsd

ProTip™
Pipe the output into a file
    xsd2codemirror C:\...\ > tags.json

Integration
===========
You can use the SimpleSchemaParser library to integrate this into other software.
