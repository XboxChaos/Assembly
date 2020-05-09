using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Scripting;
using Blamite.Blam.Scripting.Compiler;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Util;
using System.Diagnostics;

namespace Blamite.Blam.ThirdGen.Structures
{
	public class ThirdGenScenarioScriptFile : IScriptFile
	{
		private readonly EngineDescription _buildInfo;
		private readonly FileSegmentGroup _metaArea;
		private readonly StringIDSource _stringIDs;
        private readonly MetaAllocator _allocator;
        private readonly IPointerExpander _expander;

        private readonly ITag _scenarioTag;
		private readonly ITag _scriptTag;
        private readonly ITag _mdlgTag;

		private ScriptObjectTagBlock _aiObjectWaves;
		private ScriptObjectTagBlock _aiObjects;
		private ScriptObjectTagBlock _aiSquadGroups;
		private ScriptObjectTagBlock _aiSquadSingleLocations;
		private ScriptObjectTagBlock _aiSquads;

		private ScriptObjectTagBlock _cutsceneCameraPoints;
		private ScriptObjectTagBlock _cutsceneFlags;
		private ScriptObjectTagBlock _cutsceneTitles;
		private ScriptObjectTagBlock _deviceGroups;
		private ScriptObjectTagBlock _objectFolders;

		private ScriptObjectTagBlock _pointSetPoints;
		private ScriptObjectTagBlock _pointSets;
		private ScriptObjectTagBlock _referencedObjects;
		private ScriptObjectTagBlock _startingProfiles;
		private ScriptObjectTagBlock _triggerVolumes;

		private ScriptObjectTagBlock _zoneSets;
        private ScriptObjectTagBlock _designerZones;
        private ScriptObjectTagBlock _aiLines;
        private ScriptObjectTagBlock _aiLineVariants;

        // For Games before Halo 4
		public ThirdGenScenarioScriptFile(ITag scenarioTag, ITag scriptTag, ITag mdlgTag, string tagName, FileSegmentGroup metaArea,
			StringIDSource stringIDs, EngineDescription buildInfo, IPointerExpander expander, MetaAllocator allocator)
		{
			_scenarioTag = scenarioTag;
            _scriptTag = scriptTag;
            _mdlgTag = mdlgTag;
            _metaArea = metaArea;
            _stringIDs = stringIDs;
            _buildInfo = buildInfo;
            _expander = expander;
            _allocator = allocator;

			Name = tagName.Substring(tagName.LastIndexOf('\\') + 1) + ".hsc";

			DefineScriptObjectTagBlocks();
		}

        // For Halo4 and Games thereafter
		//public ThirdGenScenarioScriptFile(ITag scenarioTag, ITag scriptTag, string tagName, FileSegmentGroup metaArea,
	 //       StringIDSource stringIDs, EngineDescription buildInfo, IPointerExpander expander, MetaAllocator allocator)
		//{
  //          _scenarioTag = scenarioTag;
  //          _scriptTag = scriptTag;
  //          _mdlgTag = null;
  //          _metaArea = metaArea;
  //          _stringIDs = stringIDs;
  //          _buildInfo = buildInfo;
  //          _expander = expander;
  //          _allocator = allocator;

  //          Name = tagName.Substring(tagName.LastIndexOf('\\') + 1) + ".hsc";

		//	DefineScriptObjectTagBlocks();
		//}

		public string Name { get; private set; }

		public string Text
		{
			// Not stored in scenario scripts :(
			get { return null; }
		}

		public ScriptTable LoadScripts(IReader reader)
		{
            StructureValueCollection values = LoadScriptTag(reader, _scriptTag);

			//if (_scriptTag != _scenarioTag)
			//	values = LoadScriptTag(reader);
			//else
			//	values = LoadTag(reader);
                
            ulong strSize = values.GetInteger("script string table size");
			var result = new ScriptTable();
			var stringReader = new StringTableReader();
				
			result.Scripts = LoadScripts(reader, values);
			result.Globals = LoadGlobals(reader, values);
			result.Variables = LoadVariables(reader, values);
			result.Expressions = LoadExpressions(reader, values, stringReader);

			CachedStringTable strings = LoadStrings(reader, values, stringReader);
			foreach (ScriptExpression expr in result.Expressions.Where(e => (e != null)))
				expr.ResolveStrings(strings);

			return result;
		}

