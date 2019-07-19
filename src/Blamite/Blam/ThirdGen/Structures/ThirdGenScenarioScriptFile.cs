using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Scripting;
using Blamite.Blam.Scripting.Compiler;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;
using System.Diagnostics;

namespace Blamite.Blam.ThirdGen.Structures
{
	public class ThirdGenScenarioScriptFile : IScriptFile
	{
		private readonly EngineDescription _buildInfo;
		private readonly FileSegmentGroup _metaArea;
		private readonly StringIDSource _stringIDs;
        private readonly MetaAllocator _allocator;
		private readonly ITag _scnrTag;
        private readonly ITag _mdlgTag;
        private ScriptObjectReflexive _aoObjectiveRoles;
		private ScriptObjectReflexive _aiObjectives;
		private ScriptObjectReflexive _aiSquadGroups;
		private ScriptObjectReflexive _aiSquadSingleLocations;
		private ScriptObjectReflexive _aiSquads;

		private ScriptObjectReflexive _cutsceneCameraPoints;
		private ScriptObjectReflexive _cutsceneFlags;
		private ScriptObjectReflexive _cutsceneTitles;
		private ScriptObjectReflexive _deviceGroups;
		private ScriptObjectReflexive _objectFolders;
		private ScriptObjectReflexive _pointSetPoints;
		private ScriptObjectReflexive _pointSets;
		private ScriptObjectReflexive _objectNames;
		private ScriptObjectReflexive _startingProfiles;
		private ScriptObjectReflexive _triggerVolumes;
		private ScriptObjectReflexive _zoneSets;
        private ScriptObjectReflexive _designerZones;
        private ScriptObjectReflexive _aiLines;
        private ScriptObjectReflexive _aiLineVariants;

		public ThirdGenScenarioScriptFile(ITag scenarioTag, ITag mdlgTag, string scenarioName, FileSegmentGroup metaArea, MetaAllocator allocator,
			StringIDSource stringIDs, EngineDescription buildInfo)
		{
			_scnrTag = scenarioTag;
            _mdlgTag = mdlgTag;
			_stringIDs = stringIDs;
			_metaArea = metaArea;
            _allocator = allocator;
			_buildInfo = buildInfo;
			Name = scenarioName.Substring(scenarioName.LastIndexOf('\\') + 1) + ".hsc";

			DefineScriptObjectReflexives();
        }

		public string Name { get; private set; }

		public string Text
		{
			// Not stored in scenario scripts :(
			get { return null; }
		}

		public ScriptTable LoadScripts(IReader reader)
		{
			StructureValueCollection values = LoadScnrTag(reader);
            uint strSize = values.GetInteger("script string table size");
			var result = new ScriptTable();
			var stringReader = new StringTableReader();
			result.Scripts = LoadScripts(reader, values);
			result.Globals = LoadGlobals(reader, values);
			result.Expressions = LoadExpressions(reader, values, stringReader);

			CachedStringTable strings = LoadStrings(reader, values, stringReader);
			foreach (ScriptExpression expr in result.Expressions.Where(e => (e != null)))
				expr.ResolveStrings(strings);

			return result;
		}

		public void SaveScripts(ScriptData data, IStream stream)
		{
            StructureValueCollection scnr = LoadScnrTag(stream);
            StructureLayout scnrLayout = _buildInfo.Layouts.GetLayout("scnr");

            WriteExpressions(data, stream, scnr);
            WriteScriptsAndParams(data, stream, scnr);
            WriteStrings(data, stream, scnr);
            WriteGlobals(data, stream, scnr);
            WriteTagReferences(data, stream, scnr);

            stream.SeekTo(_scnrTag.MetaLocation.AsOffset());
            StructureWriter.WriteStructure(scnr, scnrLayout, stream);
        }

