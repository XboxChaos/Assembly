using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.Scripting.Compiler
{
	/// <summary>
	///     A reflexive which named script objects can be read from.
	/// </summary>
	public class ScriptObjectReflexive
	{
		private readonly string _addressEntryName;
		private readonly List<ScriptObjectReflexive> _children = new List<ScriptObjectReflexive>();
		private readonly string _countEntryName;
		private readonly string _layoutName;

		/// <summary>
		///     Initializes a new instance of the <see cref="ScriptObjectReflexive" /> class.
		/// </summary>
		/// <param name="countEntryName">Name of the count entry in the parent's layout.</param>
		/// <param name="addressEntryName">Name of the address entry in the parent's layout.</param>
		/// <param name="layoutName">Name of the layout of this reflexive.</param>
		public ScriptObjectReflexive(string countEntryName, string addressEntryName, string layoutName)
		{
			_countEntryName = countEntryName;
			_addressEntryName = addressEntryName;
			_layoutName = layoutName;
		}

		/// <summary>
		///     Registers a child reflexive with this reflexive.
		/// </summary>
		/// <param name="child">The child reflexive to register.</param>
		public void RegisterChild(ScriptObjectReflexive child)
		{
			_children.Add(child);
		}

		/// <summary>
		///     Reads all child objects of this reflexive.
		/// </summary>
		/// <param name="values">The values read from the parent.</param>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="metaArea">The meta area of the cache file.</param>
		/// <param name="stringIDs">The string ID source for the cache file.</param>
		/// <param name="buildInfo">The build info for the cache file.</param>
		/// <returns>The objects that were read.</returns>
		public ScriptObject[] ReadObjects(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			StringIDSource stringIDs, EngineDescription buildInfo)
		{
			var count = (int) values.GetInteger(_countEntryName);
			uint address = values.GetInteger(_addressEntryName);
			StructureLayout layout = buildInfo.Layouts.GetLayout(_layoutName);
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);
			return entries.Select(e => ReadScriptObject(e, reader, metaArea, stringIDs, buildInfo)).ToArray();
		}

		private ScriptObject ReadScriptObject(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			StringIDSource stringIDs, EngineDescription buildInfo)
		{
			string name = GetObjectName(values, stringIDs);
			var result = new ScriptObject(name);

			foreach (ScriptObjectReflexive child in _children)
				result.RegisterChildren(child, child.ReadObjects(values, reader, metaArea, stringIDs, buildInfo));

			return result;
		}

		private string GetObjectName(StructureValueCollection values, StringIDSource stringIDs)
		{
			if (values.HasString("name"))
			{
				return values.GetString("name");
			}
			if (values.HasInteger("name stringid"))
			{
				var sid = new StringID(values.GetInteger("name stringid"));
				return stringIDs.GetString(sid);
			}
			throw new InvalidOperationException("Unable to determine the name of objects in the \"" + _layoutName + "\" layout");
		}
	}
}