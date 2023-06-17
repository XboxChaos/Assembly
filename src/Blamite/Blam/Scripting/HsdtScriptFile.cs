using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Scripting.Compiler;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Blam.Scripting.Context;
using Blamite.Util;

namespace Blamite.Blam.Scripting
{
    public class HsdtScriptFile : IScriptFile
    {
		private readonly ITag _hsdtTag;
		private readonly FileSegmentGroup _metaArea;
		private readonly EngineDescription _buildInfo;
		private readonly StringIDSource _stringIDs;
		private readonly IPointerExpander _expander;

        public HsdtScriptFile(ITag hsdtTag, string tagName, FileSegmentGroup metaArea, EngineDescription buildInfo, StringIDSource stringIDs, IPointerExpander expander)
        {
			if(CharConstant.ToString(hsdtTag.Group.Magic) != "hsdt")
            {
				throw new ArgumentException("Invalid tag. The tag must belong to the hsdt group.");
            }

            _hsdtTag = hsdtTag;
			Name = tagName.Substring(tagName.LastIndexOf('\\') + 1) + ".hsc";
			_metaArea = metaArea;
            _buildInfo = buildInfo;
            _stringIDs = stringIDs;
            _expander = expander;
		}


        public string Name { get; private set; }

		public string Text
		{
			// Not stored in scenario scripts :(
			get { return null; }
		}


		public ScriptTable LoadScripts(IReader reader)
		{
			StructureValueCollection values = LoadScriptTag(reader, _hsdtTag);

			ulong strSize = values.GetInteger("script string table size");
			var result = new ScriptTable();
			var stringReader = new StringTableReader();

			result.Scripts = LoadScripts(reader, values);
			result.Globals = LoadGlobals(reader, values);
			result.Variables = LoadVariables(reader, values);
			result.Expressions = LoadExpressions(reader, values, stringReader);

			CachedStringTable strings = LoadStrings(reader, values, stringReader);
			foreach (ScriptExpression expr in result.Expressions.Where(e => (e != null)))
			{
				expr.ResolveStrings(strings);
			}

			return result;
		}


		// TODO: Implement script saving for hsdt based script files.
		public void SaveScripts(ScriptData scripts, IStream stream, IProgress<int> progress)
        {
        }


		// TODO: Implement context loading for hsdt based script files.
		public ScriptingContextCollection LoadContext(IReader reader, ICacheFile cache)
		{
			return new ScriptingContextCollection();
		}


		private StructureValueCollection LoadScriptTag(IReader reader, ITag tag)
		{
			reader.SeekTo(tag.MetaLocation.AsOffset());
			return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("hsdt"));
		}

		private List<ScriptGlobal> LoadGlobals(IReader reader, StructureValueCollection values)
		{
			int count = (int)values.GetInteger("number of script globals");
			uint address = (uint)values.GetInteger("script global table address");
			long expand = _expander.Expand(address);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("script global element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			return entries.Select(e => new ScriptGlobal(e, _stringIDs)).ToList();
		}

		private List<ScriptGlobal> LoadVariables(IReader reader, StructureValueCollection values)
		{
			int count = (int)values.GetInteger("number of script variables");
			uint address = (uint)values.GetInteger("script variable table address");
			long expand = _expander.Expand(address);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("script variable element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			return entries.Select(e => new ScriptGlobal(e, _stringIDs)).ToList();
		}

		private List<Script> LoadScripts(IReader reader, StructureValueCollection values)
		{
			int count = (int)values.GetInteger("number of scripts");
			uint address = (uint)values.GetInteger("script table address");
			long expand = _expander.Expand(address);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("script element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			return entries.Select(e => new Script(e, reader, _metaArea, _stringIDs, _buildInfo, _expander)).ToList();
		}

		private ScriptExpressionTable LoadExpressions(IReader reader, StructureValueCollection values,
			StringTableReader stringReader)
		{
			int stringsSize = (int)values.GetInteger("script string table size");
			int count = (int)values.GetInteger("number of script expressions");
			uint address = (uint)values.GetInteger("script expression table address");
			long expand = _expander.Expand(address);

			StructureLayout layout = _buildInfo.Layouts.GetLayout("script expression element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);

			var result = new ScriptExpressionTable();
			result.AddExpressions(entries.Select((e, i) => new ScriptExpression(e, (ushort)i, stringReader, stringsSize)));

			foreach (ScriptExpression expr in result.Where(expr => expr != null))
            {
				expr.ResolveReferences(result);
			}

			return result;
		}

		private CachedStringTable LoadStrings(IReader reader, StructureValueCollection values, StringTableReader stringReader)
		{
			int stringsSize = (int)values.GetInteger("script string table size");
			if (stringsSize == 0)
            {
				return new CachedStringTable();
			}

			uint tableAddr = (uint)values.GetInteger("script string table address");
			long expand = _expander.Expand(tableAddr);
			uint tableOffset = _metaArea.PointerToOffset(expand);

			var result = new CachedStringTable();
			stringReader.ReadRequestedStrings(reader, tableOffset, result);
			return result;
		}
	}
}
