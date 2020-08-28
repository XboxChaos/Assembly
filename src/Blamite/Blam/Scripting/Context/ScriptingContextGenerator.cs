using Antlr4.Runtime;
using Blamite.IO;
using Blamite.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Scripting.Context
{
    public static class ScriptingContextGenerator
    {
        public static ScriptingContextCollection GenerateContext(IReader reader, ICacheFile cache, EngineDescription buildinfo)
        {
            var result = new ScriptingContextCollection();
            var scnrTag = cache.Tags.FindTagByGroup("scnr");
            var mdlgTag = cache.Tags.FindTagByGroup("mdlg");
            
            if (scnrTag != null && buildinfo.Layouts.HasLayout("scnr script context"))
            {
                var scnrLayout = buildinfo.Layouts.GetLayout("scnr script context");
                reader.SeekTo(scnrTag.MetaLocation.AsOffset());
                var scnrValues = CacheStructureReader.ReadStructure(reader, cache, buildinfo, scnrLayout);
                FillContextCollection(reader, cache, buildinfo, scnrValues, result);
            }

            if(mdlgTag != null && buildinfo.Layouts.HasLayout("mdlg script context"))
            {
                var mdlgLayout = buildinfo.Layouts.GetLayout("mdlg script context");
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
            if(!block.Value[0].HasStringID("Name") && !block.Value[0].HasString("Name"))
            {
                return true;
            }
            else
            {
                return false;
            }
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
            var result = new ScriptingContextObject(name, index, group, isChildObject);

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
            string lastMappingName = "";
            short firstOccurenceIndex = 0;
            short count = 0;

            for (short mappingIndex = 0; mappingIndex < block.Value.Length; mappingIndex++)
            {
                var values = block.Value[mappingIndex];
                ITag unit = values.GetTagReference("Unit");
                int seatIndeces1 = (int)values.GetInteger("Seats 1");
                int seatIndeces2 = (int)values.GetInteger("Seats 2");

                List<int> seatIndices = GetSeatIndices(seatIndeces1, seatIndeces2);

                var vehiLayout = buildInfo.Layouts.GetLayout("vehi seats");
                reader.SeekTo(unit.MetaLocation.AsOffset());
                var vehiValues = CacheStructureReader.ReadStructure(reader, cache, buildInfo, vehiLayout);
                var seatsBlock = vehiValues.GetTagBlock("seats");

                string mappingName = GetVehicleMappingName(seatsBlock, seatIndices);

                if(mappingName != lastMappingName)
                {
                    // Don't add a unit seat mapping to the collection in the first iteration.
                    if(mappingIndex > 0)
                    {
                        var mappingObject = new UnitSeatMapping(firstOccurenceIndex, count, lastMappingName);
                        collection.AddUnitSeatMapping(mappingObject);
                    }

                    // Reset
                    lastMappingName = mappingName;
                    firstOccurenceIndex = mappingIndex;
                    count = 1;
                }
                else
                {
                    count++;

                    // If the last iteration contained a duplicate seat mapping, add it to the collection.
                    if (mappingIndex == block.Value.Length - 1)
                    {
                        var mappingObject = new UnitSeatMapping(firstOccurenceIndex, count, lastMappingName);
                        collection.AddUnitSeatMapping(mappingObject);
                    }
                }
            }
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
            return new string(result).TrimEnd('_');
        }

    }
}
