using System.Collections.Generic;
using Blamite.Blam.Scripting;
using Blamite.IO;
using Blamite.Blam.ThirdGen.Structures;

namespace Blamite.Blam.Util
{
    public static class SeatMappingNameExtractor
    {
        /// <summary>
        ///     Extracts all unique unit seat mappings from the script expressions of a scnr based script file.
        /// </summary>
        /// <param name="scnr">The scnr based script file.</param>
        /// <param name="reader">The stream to read from.</param>
        /// <param name="op">A lookup containing script type information.</param>
        /// <returns>All unique unit seat mappings contained in the script expressions.</returns>
        public static IEnumerable<UnitSeatMapping> ExtractScnrSeatMappings(ScnrScriptFile scnr, IReader reader, OpcodeLookup op)
        {
            ScriptTable scripts = scnr.LoadScripts(reader);
            ScriptValueType typeInfo = op.GetTypeInfo("unit_seat_mapping");

            // find all unique mappings
            SortedDictionary<uint, UnitSeatMapping> uniqueMappings = new SortedDictionary<uint, UnitSeatMapping>();

            foreach (var exp in scripts.Expressions)
            {
                if (exp.Opcode == typeInfo.Opcode && exp.ReturnType == typeInfo.Opcode && !exp.Value.IsNull)
                {
                    // Calculate the index and only add it if it doesn't exist yet.
                    uint index = exp.Value.UintValue & 0xFFFF;
                    if (!uniqueMappings.ContainsKey(index))
                    {
                        uint count = (exp.Value.UintValue & 0xFFFF0000) >> 16;
                        string name = exp.StringValue;
                        UnitSeatMapping mapping = new UnitSeatMapping((short)index, (short)count, name);
                        uniqueMappings.Add(index, mapping);
                    }
                }
            }
            return uniqueMappings.Values;
        }

    }
}