		public ScriptContext LoadContext(IReader reader)
		{
			StructureValueCollection scnrValues = LoadScnrTag(reader);
            StructureValueCollection mdlgValues = LoadMdlgTag(reader);

            return new ScriptContext
            {
                ObjectNames = ReadObjects(reader, scnrValues, _objectNames),
                TriggerVolumes = ReadObjects(reader, scnrValues, _triggerVolumes),
                CutsceneFlags = ReadObjects(reader, scnrValues, _cutsceneFlags),
                CutsceneCameraPoints = ReadObjects(reader, scnrValues, _cutsceneCameraPoints),
                CutsceneTitles = ReadObjects(reader, scnrValues, _cutsceneTitles),
                DeviceGroups = ReadObjects(reader, scnrValues, _deviceGroups),
                AISquadGroups = ReadObjects(reader, scnrValues, _aiSquadGroups),
                AISquads = ReadObjects(reader, scnrValues, _aiSquads),
                AIObjectives = ReadObjects(reader, scnrValues, _aiObjectives),
                StartingProfiles = ReadObjects(reader, scnrValues, _startingProfiles),
                ZoneSets = ReadObjects(reader, scnrValues, _zoneSets),
                DesignerZones = ReadObjects(reader, scnrValues, _designerZones),
                ObjectFolders = ReadObjects(reader, scnrValues, _objectFolders),
                PointSets = ReadPointSets(reader, scnrValues),
                AISquadSingleLocations = _aiSquadSingleLocations,
                AIObjectiveRoles = _aoObjectiveRoles,
                PointSetPoints = _pointSetPoints,
                AILines = ReadObjects(reader, mdlgValues, _aiLines),
                AILineVariants = _aiLineVariants,
                UnitSeatMappingCount = (int)scnrValues.GetInteger("unit seat mapping count")
            };
		}

		private StructureValueCollection LoadScnrTag(IReader reader)
		{
			reader.SeekTo(_scnrTag.MetaLocation.AsOffset());
			return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("scnr"));
		}

