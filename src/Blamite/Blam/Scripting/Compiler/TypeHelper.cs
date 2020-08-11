using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler
{
    public static class TypeHelper
    {
        // When making changes to these, also adjust the strings in the function DetermineReturnTypeOpcode in ScriptCompiler.cs.
        private const string _globalReferenceID = "GLOBALREFERENCE";
        private const string _scriptReferenceID = "SCRIPTREFERENCE";
        private const string _anyID = "ANY";
        private const string _numberID = "NUMBER";
        private static readonly IReadOnlyList<string> _numTypes = new ReadOnlyCollection<string>(new List<string>() { "long", "short", "real" });

        public static bool CanBeCasted(string from, string to, OpcodeLookup op)
        {
            if ((IsNumType(from) && IsNumType(to)) || (op.GetTypeInfo(from).Object && op.GetTypeInfo(to).Object))
            {
                return true;
            }

            // Check if this type supports casting
            CastInfo info = op.GetTypeCast(to);
            if (info != null)
            {
                List<string> casts = new List<string>();
                List<string> processedTypes = new List<string>();
                int addedTypes = info.From.Count;
                casts.AddRange(info.From);

                // Generate a list of all possible casts.
                while (addedTypes > 0)
                {
                    int added = 0;
                    string[] difference = casts.Except(processedTypes).ToArray();
                    foreach (string cast in difference)
                    {
                        info = op.GetTypeCast(cast);
                        if (info != null)
                        {
                            foreach (var type in info.From)
                            {
                                if (!casts.Contains(type))
                                {
                                    casts.Add(type);
                                    added++;
                                }
                            }
                        }
                        processedTypes.Add(cast);
                    }
                    addedTypes = added;
                }
                // Check if this generated list contains this cast.
                return casts.Contains(from);
            }
            else
            {
                return false;
            }
        }

        public static string GlobalsReference { get { return _globalReferenceID; } }

        public static string ScriptReference { get { return _scriptReferenceID; } }

        public static string Any { get { return _anyID; } }

        public static string Number { get { return _numberID; } }

        public static bool IsSpecialType(string type)
        {
            return type == _globalReferenceID || type == _anyID || type == _numberID;
        }

        public static bool IsNumType(string type)
        {
            return _numTypes.Contains(type);
        }
    }
}
