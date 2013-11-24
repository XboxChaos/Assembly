using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Scripting;
using Blamite.Blam.Scripting.Compiler;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Structures
{
	public class ThirdGenScenarioScriptFile : IScriptFile
	{
		private readonly EngineDescription _buildInfo;
		private readonly FileSegmentGroup _metaArea;
		private readonly StringIDSource _stringIDs;
		private readonly ITag _tag;
		private ScriptObjectReflexive _aiObjectWaves;
		private ScriptObjectReflexive _aiObjects;
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
		private ScriptObjectReflexive _referencedObjects;
		private ScriptObjectReflexive _startingProfiles;
		private ScriptObjectReflexive _triggerVolumes;
		private ScriptObjectReflexive _zoneSets;

		public ThirdGenScenarioScriptFile(ITag scenarioTag, string scenarioName, FileSegmentGroup metaArea,
			StringIDSource stringIDs, EngineDescription buildInfo)
		{
			_tag = scenarioTag;
			_stringIDs = stringIDs;
			_metaArea = metaArea;
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
			StructureValueCollection values = LoadTag(reader);

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
				AISquadGroups = ReadObjects(reader, values, _aiSquadGroups),
				AISquads = ReadObjects(reader, values, _aiSquads),
				AIObjects = ReadObjects(reader, values, _aiObjects),
				StartingProfiles = ReadObjects(reader, values, _startingProfiles),
				ZoneSets = ReadObjects(reader, values, _zoneSets),
				ObjectFolders = ReadObjects(reader, values, _objectFolders),
				PointSets = ReadPointSets(reader, values),
				AISquadSingleLocations = _aiSquadSingleLocations,
				AIObjectWaves = _aiObjectWaves,
				PointSetPoints = _pointSetPoints
			};
		}

		private StructureValueCollection LoadTag(IReader reader)
		{
			reader.SeekTo(_tag.MetaLocation.AsOffset());
			return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("scnr"));
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
			var count = (int) values.GetInteger("number of script expressions");
			uint address = values.GetInteger("script expression table address");
			StructureLayout layout = _buildInfo.Layouts.GetLayout("script expression entry");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);

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
			int tableOffset = _metaArea.PointerToOffset(values.GetInteger("script string table address"));
			stringReader.ReadRequestedStrings(reader, tableOffset, result);
			return result;
		}

		private void DefineScriptObjectReflexives()
		{
			_referencedObjects = new ScriptObjectReflexive("number of script objects", "script object table address",
				"script object entry");
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
			_aiObjects = new ScriptObjectReflexive("number of ai objects", "ai objects table address", "ai object entry");
			_aiObjectWaves = new ScriptObjectReflexive("number of waves", "waves table address", "ai object wave entry");
			_startingProfiles = new ScriptObjectReflexive("number of starting profiles", "starting profiles table address",
				"starting profile entry");
			_zoneSets = new ScriptObjectReflexive("number of zone sets", "zone sets table address", "zone set entry");
			_objectFolders = new ScriptObjectReflexive("number of object folders", "object folders table address",
				"object folder entry");
			_pointSets = new ScriptObjectReflexive("number of point sets", "point sets table address", "point set entry");
			_pointSetPoints = new ScriptObjectReflexive("number of points", "points table address", "point set point entry");

			_aiSquads.RegisterChild(_aiSquadSingleLocations);
			_aiObjects.RegisterChild(_aiObjectWaves);
			_pointSets.RegisterChild(_pointSetPoints);
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
	}
}