        private StructureValueCollection LoadMdlgTag(IReader reader)
        {
            reader.SeekTo(_mdlgTag.MetaLocation.AsOffset());
            return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("mdlg"));
        }

        private List<ScriptGlobal> LoadGlobals(IReader reader, StructureValueCollection values)
		{
			var count = (int) values.GetInteger("number of script globals");
			uint address = values.GetInteger("script global table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("script global entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			return entries.Select(e => new ScriptGlobal(e)).ToList();
		}

		private List<Script> LoadScripts(IReader reader, StructureValueCollection values)
		{
			var count = (int) values.GetInteger("number of scripts");
			uint address = values.GetInteger("script table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("script entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			return entries.Select(e => new Script(e, reader, _metaArea, _stringIDs, _buildInfo)).ToList();
		}

		private ScriptExpressionTable LoadExpressions(IReader reader, StructureValueCollection values,
			StringTableReader stringReader)
		{
            var stringsSize = (int)values.GetInteger("script string table size");
            var count = (int) values.GetInteger("number of script expressions");
			uint address = values.GetInteger("script expression table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("script expression entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);

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
			int tableOffset = _metaArea.PointerToOffset(values.GetInteger("script string table address"));
			stringReader.ReadRequestedStrings(reader, tableOffset, result);
			return result;
		}

		private void DefineScriptObjectReflexives()
		{
			_objectNames = new ScriptObjectReflexive("number of object names", "object names table address",
				"object name entry");
			_triggerVolumes = new ScriptObjectReflexive("number of trigger volumes", "trigger volumes table address",
				"trigger volume entry");
			_cutsceneFlags = new ScriptObjectReflexive("number of cutscene flags", "cutscene flags table address",
				"cutscene flag entry");
			_cutsceneCameraPoints = new ScriptObjectReflexive("number of cutscene camera points",
				"cutscene camera points table address", "cutscene camera point entry");
			_cutsceneTitles = new ScriptObjectReflexive("number of cutscene titles", "cutscene titles table address",
				"cutscene title entry");
			_deviceGroups = new ScriptObjectReflexive("number of device groups", "device groups table address",
				"device group entry");
			_aiSquadGroups = new ScriptObjectReflexive("number of ai squad groups", "ai squad groups table address",
				"ai squad group entry");
			_aiSquads = new ScriptObjectReflexive("number of ai squads", "ai squads table address", "ai squad entry");
			_aiSquadSingleLocations = new ScriptObjectReflexive("number of single locations", "single locations table address",
				"ai squad single location entry");
			_aiObjectives = new ScriptObjectReflexive("number of ai objectives", "ai objectives table address", "ai objectives entry");
			_aoObjectiveRoles = new ScriptObjectReflexive("number of roles", "roles table address", "ai objective role entry");
			_startingProfiles = new ScriptObjectReflexive("number of starting profiles", "starting profiles table address",
				"starting profile entry");
			_zoneSets = new ScriptObjectReflexive("number of zone sets", "zone sets table address", "zone set entry");
            _designerZones = new ScriptObjectReflexive("number of designer zones", "designer zones table address", "designer zone entry");
            _objectFolders = new ScriptObjectReflexive("number of object folders", "object folders table address",
				"object folder entry");
			_pointSets = new ScriptObjectReflexive("number of point sets", "point sets table address", "point set entry");
			_pointSetPoints = new ScriptObjectReflexive("number of points", "points table address", "point set point entry");

            _aiSquads.RegisterChild(_aiSquadSingleLocations);
			_aiObjectives.RegisterChild(_aoObjectiveRoles);
			_pointSets.RegisterChild(_pointSetPoints);


            _aiLines = new ScriptObjectReflexive("number of lines", "lines table address", "line entry");
            _aiLineVariants = new ScriptObjectReflexive("number of variants", "variants table address", "line variants entry");
            _aiLines.RegisterChild(_aiLineVariants);
		}

		private ScriptObject[] ReadObjects(IReader reader, StructureValueCollection values, ScriptObjectReflexive reflexive)
		{
			return reflexive.ReadObjects(values, reader, _metaArea, _stringIDs, _buildInfo);
		}

		private ScriptObject[] ReadPointSets(IReader reader, StructureValueCollection values)
		{
			// Point sets are nested in another reflexive for whatever reason
			// Seems like the length of the outer is always 1, so just take the first entry and process it
			var count = (int) values.GetInteger("point set data count");
			if (count == 0)
				return new ScriptObject[0];

			uint address = values.GetInteger("point set data address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("point set data entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
			return ReadObjects(reader, entries.First(), _pointSets);
		}

        private void WriteScriptsAndParams(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            StructureLayout scrlayout = _buildInfo.Layouts.GetLayout("script entry");
            StructureLayout paramlayout = _buildInfo.Layouts.GetLayout("script parameter entry");

            var oldScriptCount = (int)scnr.GetInteger("number of scripts");
            uint oldScriptAddress = scnr.GetInteger("script table address");
            int oldScriptSize = oldScriptCount * scrlayout.Size;
            uint newScriptAddress = 0;

            uint oldParamAddress = 0;
            int oldParamSize = 0;

            // Get the param table address and size
            StructureValueCollection[] oldScripts = ReflexiveReader.ReadReflexive(stream, oldScriptCount, oldScriptAddress, scrlayout, _metaArea);
            StructureValueCollection first = Array.Find(oldScripts, s => s.GetInteger("number of parameters") > 0);
            StructureValueCollection last = Array.FindLast(oldScripts, s => s.GetInteger("number of parameters") > 0);
            if (first != null && last != null)
            {
                oldParamAddress = first.GetInteger("address of parameter list");
                uint lastCount = last.GetInteger("number of parameters");
                uint endAddress = last.GetInteger("address of parameter list") + (uint)(lastCount * paramlayout.Size);
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

                uint newParamAddress = 0;

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
                        scr.Write(stream, _stringIDs, paramCount, newParamAddress);
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
            scnr.SetInteger("script table address", newScriptAddress);
        }

        private void WriteStrings(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            var oldStringsSize = (int)scnr.GetInteger("script string table size");
            uint oldStringAddress = scnr.GetInteger("script string table address");
            uint newStringsAddress = 0;

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
            scnr.SetInteger("script string table address", newStringsAddress);
        }

        private void WriteGlobals(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            StructureLayout glolayout = _buildInfo.Layouts.GetLayout("script global entry");
            var oldGlobalCount = (int)scnr.GetInteger("number of script globals");
            uint oldGlobalAddress = scnr.GetInteger("script global table address");
            int oldGlobalSize = oldGlobalCount * glolayout.Size;
            uint newGlobalAddress = 0;

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
            scnr.SetInteger("script global table address", newGlobalAddress);
        }

        private void WriteExpressions(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            StructureLayout expLayout = _buildInfo.Layouts.GetLayout("script expression entry");
            var oldExpCount = (int)scnr.GetInteger("number of script expressions");
            uint oldExpAddress = scnr.GetInteger("script expression table address");
            int oldExpSize = oldExpCount * expLayout.Size;
            uint newExpAddress = 0;

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
            scnr.SetInteger("script expression table address", newExpAddress);
        }

        private void WriteTagReferences(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            var oldRefCount = (int)scnr.GetInteger("number of script references");
            uint oldRefAddress = scnr.GetInteger("script references table address");
            int oldRefSize = oldRefCount * 0x10;
            uint newRefAddress = 0;

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
                    stream.WriteInt32(tag.Class.Magic);
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
            scnr.SetInteger("script references table address", newRefAddress);
        }
    }
}