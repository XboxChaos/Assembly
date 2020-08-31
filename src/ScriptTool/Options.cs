using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace ScriptTool
{
    [Verb("dump", HelpText = "Dump scripting data.")]
    class DumpOptions
    {
        [Option('i', "input", Required = true, HelpText = "Path to a folder containing .map-files.")]
        public string MapFolder { get; set; }

        [Option('e', "expressions", Group = "ScriptObject",
            HelpText = "Dump expressions. \"ALL\" will dump all Expressions contained in the maps. Only Expressions of a specific value type will be dumped if one is specified. A list of valid types can be found in the _scripting.xml files in the formats folder.")]
        public string DumpExpressions { get; set; }

        //[Option("scripts", Group = "ScriptObject", Default = (bool)false, HelpText = "Dump script definitions.")]
        //public bool DumpScripts { get; set; }

        //[Option("globals", Group = "ScriptObject", Default = (bool)false, HelpText = "Dump map global definitions.")]
        //public bool DumpGlobals { get; set; }

        //[Option("strings", Group = "ScriptObject", Default = (bool)false, HelpText = "Dump script strings.")]
        //public bool DumpStrings { get; set; }
        [Option("stringids", Group = "ScriptObject", Default = (bool)false, HelpText = "Dump StringIDs.")]
        public bool DumpStringIDs { get; set; }

        [Option("unitseatmappings", Group = "ScriptObject", Default = (bool)false, HelpText = "Dump StringIDs.")]
        public bool DumpUnitSeatMappings { get; set; }

        [Option('o', "output", HelpText = "Path to a folder where the data will be dumped to.")]
        public string OutputFolder { get; set; }
    }

    [Verb("functions", HelpText = "Extract function opcodes.")]
    class FunctionOptions
    {
        [Option('i', "input", Required = true, HelpText = "Path to a folder containing .map-files.")]
        public string MapFolder { get; set; }

        [Option('o', "output", HelpText = "Path to a folder where the opcodes will be saved to.")]
        public string OutputFolder { get; set; }

        [Option('u', "update", HelpText = "Xml file to be updated with the extracted opcodes.")]
        public string UpdateFile { get; set; }
    }

    [Verb("engineglobals", HelpText = "Extract engine global opcodes.")]
    class EngineGlobalsOptions
    {
        [Option('i', "input", Required = true, HelpText = "Path to a folder containing .map-files.")]
        public string MapFolder { get; set; }

        [Option('o', "output", HelpText = "Path to a folder where the opcodes will be saved to.")]
        public string OutputFolder { get; set; }

        [Option('u', "update", HelpText = "Xml file to be updated with the extracted opcodes.")]
        public string UpdateFile { get; set; }
    }
}