		public void SaveScripts(ScriptData data, IStream stream)
		{
            StructureValueCollection values = LoadScriptTag(stream, _scriptTag);
            StructureLayout scnrLayout = _buildInfo.Layouts.GetLayout("scnr");

            WriteExpressions(data, stream, values);
            WriteScriptsAndParams(data, stream, values);
            WriteStrings(data, stream, values);
            WriteGlobals(data, stream, values);
            WriteTagReferences(data, stream, values);

            stream.SeekTo(_scriptTag.MetaLocation.AsOffset());
            StructureWriter.WriteStructure(values, scnrLayout, stream);
        }

        public SortedDictionary<uint, UnitSeatMapping> GetUniqueSeatMappings(IReader reader, ushort opcode)
        {
            // load the expressions
            StructureValueCollection values = LoadScriptTag(reader, _scriptTag);
            var stringReader = new StringTableReader();
            ScriptExpressionTable expressions = LoadExpressions(reader, values, stringReader);
            CachedStringTable strings = LoadStrings(reader, values, stringReader);
            foreach (ScriptExpression expr in expressions.Where(e => (e != null)))
                expr.ResolveStrings(strings);


            // find all unique mappings
            SortedDictionary<uint, UnitSeatMapping> uniqueMappings = new SortedDictionary<uint, UnitSeatMapping>();

            foreach (var exp in expressions)
            {
                if (exp.Opcode == opcode && exp.ReturnType == opcode && exp.Value != 0xFFFFFFFF)
                {
                    // Calculate the index and only add it if it doesn't exist yet.
                    uint index = exp.Value & 0xFFFF;
                    if (!uniqueMappings.ContainsKey(index))
                    {
                        uint count = (exp.Value & 0xFFFF0000) >> 16;
                        string name = exp.StringValue;
                        UnitSeatMapping mapping = new UnitSeatMapping((short)index, (short)count, name);
                        uniqueMappings.Add(index, mapping);
                    }
                }
            }
            return uniqueMappings;
        }

        private void DefineScriptObjectTagBlocks()
        {
            _referencedObjects = new ScriptObjectTagBlock("number of script objects", "script object table address",
                "script object element");
            _triggerVolumes = new ScriptObjectTagBlock("number of trigger volumes", "trigger volumes table address",
                "trigger volume element");
            _cutsceneFlags = new ScriptObjectTagBlock("number of cutscene flags", "cutscene flags table address",
                "cutscene flag element");
            _cutsceneCameraPoints = new ScriptObjectTagBlock("number of cutscene camera points",
                "cutscene camera points table address", "cutscene camera point element");
            _cutsceneTitles = new ScriptObjectTagBlock("number of cutscene titles", "cutscene titles table address",
                "cutscene title element");
            _deviceGroups = new ScriptObjectTagBlock("number of device groups", "device groups table address",
                "device group element");
            _aiSquadGroups = new ScriptObjectTagBlock("number of ai squad groups", "ai squad groups table address",
                "ai squad group element");
            _aiSquads = new ScriptObjectTagBlock("number of ai squads", "ai squads table address", "ai squad element");
            _aiSquadSingleLocations = new ScriptObjectTagBlock("number of single locations", "single locations table address",
                "ai squad single location element");
            _aiObjects = new ScriptObjectTagBlock("number of ai objects", "ai objects table address", "ai object element");
            _aiObjectWaves = new ScriptObjectTagBlock("number of waves", "waves table address", "ai object wave element");
            _startingProfiles = new ScriptObjectTagBlock("number of starting profiles", "starting profiles table address",
                "starting profile element");
            _zoneSets = new ScriptObjectTagBlock("number of zone sets", "zone sets table address", "zone set element");
            _objectFolders = new ScriptObjectTagBlock("number of object folders", "object folders table address",
                "object folder element");
            _designerZones = new ScriptObjectTagBlock("number of designer zones", "designer zones table address", "designer zone element");
            _objectFolders = new ScriptObjectTagBlock("number of object folders", "object folders table address", "object folder element");
            _pointSets = new ScriptObjectTagBlock("number of point sets", "point sets table address", "point set element");
            _pointSetPoints = new ScriptObjectTagBlock("number of points", "points table address", "point set point element");

            _aiSquads.RegisterChild(_aiSquadSingleLocations);
            _aiObjects.RegisterChild(_aiObjectWaves);
            _pointSets.RegisterChild(_pointSetPoints);


            _aiLines = new ScriptObjectTagBlock("number of lines", "lines table address", "line element");
            _aiLineVariants = new ScriptObjectTagBlock("number of variants", "variants table address", "line variants element");
            _aiLines.RegisterChild(_aiLineVariants);
        }

