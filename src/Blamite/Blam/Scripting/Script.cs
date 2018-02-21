﻿using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.Scripting
{
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
			EngineDescription buildInfo, int headeraddr)
		{
			Load(values, reader, metaArea, stringIDs, buildInfo, headeraddr);
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

		private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, StringIDSource stringIDs,
			EngineDescription buildInfo, int headeraddr)
		{
			Name = values.HasInteger("name index")
				? stringIDs.GetString(new StringID(values.GetInteger("name index")))
				: values.GetString("name");
			ExecutionType = (short) values.GetInteger("execution type");
			ReturnType = (short) values.GetInteger("return type");
			RootExpressionIndex = new DatumIndex(values.GetInteger("first expression index"));
			if (Name == null)
				Name = "script_" + RootExpressionIndex.Value.ToString("X8");

			Parameters = LoadParameters(reader, values, metaArea, buildInfo, headeraddr);
		}

		private IList<ScriptParameter> LoadParameters(IReader reader, StructureValueCollection values,
			FileSegmentGroup metaArea, EngineDescription buildInfo, int addrFix)
		{
			var count = (int) values.GetInteger("number of parameters");
			uint address = values.GetInteger("address of parameter list");
			StructureLayout layout = buildInfo.Layouts.GetLayout("script parameter entry");

			if (addrFix != 0)
				address = (uint)addrFix + (address & 0xFFFFFFF);
			
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);
			return entries.Select(e => new ScriptParameter(e)).ToList();
		}
	}
}