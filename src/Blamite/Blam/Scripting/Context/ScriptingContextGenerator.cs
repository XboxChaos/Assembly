using Blamite.IO;
using Blamite.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Blamite.Blam.Scripting.Context
{
    public static class ScriptingContextGenerator
    {
        public static ScriptingContextCollection GenerateContext(IReader reader, ICacheFile cache, EngineDescription buildinfo)
        {
            var result = new ScriptingContextCollection();
            var scnrTag = cache.Tags.FindTagByGroup("scnr");
            var mdlgTag = cache.Tags.FindTagByGroup("mdlg");
            
            if (scnrTag != null && buildinfo.Layouts.HasLayout("scnr_script_context"))
            {
                var scnrLayout = buildinfo.Layouts.GetLayout("scnr_script_context");
                reader.SeekTo(scnrTag.MetaLocation.AsOffset());
                var scnrValues = CacheStructureReader.ReadStructure(reader, cache, buildinfo, scnrLayout);
                FillContextCollection(reader, cache, buildinfo, scnrValues, result);
            }

            if(mdlgTag != null && buildinfo.Layouts.HasLayout("mdlg_script_context"))
            {
                var mdlgLayout = buildinfo.Layouts.GetLayout("mdlg_script_context");
                reader.SeekTo(mdlgTag.MetaLocation.AsOffset());
                var mdlgValues = CacheStructureReader.ReadStructure(reader, cache, buildinfo, mdlgLayout);
                FillContextCollection(reader, cache, buildinfo, mdlgValues, result);
            }

            return result;
        }

        private static void FillContextCollection(IReader reader, ICacheFile cache, EngineDescription buildinfo, StructureValueCollection values, ScriptingContextCollection collection)
        {
            var tagBlocks = values.GetTagBlocks();

            foreach(var block in tagBlocks)
            {
                if(block.Value.Length == 0)
                {
                    continue;
                }

                if(block.Key == "unit_seat_mapping")
                {
                    HandleUnitSeatMappings(reader, cache, buildinfo, block, collection);
                }
                else if(IsWrapperBlock(block))
                {
                    HandleWrapperBlock(block, collection);
                }
                else
                {
                    collection.AddObjectGroup(CreateContextBlock(block, false));
                }
            }
        }

        private static bool IsWrapperBlock(KeyValuePair<string, StructureValueCollection[]> block)
        {
            return block.Value[0].HasStringID("Name") || block.Value[0].HasString("Name") ? false : true;
        }

        private static void HandleWrapperBlock(KeyValuePair<string, StructureValueCollection[]> block, ScriptingContextCollection collection)
        {
            if (block.Value.Length != 1)
            {
                throw new ArgumentException($"The wrapper context block {block.Key} doesn't have exactly one element.");
            }

            var innerBlocks = block.Value[0].GetTagBlocks();

            foreach (var inner in innerBlocks)
            {
                collection.AddObjectGroup(CreateContextBlock(inner, false));
            }
        }

        private static ScriptingContextBlock CreateContextBlock(KeyValuePair<string, StructureValueCollection[]> block, bool isChildBlock)
        {
            IEnumerable<ScriptingContextObject> objects = block.Value.Select((values, index) => CreateContextObject(index, block.Key, isChildBlock, values));
            return new ScriptingContextBlock(block.Key, objects);
        }

        private static ScriptingContextObject CreateContextObject(int index, string group, bool isChildObject, StructureValueCollection values)
        {
            string name = values.HasStringID("Name") ? values.GetStringID("Name") : values.GetString("Name");
            var result = new ScriptingContextObject(name.ToLowerInvariant(), index, group, isChildObject);

            var childBlocks = values.GetTagBlocks();
            if(childBlocks.Length > 0)
            {
                foreach(var block in childBlocks)
                {
                    result.AddChildBlock(CreateContextBlock(block, true));
                }
            }

            return result;
        }

        private static void HandleUnitSeatMappings(IReader reader, ICacheFile cache, EngineDescription buildInfo, 
            KeyValuePair<string, StructureValueCollection[]> block, ScriptingContextCollection collection)
        {
            string[] mappingNames = new string[block.Value.Length];

            for (short mappingIndex = 0; mappingIndex < block.Value.Length; mappingIndex++)
            {
                var values = block.Value[mappingIndex];
                ITag unit = values.GetTagReference("Unit");
                int seatIndeces1 = (int)values.GetInteger("Seats 1");
                int seatIndeces2 = (int)values.GetInteger("Seats 2");

                List<int> seatIndices = GetSeatIndices(seatIndeces1, seatIndeces2);

                var vehiLayout = buildInfo.Layouts.GetLayout("vehi_seats");
                reader.SeekTo(unit.MetaLocation.AsOffset());
                var vehiValues = CacheStructureReader.ReadStructure(reader, cache, buildInfo, vehiLayout);
                var seatsBlock = vehiValues.GetTagBlock("seats");

                mappingNames[mappingIndex] = GetVehicleMappingName(seatsBlock, seatIndices);
            }

            var mappings = GroupMappingNames(mappingNames);

            foreach(var mapping in mappings)
            {
                collection.AddUnitSeatMapping(mapping);
            }
        }

        private static IEnumerable<UnitSeatMapping> GroupMappingNames(string[] names)
        {
            var result = new List<UnitSeatMapping>();
            result.Add( new UnitSeatMapping(0, 0, names[0]));

            for(short i = 0; i < names.Length; i++)
            {
                // Shorten the name, if such a seat mapping group has already been created.
                string currentName = names[i];
                if(result.Last().Name != currentName && result.Any(u => u.Name == currentName))
                {
                    currentName = ShortenMappingName(currentName);
                }

                // Increase the count if adjacent mappings have the same name. Otherwise add a new seat mapping.
                var lastMapping = result.Last();
                if(lastMapping.Name == currentName)
                {
                    result[result.Count - 1] = new UnitSeatMapping(lastMapping.Index, (short)(lastMapping.Count + 1), lastMapping.Name);
                }
                else
                {
                    result.Add(new UnitSeatMapping(i, 1, currentName));
                }
            }

            return result;
        }

        private static string ShortenMappingName(string name)
        {
            string[] split = name.Split('_');
            string[] shortened = split.Take(split.Length - 1).ToArray();
            return string.Join("_", shortened);
        }

        private static List<int> GetSeatIndices(int seats1, int seats2)
        {
            var bits = new BitArray(new int[] { seats1, seats2 });
            List<int> result = new List<int>();

            for (int seatIndex = 0; seatIndex < bits.Length; seatIndex++)
            {
                if (bits[seatIndex])
                {
                    result.Add(seatIndex);
                }
            }

            return result;
        }

        private static string GetVehicleMappingName(StructureValueCollection[] seats, IEnumerable<int> indeces)
        {
            List<string> names = new List<string>();

            // Retrieve the names of all included seats.
            foreach(int index in indeces)
            {
                string seatName = seats[index].GetStringID("Name");
                names.Add(seatName);
            }

            // Get the longest common prefix. This will be the mapping name.
            var result = names.Min().TakeWhile((c, i) => names.All(s => s[i] == c)).ToArray();
            return new string(result).TrimEnd('_', '0');
        }

    }
}