        public ScriptContext LoadContext(IReader reader)
		{
			StructureValueCollection scnrValues = LoadScriptTag(reader, _scenarioTag);

            ScriptObject[] lines = new ScriptObject[0];
            //ScriptObject[] lineVariants = new ScriptObject[0];

            if(_mdlgTag != null)
            {
                StructureValueCollection mdlgValues = LoadScriptTag(reader, _mdlgTag);
                lines = ReadObjects(reader, mdlgValues, _aiLines);
            }


            return new ScriptContext
            {
                ObjectReferences = ReadObjects(reader, scnrValues, _referencedObjects),
                TriggerVolumes = ReadObjects(reader, scnrValues, _triggerVolumes),
                CutsceneFlags = ReadObjects(reader, scnrValues, _cutsceneFlags),
                CutsceneCameraPoints = ReadObjects(reader, scnrValues, _cutsceneCameraPoints),
                CutsceneTitles = ReadObjects(reader, scnrValues, _cutsceneTitles),
                DeviceGroups = ReadObjects(reader, scnrValues, _deviceGroups),
                AISquadGroups = ReadObjects(reader, scnrValues, _aiSquadGroups),
                AISquads = ReadObjects(reader, scnrValues, _aiSquads),
                AIObjects = ReadObjects(reader, scnrValues, _aiObjects),
                StartingProfiles = ReadObjects(reader, scnrValues, _startingProfiles),
                ZoneSets = ReadObjects(reader, scnrValues, _zoneSets),
                DesignerZones = ReadObjects(reader, scnrValues, _designerZones),
                ObjectFolders = ReadObjects(reader, scnrValues, _objectFolders),
                PointSets = ReadPointSets(reader, scnrValues),
                AISquadSingleLocations = _aiSquadSingleLocations,
                AIObjectWaves = _aiObjectWaves,
                PointSetPoints = _pointSetPoints,

                AILines = lines,
                AILineVariants = _aiLineVariants,
                UnitSeatMappingCount = (int)scnrValues.GetInteger("unit seat mapping count")
            };
		}

        //private StructureValueCollection LoadScnrTag(IReader reader)
        //{
        //    reader.SeekTo(_scenarioTag.MetaLocation.AsOffset());
        //    return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("scnr"));
        //}
        //private StructureValueCollection LoadScriptTag(IReader reader)
        //{
        //    reader.SeekTo(_scriptTag.MetaLocation.AsOffset());
        //    return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("hsdt"));
        //}

        //private StructureValueCollection LoadMdlgTag(IReader reader)
        //{
        //    reader.SeekTo(_mdlgTag.MetaLocation.AsOffset());
        //    return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("mdlg"));
        //}

