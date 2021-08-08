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
        /// <summary>
        /// Generates the scripting context for a cache file.
        /// </summary>
        /// <param name="reader">The stream to read from.</param>
        /// <param name="cache">The cache filr.</param>
        /// <param name="buildinfo">The cache file's build information.</param>
        /// <returns>Returns a <see cref="ScriptingContextCollection"/>.</returns>
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


        /// <summary>
        /// Fills a context collection with unit seat mappings and context objects.
        /// </summary>
        /// <param name="reader">The stream to read from.</param>
        /// <param name="cache">The cache file containing the scripting context.</param>
        /// <param name="buildinfo">The cache file's build information.</param>
        /// <param name="data">The meta data in which is the context is stored.</param>
        /// <param name="collection">The context collection.</param>
        private static void FillContextCollection(IReader reader, ICacheFile cache, EngineDescription buildinfo, StructureValueCollection data, ScriptingContextCollection collection)
        {
            var tagBlocks = data.GetTagBlocks();

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
                    HandleParentWrapperBlock(block, collection);
                }
                else
                {
                    collection.AddObjectGroup(CreateContextBlock(block, false));
                }
            }
        }


        /// <summary>
        /// Determines whether a block is a wrapper block or not.
        /// </summary>
        /// <param name="block">The block to examine.</param>
        /// <returns>Returns true if the block is a wrapper block.</returns>
        private static bool IsWrapperBlock(KeyValuePair<string, StructureValueCollection[]> block)
        {
            return block.Value[0].HasStringID("Name") || block.Value[0].HasString("Name") ? false : true;
        }


        /// <summary>
        /// Processes a parent wrapper block and adds its child blocks to a context collection.
        /// </summary>
        /// <param name="block">The parent wrapper block.</param>
        /// <param name="collection">The context collection.</param>
        /// <exception cref="ArgumentException">Thrown when a parent wrapper block has more than one element.</exception>
        private static void HandleParentWrapperBlock(KeyValuePair<string, StructureValueCollection[]> block, ScriptingContextCollection collection)
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


        /// <summary>
        /// Creates a context block from meta data.
        /// </summary>
        /// <param name="data">The meta data.</param>
        /// <param name="isChildBlock">Is true, if this block is the child of a context object.</param>
        /// <returns>A context block.</returns>
        private static ScriptingContextBlock CreateContextBlock(KeyValuePair<string, StructureValueCollection[]> data, bool isChildBlock)
        {
            IEnumerable<ScriptingContextObject> objects = data.Value.Select((values, index) => CreateContextObject(index, data.Key, isChildBlock, values));
            return new ScriptingContextBlock(data.Key, objects);
        }

        private static IEnumerable<ScriptingContextObject> GetWrapperContextObjects(KeyValuePair<string, StructureValueCollection[]> block, int wrapperIndex)
        {
            return block.Value.Select((values, index) => CreateContextObject(index, block.Key, true, values, wrapperIndex));
        }


        /// <summary>
        /// Creates a context object from meta data.
        /// </summary>
        /// <param name="index">The index of the object.</param>
        /// <param name="group">The object group of the object.</param>
        /// <param name="isChildObject">is true if thi sobject is the child of another object.</param>
        /// <param name="data">The meta data.</param>
        /// <returns>A context object.</returns>
        private static ScriptingContextObject CreateContextObject(int index, string group, bool isChildObject, StructureValueCollection data, int wrapperIndex = -1)
        {
            string name = data.HasStringID("Name") ? data.GetStringID("Name") : data.GetString("Name");
            var result = new ScriptingContextObject(name.ToLowerInvariant(), index, group, isChildObject, wrapperIndex);

            var childBlocks = data.GetTagBlocks();
            if(childBlocks.Length > 0)
            {
                foreach(var block in childBlocks.Where(b => b.Value.Length > 0))
                {
                    // Handle child blocks, which are wrapped up in another block without a name field.
                    if(IsWrapperBlock(block))
                    {
                        var wrappedBlocks = new Dictionary<string, List<ScriptingContextObject>>();

                        // Iterate through the wrapper elements and collect all wrapped blocks and their objects.
                        for(int i = 0; i < block.Value.Length; i++)
                        {
                            var innerChildrenBlocks = block.Value[i].GetTagBlocks();
                            foreach (var inner in innerChildrenBlocks)
                            {
                                var wrappedObjects = GetWrapperContextObjects(inner, i);
                                if(wrappedBlocks.ContainsKey(inner.Key))
                                {
                                    wrappedBlocks[inner.Key].AddRange(wrappedObjects);
                                }
                                else
                                {
                                    wrappedBlocks[inner.Key] = wrappedObjects.ToList();
                                }
                            }
                        }

                        // Add the grouped blocks and objects to the result.
                        foreach(var groupedBlock in wrappedBlocks)
                        {
                            result.AddChildBlock(new ScriptingContextBlock(groupedBlock.Key, groupedBlock.Value));
                        }

                    }
                    // Handle regular child blocks.
                    else
                    {
                        result.AddChildBlock(CreateContextBlock(block, true));
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// Collects all unit seat mappings from a cache file and adds them to a context collection.
        /// </summary>
        /// <param name="reader">The stream to read from.</param>
        /// <param name="cache">The cache file.</param>
        /// <param name="buildInfo">The build information for this cache file.</param>
        /// <param name="block">The cache file's unit seat mappings block.</param>
        /// <param name="collection">The context collection.</param>
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

            // Identify and create seat mappings. Mappings will be grouped in this step.
            IEnumerable<UnitSeatMapping> mappings;
            if(buildInfo.Name.Contains("Halo 3"))
            {
                mappings = CreateUnitSeatMappings(information, PostProcessing.Halo3);
            }
            else
            {
                mappings = CreateUnitSeatMappings(information, PostProcessing.HaloReach);
            }

            // Add the final unit seat mapping objects to the result.
            foreach(var mapping in mappings)
            {
                collection.AddUnitSeatMapping(mapping);
            }
        }


        /// <summary>
        /// Creates unit seat mappings from mapping information. Also identifies and handles mapping groups.
        /// </summary>
        /// <param name="information">The information</param>
        /// <returns>A <see cref="IEnumerable"/> containing the finalized seat mappings.</returns>
        private static IEnumerable<UnitSeatMapping> CreateUnitSeatMappings(MappingInformation[] information, PostProcessing postProcessing)
        {
            var queue = new Queue<MappingInformation>(information);
            var result = new List<UnitSeatMapping>();
            while (queue.Count > 0)
            {
                MappingInformation info = queue.Dequeue();

                var members = new List<MappingInformation>();
                var processedTags = new List<string>();
                members.Add(info);
                processedTags.Add(info.TagName);

                // Retrieve other group members.
                while (queue.Count > 0)
                {
                    MappingInformation next = queue.Peek();

                    // The border of a group is reached, when any of these conditions is true.
                    if (next.TagName == info.TagName || next.SplitName[0] != info.SplitName[0] || next.SplitName[1] != info.SplitName[1] 
                        || members.Contains(next) || processedTags.Contains(next.TagName))
                    {
                        break;
                    }
                    else
                    {
                        processedTags.Add(next.TagName);
                        members.Add(queue.Dequeue());
                    }
                }

                // Handle mapping groups.
                if(members.Count > 1)
                {
                    if(postProcessing == PostProcessing.HaloReach)
                    {
                        // Some groups are not being detected correctly and need to be postprocessed. 
                        // If subsequent group member share the same name, they belong to a separate group. In this case the original group needs to be split.
                        List<MappingInformation> currentGroup = new List<MappingInformation> { members.First() };
                        foreach (var i in members.Skip(1))
                        {
                            if (currentGroup.Count > 1 && currentGroup.Last().Name != i.Name && currentGroup.All(i => i.Name == currentGroup[0].Name))
                            {
                                var group = new UnitSeatMapping(currentGroup.First().Index, (short)currentGroup.Count, currentGroup.First().Name);
                                result.Add(group);
                                currentGroup.Clear();
                            }

                            currentGroup.Add(i);
                        }

                        // Add the last group.
                        var mapping = new UnitSeatMapping(currentGroup.First().Index, (short)currentGroup.Count, GetMappingGroupName(currentGroup));
                        result.Add(mapping);
                        currentGroup.Clear();
                    }
                    else if(postProcessing == PostProcessing.Halo3)
                    {
                        // Halo 3 doesn't require any postprocessing.
                        result.Add(new UnitSeatMapping(members[0].Index, (short)members.Count, GetMappingGroupName(members)));
                    }
                }
                // Handle single mappings.
                else
                {
                    result.Add(new UnitSeatMapping(members[0].Index, 1, members[0].Name));
                }
            }
            return result;
        }


        /// <summary>
        /// Gets the name for a unit seat mapping group.
        /// </summary>
        /// <param name="mappings">The mappings that are members of this group.</param>
        /// <returns>The name of the group.</returns>
        /// <exception cref="ArgumentException">Thrown when one or more mapping names have less than 2 substrings.</exception>
        private static string GetMappingGroupName(IEnumerable<MappingInformation> mappings)
        {
            var split = mappings.Select(m => m.SplitName).ToArray();
            int maxLength = split.Min(s => s.Length);

            // Mapping names consist of at least 2 substrings.
            if(maxLength < 2)
            {
                throw new ArgumentException("Failed to calculate the the name for a unit seat mapping group. One or more mapping names were too short.");
            }

            int matchingParts = 2;

            // Calculate the longest common prefix.
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


        /// <summary>
        /// Converts a unit seat mapping's two seat bitmasks to a list of all seat indices, which belong to the mapping.
        /// </summary>
        /// <param name="seats1">The Seats1 bitmask of the unit seat mapping.</param>
        /// <param name="seats2">The Seats2 bitmask of the unit seat mapping.</param>
        /// <returns>A list of all seat indices, which belong to the unit seat mapping.</returns>
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


        /// <summary>
        /// Gets the name of a single seat mapping based on the vehicle meta data.
        /// </summary>
        /// <param name="seats">The seats meta data of the corresponding vehi tag.</param>
        /// <param name="indeces">The indeces of the seats, which belong to this seat mapping.</param>
        /// <returns></returns>
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

        private enum PostProcessing
        {
            Halo3,
            HaloReach
        }
    }
}
