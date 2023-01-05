using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Scripting.Compiler;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Util;
using Blamite.Blam.Scripting.Context;

namespace Blamite.Blam.Scripting
{
	public class ScnrScriptFile : IScriptFile
	{
        private readonly ITag _scnrTag;
        private readonly FileSegmentGroup _metaArea;
        private readonly EngineDescription _buildInfo;
		private readonly StringIDSource _stringIDs;
        private readonly IPointerExpander _expander;
        private readonly MetaAllocator _allocator;

        public ScnrScriptFile(ITag scnrTag, string tagName, FileSegmentGroup metaArea, EngineDescription buildInfo, StringIDSource stringIDs, IPointerExpander expander, MetaAllocator allocator)
        {
            if (CharConstant.ToString(scnrTag.Group.Magic) != "scnr")
            {
                throw new ArgumentException("Invalid tag. The tag must belong to the scnr group.");
            }

            _scnrTag = scnrTag;
            Name = tagName.Substring(tagName.LastIndexOf('\\') + 1) + ".hsc"; ;
            _metaArea = metaArea;
            _buildInfo = buildInfo;
            _stringIDs = stringIDs;
            _expander = expander;
            _allocator = allocator;
        }

        public string Name { get; private set; }

		public string Text
		{
			// Not stored in scenario scripts :(
			get { return null; }
		}

		public ScriptTable LoadScripts(IReader reader)
		{
            StructureValueCollection values = LoadScriptTag(reader, _scnrTag);
                
            ulong strSize = values.GetInteger("script string table size");
			var result = new ScriptTable();
			var stringReader = new StringTableReader();
				
			result.Scripts = LoadScripts(reader, values);
			result.Globals = LoadGlobals(reader, values);

            if (values.HasInteger("script syntax table size"))
                result.Expressions = LoadSyntax(reader, values, stringReader);
            else
                result.Expressions = LoadExpressions(reader, values, stringReader);

            CachedStringTable strings = LoadStrings(reader, values, stringReader);
			foreach (ScriptExpression expr in result.Expressions.Where(e => (e != null)))
            {
                expr.ResolveStrings(strings);
            }

            return result;
		}

		public void SaveScripts(ScriptData data, IStream stream, IProgress<int> progress)
		{
            progress.Report(0);
            StructureValueCollection values = LoadScriptTag(stream, _scnrTag);
            StructureLayout scnrLayout = _buildInfo.Layouts.GetLayout("scnr");
            progress.Report(10);

            WriteExpressions(data, stream, values);
            progress.Report(40);

            WriteStrings(data, stream, values);
            progress.Report(50);

            WriteGlobals(data, stream, values);
            progress.Report(60);

            WriteTagReferences(data, stream, values);
            progress.Report(70);

            WriteScripts(data, stream, values);
            progress.Report(90);

            stream.SeekTo(_scnrTag.MetaLocation.AsOffset());
            StructureWriter.WriteStructure(values, scnrLayout, stream);
            progress.Report(100);
        }

        public ScriptingContextCollection LoadContext(IReader reader, ICacheFile cache)
        {
            return ScriptingContextGenerator.GenerateContext(reader, cache, _buildInfo);
        }

        private StructureValueCollection LoadScriptTag(IReader reader, ITag tag)
        {
            reader.SeekTo(tag.MetaLocation.AsOffset());
            return StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("scnr"));
        }

