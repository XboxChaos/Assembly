﻿using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.Scripting
{
	public enum ScriptLayout
	{
		Halo3,
		HaloReach
	}

	/// <summary>
	///     A script.
	/// </summary>
	public class Script
	{
		public Script()
		{
			Parameters = new List<ScriptParameter>();
		}

		internal Script(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, StringIDSource stringIDs,
			EngineDescription buildInfo, IPointerExpander expander)
		{
			Load(values, reader, metaArea, stringIDs, buildInfo, expander);
		}

		/// <summary>
		///     Gets or sets the script's name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		///     Gets a list of parameters that the script accepts.
		/// </summary>
		public IList<ScriptParameter> Parameters { get; private set; }

		/// <summary>
		///     Gets or sets the execution type opcode (static, startup, dormant, etc.) of the script.
		/// </summary>
		public short ExecutionType { get; set; }

		/// <summary>
		///     Gets or sets the script's return type.
		/// </summary>
		public short ReturnType { get; set; }

		/// <summary>
		///     Gets or sets the datum index of the first expression to execute in the script.
		/// </summary>
		public DatumIndex RootExpressionIndex { get; set; }

        public void Write(IWriter writer, StringIDSource stringIDs, int parameterCount, uint parameterAddress, ScriptLayout layout)
        {
			if(layout == ScriptLayout.HaloReach)
            {
				writer.WriteUInt32(stringIDs.FindOrAddStringID(Name).Value);
			}
			else
            {
				writer.WriteAscii(Name, 0x20);
            }
            writer.WriteInt16(ExecutionType);
            writer.WriteInt16(ReturnType);
            writer.WriteUInt32(RootExpressionIndex.Value);
            writer.WriteInt32(parameterCount);
            writer.WriteUInt32(parameterAddress);
            writer.WriteUInt32(0);
        }

		private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, StringIDSource stringIDs,
			EngineDescription buildInfo, IPointerExpander expander)
		{
			Name = values.HasInteger("name index")
				? stringIDs.GetString(new StringID(values.GetInteger("name index")))
				: values.GetString("name");
			ExecutionType = (short) values.GetInteger("execution type");
			ReturnType = (short) values.GetInteger("return type");
			RootExpressionIndex = new DatumIndex(values.GetInteger("first expression index"));
			if (Name == null)
				Name = "script_" + RootExpressionIndex.Value.ToString("X8");

			if (buildInfo.Layouts.HasLayout("script parameter element"))
				Parameters = LoadParameters(reader, values, metaArea, buildInfo, expander);
		}

		private IList<ScriptParameter> LoadParameters(IReader reader, StructureValueCollection values,
			FileSegmentGroup metaArea, EngineDescription buildInfo, IPointerExpander expander)
		{
			var count = (int) values.GetInteger("number of parameters");
			uint address = (uint)values.GetInteger("address of parameter list");

			long expand = expander.Expand(address);

			StructureLayout layout = buildInfo.Layouts.GetLayout("script parameter element");
			StructureValueCollection[] entries = TagBlockReader.ReadTagBlock(reader, count, expand, layout, metaArea);
			return entries.Select(e => new ScriptParameter(e)).ToList();
		}
	}
}