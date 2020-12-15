using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Blamite.Blam;
using Blamite.Blam.Scripting;
using Blamite.Serialization;
using Blamite.Serialization.Settings;
using Blamite.IO;

namespace ScriptTool
{
    public class OpcodeExtractor
    {
        private readonly string _exeDir;
        private readonly string _formatsDir;
        private readonly string _supportedBuildsPath;
        private readonly EngineDatabase _db;

        private enum OpcodeType
        {
            Functions,
            EngineGlobals
        }

        public OpcodeExtractor()
        {
            _exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _formatsDir = Path.Combine(_exeDir, "Formats");
            _supportedBuildsPath = Path.Combine(_formatsDir, "Engines.xml");
            _db = XMLEngineDatabaseLoader.LoadDatabase(_supportedBuildsPath);
        }

        public SortedDictionary<int, string> ExtractFunctionOpcodes(string[] maps)
        {
            SortedDictionary<int, string> result = new SortedDictionary<int, string>();
            HandleMaps(maps, result, OpcodeType.Functions);
            return result;
        }

        public SortedDictionary<int, string> ExtractEngineGlobals(string[] maps)
        {
            SortedDictionary<int, string> result = new SortedDictionary<int, string>();
            HandleMaps(maps, result, OpcodeType.EngineGlobals);
            return result;
        }

        private void HandleMaps(string[] maps, SortedDictionary<int, string> dict, OpcodeType type)
        {
            foreach (string map in maps)
            {
                Console.WriteLine($"Processing {Path.GetFileName(map)}.");
                var scriptTables = MapLoader.LoadAllScriptFiles(map, _db, out _);
                foreach (ScriptTable table in scriptTables.Values)
                {
                    if(type == OpcodeType.Functions)
                    {
                        FunctionOpcodesFromExpressions(table.Expressions, dict);

                    }
                    else if(type == OpcodeType.EngineGlobals)
                    {
                        EngineGlobalsFromExpressions(table.Expressions, dict);
                    }
                }
            }
        }

        private void FunctionOpcodesFromExpressions(ScriptExpressionTable expressions, SortedDictionary<int, string> dict)
        {
            foreach (ScriptExpression exp in expressions.ExpressionsAsReadonly)
            {
                if (exp.Type == ScriptExpressionType.Group)
                {
                    DatumIndex index = new DatumIndex(exp.Value.UintValue);
                    ScriptExpression name = expressions.FindExpression(index);

                    if (exp.Opcode != name.Opcode)
                    {
                        Console.WriteLine($"Warning: Non-matching opcodes! Call Opcode: {exp.Opcode} Function Name Opcode: {name.Opcode}");
                    }

                    if (dict.TryGetValue(name.Opcode, out string funcName) && funcName != name.StringValue)
                    {
                        Console.WriteLine($"Warning: Duplicate opcodes! Opcode: {name.Opcode.ToString("X3")} Name: \"{name.StringValue}\"\n");
                    }
                    else
                    {
                        dict[name.Opcode] = name.StringValue;
                    }
                }
            }
        }

        private void EngineGlobalsFromExpressions(ScriptExpressionTable expressions, SortedDictionary<int, string> dict)
        {
            foreach (ScriptExpression expr in expressions.ExpressionsAsReadonly)
            {
                if (expr.Type == ScriptExpressionType.GlobalsReference)
                {
                    var bytes = BitConverter.GetBytes(expr.Value.UintValue);
                    ushort first16 = BitConverter.ToUInt16(bytes, 2);
                    ushort second16 = (ushort)(BitConverter.ToUInt16(bytes, 0) ^ 0x8000);

                    if(first16 == 0xFFFF)
                    {
                        if (dict.TryGetValue(second16, out string global) && global != expr.StringValue)
                        {
                            Console.WriteLine($"Warning: Duplicate opcodes! Opcode: {second16.ToString("X3")} Name1: \"{global}\" Name2: \"{expr.StringValue}\"\n");
                        }
                        else
                        {
                            dict[second16] = expr.StringValue;
                        }
                    }
                }
            }
        }

    }
}