        private StructureValueCollection LoadScriptTag(IReader reader, ITag tag)
        {
            reader.SeekTo(tag.MetaLocation.AsOffset());
            string groupMagic = CharConstant.ToString(tag.Group.Magic);
            switch (groupMagic)
            {
                case "scnr":
                    {
                        return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("scnr"));
                    }
                case "hsdt":
                    {
                        return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("hsdt"));
                    }
                case "mdlg":
                    {
                        return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("mdlg"));
                    }
                default:
                    {
                        throw new Exception("The tag doesn't belong to a script related tag class");
                    }
            }
        }

        private List<ScriptGlobal> LoadGlobals(IReader reader, StructureValueCollection values)
		{
			var count = (int) values.GetInteger("number of script globals");
			uint address = (uint)values.GetInteger("script global table address");

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("script global element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			return entries.Select(e => new ScriptGlobal(e, _stringIDs)).ToList();
		}

		private List<ScriptGlobal> LoadVariables(IReader reader, StructureValueCollection values)
		{
			if (_buildInfo.Layouts.HasLayout("script variable element"))
			{
				var count = (int)values.GetInteger("number of script variables");
				uint address = (uint)values.GetInteger("script variable table address");

				long expand = _expander.Expand(address);

				StructureLayout layout = _buildInfo.Layouts.GetLayout("script variable element");
				StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
				return entries.Select(e => new ScriptGlobal(e, _stringIDs)).ToList();
			}
			else
				return null;

		}

		private List<Script> LoadScripts(IReader reader, StructureValueCollection values)
		{
			var count = (int) values.GetInteger("number of scripts");
			uint address = (uint)values.GetInteger("script table address");

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("script element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			return entries.Select(e => new Script(e, reader, _metaArea, _stringIDs, _buildInfo, _expander)).ToList();
		}

		private ScriptExpressionTable LoadExpressions(IReader reader, StructureValueCollection values,
			StringTableReader stringReader)
		{
            var stringsSize = (int)values.GetInteger("script string table size");           
            var count = (int) values.GetInteger("number of script expressions");
			uint address = (uint)values.GetInteger("script expression table address");
			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("script expression element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);

			var result = new ScriptExpressionTable();
			result.AddExpressions(entries.Select((e, i) => new ScriptExpression(e, (ushort) i, stringReader, stringsSize)));

			foreach (ScriptExpression expr in result.Where(expr => expr != null))
				expr.ResolveReferences(result);

			return result;
		}

		private CachedStringTable LoadStrings(IReader reader, StructureValueCollection values, StringTableReader stringReader)
		{
			var stringsSize = (int) values.GetInteger("script string table size");
			if (stringsSize == 0)
				return new CachedStringTable();

			var result = new CachedStringTable();

			uint tableAddr = (uint)values.GetInteger("script string table address");

			long expand = _expander.Expand(tableAddr);

			int tableOffset = _metaArea.PointerToOffset(expand);
			stringReader.ReadRequestedStrings(reader, tableOffset, result);
			return result;
		}

		private ScriptObject[] ReadObjects(IReader reader, StructureValueCollection values, ScriptObjectTagBlock block)
		{
			return block.ReadObjects(values, reader, _metaArea, _stringIDs, _buildInfo, _expander);
		}

		private ScriptObject[] ReadPointSets(IReader reader, StructureValueCollection values)
		{
			// Point sets are nested in another block for whatever reason
			// Seems like the length of the outer is always 1, so just take the first element and process it
			var count = (int) values.GetInteger("point set data count");
			if (count == 0)
				return new ScriptObject[0];

			uint address = (uint)values.GetInteger("point set data address");

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("point set data element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			return ReadObjects(reader, entries.First(), _pointSets);
		}

        private void WriteScriptsAndParams(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            StructureLayout scrlayout = _buildInfo.Layouts.GetLayout("script element");
            StructureLayout paramlayout = _buildInfo.Layouts.GetLayout("script parameter element");

            int oldScriptCount = (int)scnr.GetInteger("number of scripts");
            long oldScriptAddress = (long)scnr.GetInteger("script table address");
            int oldScriptSize = oldScriptCount * scrlayout.Size;
            long newScriptAddress = 0;

            long oldParamAddress = 0;
            int oldParamSize = 0;

            // Get the param table address and size
            StructureValueCollection[] oldScripts = TagBlockReader.ReadTagBlock(stream, oldScriptCount, oldScriptAddress, scrlayout, _metaArea);
            StructureValueCollection first = Array.Find(oldScripts, s => s.GetInteger("number of parameters") > 0);
            StructureValueCollection last = Array.FindLast(oldScripts, s => s.GetInteger("number of parameters") > 0);
            if (first != null && last != null)
            {
                oldParamAddress = (uint)first.GetInteger("address of parameter list");
                uint lastCount = (uint)last.GetInteger("number of parameters");
                uint endAddress = (uint)last.GetInteger("address of parameter list") + (uint)(lastCount * paramlayout.Size);
                oldParamSize = (int)(endAddress - oldParamAddress);
            }


            // Check if the new hsc file contained scripts
            if (data.Scripts.Count > 0)
            {
                // calculate the size of the new param table
                List<ScriptParameter> newParams = new List<ScriptParameter>();
                foreach (var scr in data.Scripts)
                {
                    if (scr.Parameters.Count > 0)
                    {
                        newParams.AddRange(scr.Parameters);
                    }
                }

                long newParamAddress = 0;

                if (newParams.Count > 0)
                {
                    int newParamSize = newParams.Count * paramlayout.Size;

                    // allocate or realllocate the params table
                    if (oldParamSize > 0)
                    {
                        newParamAddress = _allocator.Reallocate(oldParamAddress, oldParamSize, newParamSize, stream);
                    }
                    else
                    {
                        newParamAddress = _allocator.Allocate(newParamSize, stream);
                    }

                    // write params
                    stream.SeekTo(_metaArea.PointerToOffset(newParamAddress));
                    foreach (var par in newParams)
                    {
                        par.Write(stream);
                    }
                }

                // new address of the script reflexive
                int newScriptSize = data.Scripts.Count * scrlayout.Size;

                if (oldScriptSize > 0)
                {
                    newScriptAddress = _allocator.Reallocate(oldScriptAddress, oldScriptSize, newScriptSize, stream);
                }
                else
                {
                    newScriptAddress = _allocator.Allocate(newScriptSize, stream);
                }

                // write scripts
                stream.SeekTo(_metaArea.PointerToOffset(newScriptAddress));

                foreach (var scr in data.Scripts)
                {
                    int paramCount = scr.Parameters.Count;
                    // has parameters
                    if (paramCount > 0)
                    {
                        scr.Write(stream, _stringIDs, paramCount, _expander.Contract(newParamAddress));
                        newParamAddress += (uint)(paramCount * paramlayout.Size);
                    }
                    // doesn't have parameters
                    else
                    {
                        scr.Write(stream, _stringIDs, 0, 0);
                    }
                }
            }
            // free the script reflexive if it existed
            else if (oldScriptSize > 0)
            {
                _allocator.Free(oldScriptAddress, oldScriptSize);
                // Free the param table if it existed
                if (oldParamAddress != 0 && oldParamSize != 0)
                {
                    _allocator.Free(oldParamAddress, oldParamSize);
                }
            }

            scnr.SetInteger("number of scripts", (uint)data.Scripts.Count);
            scnr.SetInteger("script table address", _expander.Contract(newScriptAddress));
        }

        private void WriteStrings(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            int oldStringsSize = (int)scnr.GetInteger("script string table size");
            long oldStringAddress = (long)scnr.GetInteger("script string table address");
            long newStringsAddress = 0;

            // Check if the returned data contained strings
            if (data.Strings.Size > 0)
            {
                // allocate space for the string table
                if (oldStringsSize > 0)
                {
                    newStringsAddress = _allocator.Reallocate(oldStringAddress, oldStringsSize, data.Strings.Size, stream);
                }
                else
                {
                    newStringsAddress = _allocator.Allocate(data.Strings.Size, stream);
                }
                // write strings
                stream.SeekTo(_metaArea.PointerToOffset(newStringsAddress));
                data.Strings.Write(stream);
            }
            // free the old string table if the new scripts didn't contain strings (impossible)
            else if (oldStringsSize > 0)
            {
                _allocator.Free(oldStringAddress, oldStringsSize);
            }

            scnr.SetInteger("script string table size", (uint)data.Strings.Size);
            scnr.SetInteger("script string table address", _expander.Contract(newStringsAddress));
        }

        private void WriteGlobals(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            StructureLayout glolayout = _buildInfo.Layouts.GetLayout("script global element");
            int oldGlobalCount = (int)scnr.GetInteger("number of script globals");
            long oldGlobalAddress = (long)scnr.GetInteger("script global table address");
            int oldGlobalSize = oldGlobalCount * glolayout.Size;
            long newGlobalAddress = 0;

            // check if the returned data contains globals
            if (data.Globals.Count > 0)
            {
                int newGlobalSize = data.Globals.Count * glolayout.Size;

                // allocate space for the globals
                if (oldGlobalCount > 0)
                {
                    newGlobalAddress = _allocator.Reallocate(oldGlobalAddress, oldGlobalSize, newGlobalSize, stream);
                }
                else
                {
                    newGlobalAddress = _allocator.Allocate(newGlobalSize, stream);
                }

                // write globals
                stream.SeekTo(_metaArea.PointerToOffset(newGlobalAddress));
                foreach (var glo in data.Globals)
                {
                    glo.Write(stream);
                }
            }
            else if (oldGlobalCount > 0)
            {
                _allocator.Free(oldGlobalAddress, oldGlobalSize);
            }

            scnr.SetInteger("number of script globals", (uint)data.Globals.Count);
            scnr.SetInteger("script global table address", _expander.Contract(newGlobalAddress));
        }

        private void WriteExpressions(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            StructureLayout expLayout = _buildInfo.Layouts.GetLayout("script expression element");
            int oldExpCount = (int)scnr.GetInteger("number of script expressions");
            long oldExpAddress = (long)scnr.GetInteger("script expression table address");
            int oldExpSize = oldExpCount * expLayout.Size;
            long newExpAddress = 0;

            if (data.Expressions.Count > 0)
            {
                int newExpSize = data.Expressions.Count * expLayout.Size;

                // allocate space for the expessions
                if (oldExpCount > 0)
                {
                    newExpAddress = _allocator.Reallocate(oldExpAddress, oldExpCount, newExpSize, stream);
                }
                else
                {
                    newExpAddress = _allocator.Allocate(newExpSize, stream);
                }

                // write expressions
                stream.SeekTo(_metaArea.PointerToOffset(newExpAddress));
                foreach (var exp in data.Expressions)
                {
                    exp.Write(stream);
                }
            }
            else if (oldExpCount > 0)
            {
                _allocator.Free(oldExpAddress, oldExpSize);
            }

            scnr.SetInteger("number of script expressions", (uint)data.Expressions.Count);
            scnr.SetInteger("script expression table address", _expander.Contract(newExpAddress));
        }

        private void WriteTagReferences(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            int oldRefCount = (int)scnr.GetInteger("number of script references");
            long oldRefAddress = (long)scnr.GetInteger("script references table address");
            int oldRefSize = oldRefCount * 0x10;
            long newRefAddress = 0;

            //check if the returned data contained tag references
            if (data.TagReferences.Count > 0)
            {
                int newRefSize = data.TagReferences.Count * 0x10;

                // allocate space for the references
                if (oldRefCount > 0)
                {
                    newRefAddress = _allocator.Reallocate(oldRefAddress, oldRefSize, newRefSize, stream);
                }
                else
                {
                    newRefAddress = _allocator.Allocate(newRefSize, stream);
                }

                //write the references
                stream.SeekTo(_metaArea.PointerToOffset(newRefAddress));
                foreach (ITag tag in data.TagReferences)
                {
                    stream.WriteInt32(tag.Group.Magic);
                    stream.WriteUInt32(0);
                    stream.WriteUInt32(0);
                    stream.WriteUInt32(tag.Index.Value);
                }
            }
            else if (oldRefCount > 0)
            {
                _allocator.Free(oldRefAddress, oldRefSize);
            }

            scnr.SetInteger("number of script references", (uint)data.TagReferences.Count);
            scnr.SetInteger("script references table address", _expander.Contract(newRefAddress));
        }
    }
}