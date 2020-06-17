using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler
{
    public static class Casting
    {
        private static readonly List<string> _numTypes = new List<string> { "real", "short", "long" };

        private static readonly List<string> _flexTypes = new List<string>() { "ANY", "NUMBER", "GLOBALREFERENCE" };

        public static bool CanBeCasted(string from, string to, OpcodeLookup op)
        {
            if ((IsNumType(from) && IsNumType(to)) || (op.GetTypeInfo(from).Object && op.GetTypeInfo(to).Object))
            {
                return true;
            }

            // check if this type supports casting
            CastInfo info = op.GetTypeCast(to);
            if (info != null)
            {
                List<string> casts = new List<string>();
                List<string> processedTypes = new List<string>();
                int addedTypes = info.From.Count;
                casts.AddRange(info.From);

                // generate a list of all possible casts.
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
                return casts.Contains(from);
            }
            else
            {
                return false;
            }
        }

        public static bool IsNumType(string type)
        {
            return _numTypes.Contains(type);
        }

        public static bool IsFlexType(string type)
        {
            return _flexTypes.Contains(type);
        }
    }
}
