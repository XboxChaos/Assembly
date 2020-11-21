using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Scripting;
using Blamite.Blam.Scripting.Compiler;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.SecondGen.Structures
{
	public class SecondGenScenarioScriptFile : IScriptFile
	{
		private readonly EngineDescription _buildInfo;
		private readonly FileSegmentGroup _metaArea;
		private readonly StringIDSource _stringIDs;
		private readonly ITag _scenarioTag;
		private readonly ITag _scriptTag;
		private readonly IPointerExpander _expander;

		private ScriptObjectTagBlock _aiSquadSingleLocations;
		private ScriptObjectTagBlock _aiSquads;

		private ScriptObjectTagBlock _cutsceneCameraPoints;
		private ScriptObjectTagBlock _cutsceneFlags;
		private ScriptObjectTagBlock _cutsceneTitles;
		private ScriptObjectTagBlock _deviceGroups;
		private ScriptObjectTagBlock _objectFolders;
		private ScriptObjectTagBlock _referencedObjects;
		private ScriptObjectTagBlock _startingProfiles;
		private ScriptObjectTagBlock _triggerVolumes;

		public SecondGenScenarioScriptFile(ITag scenarioTag, string tagName, FileSegmentGroup metaArea,
			StringIDSource stringIDs, EngineDescription buildInfo, IPointerExpander expander)
		{
			_scenarioTag = scenarioTag;
			_expander = expander;
			_scriptTag = null;
			_stringIDs = stringIDs;
			_metaArea = metaArea;
			_buildInfo = buildInfo;
			Name = tagName.Substring(tagName.LastIndexOf('\\') + 1) + ".hsc";

			DefineScriptObjectTagBlocks();
		}

		public SecondGenScenarioScriptFile(ITag scenarioTag, ITag scriptTag, string tagName, FileSegmentGroup metaArea,
	StringIDSource stringIDs, EngineDescription buildInfo, IPointerExpander expander)
		{
			_scenarioTag = scenarioTag;
			_expander = expander;
			_scriptTag = scriptTag;
			_stringIDs = stringIDs;
			_metaArea = metaArea;
			_buildInfo = buildInfo;
			Name = tagName.Substring(tagName.LastIndexOf('\\') + 1) + ".hsc";

			DefineScriptObjectTagBlocks();
		}

		public string Name { get; private set; }

		public string Text
		{
			// Not stored in scenario scripts :(
			get { return null; }
		}

		public ScriptTable LoadScripts(IReader reader)
		{
			StructureValueCollection values;

			if (_scriptTag != null)
				values = LoadScriptTag(reader);
			else
				values = LoadTag(reader);

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

		public void SaveScripts(ScriptTable scripts, IStream stream)
		{
			throw new NotImplementedException();
		}

		public ScriptContext LoadContext(IReader reader)
		{
			StructureValueCollection values = LoadTag(reader);

			return new ScriptContext
			{
				ObjectReferences = ReadObjects(reader, values, _referencedObjects),
				TriggerVolumes = ReadObjects(reader, values, _triggerVolumes),
				CutsceneFlags = ReadObjects(reader, values, _cutsceneFlags),
				CutsceneCameraPoints = ReadObjects(reader, values, _cutsceneCameraPoints),
				CutsceneTitles = ReadObjects(reader, values, _cutsceneTitles),
				DeviceGroups = ReadObjects(reader, values, _deviceGroups),
				AISquads = ReadObjects(reader, values, _aiSquads),
				StartingProfiles = ReadObjects(reader, values, _startingProfiles),
				ObjectFolders = ReadObjects(reader, values, _objectFolders),
				AISquadSingleLocations = _aiSquadSingleLocations,
			};
		}

		private StructureValueCollection LoadTag(IReader reader)
		{
			reader.SeekTo(_scenarioTag.MetaLocation.AsOffset());
			return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("scnr"));
		}
		private StructureValueCollection LoadScriptTag(IReader reader)
		{
			reader.SeekTo(_scriptTag.MetaLocation.AsOffset());
			return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("hsdt"));
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
			var count = (int) values.GetInteger("number of script expressions");
			uint address = (uint)values.GetInteger("script expression table address");

			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("script expression element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);

			var result = new ScriptExpressionTable();
			result.AddExpressions(entries.Select((e, i) => new ScriptExpression(e, (ushort) i, stringReader)));

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
			_aiSquads = new ScriptObjectTagBlock("number of ai squads", "ai squads table address", "ai squad element");
			_aiSquadSingleLocations = new ScriptObjectTagBlock("number of single locations", "single locations table address",
				"ai squad single location element");
			_startingProfiles = new ScriptObjectTagBlock("number of starting profiles", "starting profiles table address",
				"starting profile element");
			_objectFolders = new ScriptObjectTagBlock("number of object folders", "object folders table address",
				"object folder element");

			_aiSquads.RegisterChild(_aiSquadSingleLocations);
		}

		private ScriptObject[] ReadObjects(IReader reader, StructureValueCollection values, ScriptObjectTagBlock block)
		{
			return block.ReadObjects(values, reader, _metaArea, _stringIDs, _buildInfo, _expander);
		}

	}
}