        private List<ScriptGlobal> LoadGlobals(IReader reader, StructureValueCollection values)
		{
			int count = (int) values.GetInteger("number of script globals");
			uint address = (uint)values.GetInteger("script global table address");
			long expand = _expander.Expand(address);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("script global element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			return entries.Select(e => new ScriptGlobal(e, _stringIDs)).ToList();
		}

		private List<Script> LoadScripts(IReader reader, StructureValueCollection values)
		{
			int count = (int) values.GetInteger("number of scripts");
			uint address = (uint)values.GetInteger("script table address");
			long expand = _expander.Expand(address);
			StructureLayout layout = _buildInfo.Layouts.GetLayout("script element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, _metaArea);
			var result = entries.Select(e => new Script(e, reader, _metaArea, _stringIDs, _buildInfo, _expander)).ToList();
            return result;
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

        private ScriptExpressionTable LoadSyntax(IReader reader, StructureValueCollection values,
            StringTableReader stringReader)
        {
            var stringsSize = (int)values.GetInteger("script string table size");

            var tableSize = (int)values.GetInteger("script syntax table size");
            uint tableAddress = (uint)values.GetInteger("script syntax table address");
            long expand = _expander.Expand(tableAddress);

            StructureLayout headerLayout = _buildInfo.Layouts.GetLayout("script syntax table header");
            StructureValueCollection[] headerEntry = TagBlockReader.ReadTagBlock(reader, 1, expand, headerLayout, _metaArea);

            int count = (int)headerEntry[0].GetInteger("element count");

            StructureLayout layout = _buildInfo.Layouts.GetLayout("script syntax table element");
            StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand + headerLayout.Size, layout, _metaArea);

            var result = new ScriptExpressionTable();
            result.AddExpressions(entries.Select((e, i) => new ScriptExpression(e, (ushort)i, stringReader, stringsSize)));

            foreach (ScriptExpression expr in result.Where(expr => expr != null))
                expr.ResolveReferences(result);

            return result;
        }

        private CachedStringTable LoadStrings(IReader reader, StructureValueCollection values, StringTableReader stringReader)
		{
			var stringsSize = (int) values.GetInteger("script string table size");
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


        /// <summary>
        ///     Frees the old Script Parameters and writes new ones to the stream.
        /// </summary>
        /// <param name="data">The new script data</param>
        /// <param name="stream">The stream to write to</param>
        /// <param name="scnr">scnr Meta Data</param>
        /// <returns>A dictionary containing the hashes of parameter groups an their new addresses.</returns>
        private Dictionary<int, uint> WriteParams(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            StructureLayout scrlayout = _buildInfo.Layouts.GetLayout("script element");
            StructureLayout paramlayout = _buildInfo.Layouts.GetLayout("script parameter element");

            int oldScriptCount = (int)scnr.GetInteger("number of scripts");
            uint oldScriptAddress = (uint)scnr.GetInteger("script table address");
            long expOldScriptAddress = _expander.Expand(oldScriptAddress);
            uint oldParamSize = 0;

            StructureValueCollection[] oldScripts = TagBlockReader.ReadTagBlock(stream, oldScriptCount, expOldScriptAddress, scrlayout, _metaArea);

            HashSet<uint> freedParamAddresses = new HashSet<uint>();

            int oldTotalParamCount = 0;

            // loop through old scripts
            for (int i = 0; i < oldScripts.Length; i++)
            {
                int paramCount = (int)oldScripts[i].GetInteger("number of parameters");
                uint addr = (uint)oldScripts[i].GetInteger("address of parameter list");

                oldTotalParamCount += paramCount;

                // free params
                if(addr != 0 && !freedParamAddresses.Contains(addr))
                {
                    long exp = _expander.Expand(addr);
                    uint blockSize = (uint)(paramCount * paramlayout.Size);
                    _allocator.Free(exp, blockSize);
                    oldParamSize += blockSize;
                    freedParamAddresses.Add(addr);                  
                }
            }

            int newTotalParamCount = 0;
            uint newParamSize = 0;
            Dictionary<int, uint> newParamAddresses = new Dictionary<int, uint>();

            // loop through new scripts
            foreach (var scr in data.Scripts)
            {
                int count = scr.Parameters.Count;

                if (count > 0)
                {
                    int paramHash = SequenceHash.GetSequenceHashCode(scr.Parameters);
                    if (!newParamAddresses.ContainsKey(paramHash))
                    {
                        long newAddr = _allocator.Allocate((uint)(paramlayout.Size * count), stream);
                        uint conNewAddr = _expander.Contract(newAddr);
                        stream.SeekTo(_metaArea.PointerToOffset(newAddr));
                        // write params to stream
                        foreach(var par in scr.Parameters)
                        {
                            par.Write(stream);
                        }

                        newParamAddresses.Add(paramHash, conNewAddr);
                        newParamSize += (uint)scr.Parameters.Count * 36;
                    }
                }
                newTotalParamCount += scr.Parameters.Count;
            }

            return newParamAddresses;
        }

        private void WriteScripts(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            ScriptLayout layout;
            if(_buildInfo.Name.Contains("Halo: Reach"))
            {
                layout = ScriptLayout.HaloReach;
            }
            else if(_buildInfo.Name.Contains("Halo 3") || _buildInfo.Name.Contains("Halo ODST"))
            {
                layout = ScriptLayout.Halo3;
            }
            else
            {
                throw new NotImplementedException("Saving this game's scripts is not supported yet.");
            }

            Dictionary<int, uint> newParamAddresses = WriteParams(data, stream, scnr);
            StructureLayout scrlayout = _buildInfo.Layouts.GetLayout("script element");

            int oldScriptCount = (int)scnr.GetInteger("number of scripts");
            int newScriptCount = data.Scripts.Count;

            uint oldScriptAddress = (uint)scnr.GetInteger("script table address");
            long expOldScriptAddress = _expander.Expand(oldScriptAddress);
            long newScriptAddress = 0;

            uint oldScriptSize = (uint)(oldScriptCount * scrlayout.Size);
            uint newScriptSize = (uint)(newScriptCount * scrlayout.Size);

            // get new Script Address
            if(newScriptCount > 0)
            {
                if(oldScriptCount > 0)
                {
                    newScriptAddress = _allocator.Reallocate(expOldScriptAddress, oldScriptSize, newScriptSize, stream);
                }
                else
                {
                    newScriptAddress = _allocator.Allocate(newScriptSize, stream);
                }


                stream.SeekTo(_metaArea.PointerToOffset(newScriptAddress));

                // write scripts
                foreach (var scr in data.Scripts)
                {
                    int paramCount = scr.Parameters.Count;

                    if (paramCount > 0)
                    {
                        int hash = SequenceHash.GetSequenceHashCode(scr.Parameters);

                        if (newParamAddresses.TryGetValue(hash, out uint addr))
                        {
                            scr.Write(stream, _stringIDs, paramCount, addr, layout);
                        }
                        else
                        {
                            throw new KeyNotFoundException("Failed to retrieve the address for a new parameter block.");
                        }

                    }
                    else
                    {
                        scr.Write(stream, _stringIDs, 0, 0, layout);
                    }
                }
            }
            else
            {
                if(oldScriptCount > 0)
                {
                    _allocator.Free(expOldScriptAddress, oldScriptSize);
                }
            }

            scnr.SetInteger("number of scripts", (uint)data.Scripts.Count);
            scnr.SetInteger("script table address", _expander.Contract(newScriptAddress));
        }

        private void WriteStrings(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            uint oldStringsSize = (uint)scnr.GetInteger("script string table size");
            uint oldStringAddress = (uint)scnr.GetInteger("script string table address");
            long expOldStringAddress = _expander.Expand(oldStringAddress);
            long newStringsAddress = 0;

            // Check if the returned data contained strings
            if (data.Strings.Size > 0)
            {
                // allocate space for the string table
                if (oldStringsSize > 0)
                {
                    newStringsAddress = _allocator.Reallocate(expOldStringAddress, oldStringsSize, (uint)data.Strings.Size, stream);
                }
                else
                {
                    newStringsAddress = _allocator.Allocate((uint)data.Strings.Size, stream);
                }
                // write strings
                stream.SeekTo(_metaArea.PointerToOffset(newStringsAddress));
                data.Strings.Write(stream);
            }
            // free the old string table if the new scripts didn't contain strings (impossible)
            else if (oldStringsSize > 0)
            {
                _allocator.Free(expOldStringAddress, oldStringsSize);
            }

            scnr.SetInteger("script string table size", (uint)data.Strings.Size);
            scnr.SetInteger("script string table address", _expander.Contract(newStringsAddress));
        }

        private void WriteGlobals(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            StructureLayout glolayout = _buildInfo.Layouts.GetLayout("script global element");
            int oldGlobalCount = (int)scnr.GetInteger("number of script globals");
            uint oldGlobalAddress = (uint)scnr.GetInteger("script global table address");
            long expOldGlobalAddress = _expander.Expand(oldGlobalAddress);
            uint oldGlobalSize = (uint)(oldGlobalCount * glolayout.Size);
            long newGlobalAddress = 0;

            // check if the returned data contains globals
            if (data.Globals.Count > 0)
            {
                uint newGlobalSize = (uint)(data.Globals.Count * glolayout.Size);

                // allocate space for the globals
                if (oldGlobalCount > 0)
                {
                    newGlobalAddress = _allocator.Reallocate(expOldGlobalAddress, oldGlobalSize, newGlobalSize, stream);
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
            int newExpCount = data.Expressions.Count;
            uint oldExpressionAddress = (uint)scnr.GetInteger("script expression table address");
            long oldExpandedAddress = _expander.Expand(oldExpressionAddress);
            uint oldExpSize = (uint)(oldExpCount * expLayout.Size);
            long newExpAddress = 0;

            if (newExpCount > 0)
            {
                uint newExpSize = (uint)(newExpCount * expLayout.Size);

                // allocate space for the expressions
                if (oldExpCount > 0)
                {
                    newExpAddress = _allocator.Reallocate(oldExpandedAddress, oldExpSize, newExpSize, stream);
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
                _allocator.Free(oldExpandedAddress, oldExpSize);
            }

            scnr.SetInteger("number of script expressions", (ulong)newExpCount);
            scnr.SetInteger("script expression table address", _expander.Contract(newExpAddress));
        }

        private void WriteTagReferences(ScriptData data, IStream stream, StructureValueCollection scnr)
        {
            int oldRefCount = (int)scnr.GetInteger("number of script tag references");
            uint oldRefAddress = (uint)scnr.GetInteger("script tag references table address");
            long expOldRefAddress = _expander.Expand(oldRefAddress);
            uint oldRefSize = (uint)oldRefCount * 0x10;
            long newRefAddress = 0;

            //check if the returned data contained tag references
            if (data.TagReferences.Count > 0)
            {
                uint newRefSize = (uint)data.TagReferences.Count * 0x10;

                // allocate space for the references
                if (oldRefCount > 0)
                {
                    newRefAddress = _allocator.Reallocate(expOldRefAddress, oldRefSize, newRefSize, stream);
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
                _allocator.Free(expOldRefAddress, oldRefSize);
            }

            scnr.SetInteger("number of script tag references", (uint)data.TagReferences.Count);
            scnr.SetInteger("script tag references table address", _expander.Contract(newRefAddress));
        }
    }
}