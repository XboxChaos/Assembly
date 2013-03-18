using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Scripting;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Structures
{
    public class ThirdGenScenarioMeta : IScenario
    {
        public ThirdGenScenarioMeta(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, StringIDSource stringIDs, BuildInformation buildInfo)
        {
            Load(values, reader, metaArea, stringIDs, buildInfo);
        }

        public ScriptExpressionTable ScriptExpressions { get; private set; }
        public List<ScriptGlobal> ScriptGlobals { get; private set; }
        public List<ScriptObjectReference> ScriptObjects { get; private set; }
        public List<Script> Scripts { get; private set; }

        private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, StringIDSource stringIDs, BuildInformation buildInfo)
        {
			var stringReader = new StringTableReader();
            ScriptExpressions = LoadScriptExpressions(values, reader, metaArea, stringReader, buildInfo.GetLayout("script expression entry"));
            ScriptObjects = LoadScriptObjects(values, reader, metaArea, stringIDs, buildInfo.GetLayout("script object entry"));
            ScriptGlobals = LoadScriptGlobals(values, reader, metaArea, ScriptExpressions, buildInfo.GetLayout("script global entry"));
            Scripts = LoadScripts(values, reader, metaArea, stringIDs, ScriptExpressions, buildInfo.GetLayout("script entry"), buildInfo);

			var strings = LoadStrings(values, reader, stringReader, metaArea);
			foreach (var expr in ScriptExpressions.Where(expr => expr != null))
			{
				expr.ResolveStrings(strings);
			}
        }

        private ScriptExpressionTable LoadScriptExpressions(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, StringTableReader stringReader, StructureLayout entryLayout)
        {
			var count = (int)values.GetInteger("number of script expressions");
			var address = values.GetInteger("script expression table address");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, entryLayout, metaArea);

			var result = new ScriptExpressionTable();
            result.AddExpressions(entries.Select((e, i) => new ScriptExpression(e, (ushort)i, stringReader)));

			foreach (var expr in result.Where(expr => expr != null))
				expr.ResolveReferences(result);

            return result;
        }

        private List<ScriptObjectReference> LoadScriptObjects(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, StringIDSource stringIDs, StructureLayout entryLayout)
        {
			var count = (int)values.GetInteger("number of script objects");
			var address = values.GetInteger("script object table address");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, entryLayout, metaArea);
            return entries.Select(e => new ScriptObjectReference(e, stringIDs)).ToList();
        }

        private List<ScriptGlobal> LoadScriptGlobals(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, ScriptExpressionTable expressions, StructureLayout entryLayout)
        {
			var count = (int)values.GetInteger("number of script globals");
			var address = values.GetInteger("script global table address");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, entryLayout, metaArea);
            return entries.Select(e => new ScriptGlobal(e, expressions)).ToList();
        }

        private List<Script> LoadScripts(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, StringIDSource stringIDs, ScriptExpressionTable expressions, StructureLayout entryLayout, BuildInformation buildInfo)
        {
			var count = (int)values.GetInteger("number of scripts");
			var address = values.GetInteger("script table address");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, entryLayout, metaArea);
            return entries.Select(e => new Script(e, reader, metaArea, stringIDs, expressions, buildInfo)).ToList();
        }

        private CachedStringTable LoadStrings(StructureValueCollection values, IReader reader, StringTableReader stringReader, FileSegmentGroup metaArea)
        {
			var stringsSize = (int)values.GetInteger("script string table size");
            if (stringsSize == 0)
                return new CachedStringTable();

            var result = new CachedStringTable();
            var tableOffset = metaArea.PointerToOffset(values.GetInteger("script string table address"));
            stringReader.ReadRequestedStrings(reader, tableOffset, result);
            return result;
        }
    }
}