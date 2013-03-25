using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Blamite.Blam.Scripting;
using Blamite.Blam.Scripting.Compiler;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Structures
{
    public class ThirdGenScenarioScriptFile : IScriptFile
    {
        private ITag _tag;
        private StringIDSource _stringIDs;
        private FileSegmentGroup _metaArea;
        private BuildInformation _buildInfo;

        public ThirdGenScenarioScriptFile(ITag scenarioTag, string scenarioName, FileSegmentGroup metaArea, StringIDSource stringIDs, BuildInformation buildInfo)
        {
            _tag = scenarioTag;
            _stringIDs = stringIDs;
            _metaArea = metaArea;
            _buildInfo = buildInfo;
            Name = scenarioName.Substring(scenarioName.LastIndexOf('\\') + 1) + ".hsc";
        }

        public string Name { get; private set; }

        public string Text
        {
            // Not stored in scenario scripts :(
            get { return null; }
        }

        public ScriptTable LoadScripts(IReader reader)
        {
            var values = LoadTag(reader);

            ScriptTable result = new ScriptTable();
            StringTableReader stringReader = new StringTableReader();
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
            throw new NotImplementedException();
        }

        private StructureValueCollection LoadTag(IReader reader)
        {
            reader.SeekTo(_tag.MetaLocation.AsOffset());
            return StructureReader.ReadStructure(reader, _buildInfo.GetLayout("scnr"));
        }

        private List<ScriptGlobal> LoadGlobals(IReader reader, StructureValueCollection values)
        {
            int count = (int)values.GetInteger("number of script globals");
            uint address = values.GetInteger("script global table address");
            var layout = _buildInfo.GetLayout("script global entry");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
            return entries.Select(e => new ScriptGlobal(e)).ToList();
        }

        private List<Script> LoadScripts(IReader reader, StructureValueCollection values)
        {
            int count = (int)values.GetInteger("number of scripts");
            uint address = values.GetInteger("script table address");
            var layout = _buildInfo.GetLayout("script entry");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);
            return entries.Select(e => new Script(e, reader, _metaArea, _stringIDs, _buildInfo)).ToList();
        }

        private ScriptExpressionTable LoadExpressions(IReader reader, StructureValueCollection values, StringTableReader stringReader)
        {
            int count = (int)values.GetInteger("number of script expressions");
            uint address = values.GetInteger("script expression table address");
            var layout = _buildInfo.GetLayout("script expression entry");
            var entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, _metaArea);

            var result = new ScriptExpressionTable();
            result.AddExpressions(entries.Select((e, i) => new ScriptExpression(e, (ushort)i, stringReader)));

            foreach (var expr in result.Where(expr => expr != null))
                expr.ResolveReferences(result);

            return result;
        }

        private CachedStringTable LoadStrings(IReader reader, StructureValueCollection values, StringTableReader stringReader)
        {
            var stringsSize = (int)values.GetInteger("script string table size");
            if (stringsSize == 0)
                return new CachedStringTable();

            var result = new CachedStringTable();
            var tableOffset = _metaArea.PointerToOffset(values.GetInteger("script string table address"));
            stringReader.ReadRequestedStrings(reader, tableOffset, result);
            return result;
        }
    }
}
