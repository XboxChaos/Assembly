using Blamite.Blam.Resources.Sounds;
using Blamite.IO;
using Blamite.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
            MappingInformation[] information = new MappingInformation[block.Value.Length];

            // Collect information for all unti seat mappings in scnr.
            for (short mappingIndex = 0; mappingIndex < block.Value.Length; mappingIndex++)
            {
                var values = block.Value[mappingIndex];
                ITag unit = values.GetTagReference("Unit");
                int seatIndeces1 = (int)values.GetInteger("Seats 1");
                int seatIndeces2 = (int)values.GetInteger("Seats 2");
                List<int> seatIndices = GetSeatIndices(seatIndeces1, seatIndeces2);
                var vehiLayout = buildInfo.Layouts.GetLayout("vehi_seats");

                // Read the vehicle meta data from the cache file.
                reader.SeekTo(unit.MetaLocation.AsOffset());
                var vehiValues = CacheStructureReader.ReadStructure(reader, cache, buildInfo, vehiLayout);
                var seatsBlock = vehiValues.GetTagBlock("seats");

                // Guess the name of each mapping entry in scnr.
                string name = GetVehicleMappingName(seatsBlock, seatIndices);

                // Add the information to the array.
                information[mappingIndex] = new MappingInformation
                {
                    Index = mappingIndex,
                    Name = name,
                    SplitName = name.Split('_'),
                    TagName = cache.FileNames.GetTagName(unit),
                    SeatIndices = seatIndices
                };
            }

            // Identify and create seat mapping groups.
            var mappings = GroupMappingNames(information);

            // Add the final unit seat mapping objects to the result.
            foreach(var mapping in mappings)
            {
                collection.AddUnitSeatMapping(mapping);
            }
        }

        private static IEnumerable<UnitSeatMapping> GroupMappingNames(MappingInformation[] information)
        {
            var queue = new Queue<MappingInformation>(information);
            var result = new List<UnitSeatMapping>();
            while (queue.Count > 0)
            {
                MappingInformation info = queue.Dequeue();
                short index = info.Index;
                string name = info.Name;
                short count = 1;

                var processedInformation = new List<MappingInformation>();
                var processedTags = new List<string>();
                processedInformation.Add(info);
                processedTags.Add(info.TagName);
                while (queue.Count > 0)
                {
                    MappingInformation next = queue.Peek();
                    if(next.TagName == info.TagName || next.SplitName[0] != info.SplitName[0] || next.SplitName[1] != info.SplitName[1] 
                        || processedInformation.Contains(next) || processedTags.Contains(next.TagName))
                    {
                        break;
                    }
                    else
                    {
                        processedTags.Add(next.TagName);
                        processedInformation.Add(queue.Dequeue());
                    }
                }

                if(processedInformation.Count > 1)
                {
                    count = (short)processedInformation.Count;
                    name = ShortenMappingName(processedInformation);
                }

                result.Add(new UnitSeatMapping(index, count, name));
            }

            return result;
        }

        private static string ShortenMappingName(IEnumerable<MappingInformation> mappings)
        {
            var split = mappings.Select(m => m.SplitName).ToArray();
            int maxLength = split.Min(s => s.Length);
            int matchingParts = 2;

            for (int i = 2; i < maxLength; i++)
            {
                var nextPart = split.Select(s => s[i]).ToArray();
                if (nextPart.All(s => s == nextPart[0]))
                {
                    matchingParts++;
                }
                else
                {
                    break;
                }
            }

            return string.Join("_", split[0].Take(matchingParts));
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

        private class MappingInformation
        {
            public short Index { get; set; }
            public string Name { get; set; }
            public string[] SplitName { get; set; }
            public string TagName { get; set; }
            public IEnumerable<int> SeatIndices { get; set; }
        }

    }
}
