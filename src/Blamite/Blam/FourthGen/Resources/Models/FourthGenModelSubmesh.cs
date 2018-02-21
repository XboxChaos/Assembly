﻿using Blamite.Blam.Resources.Models;
using Blamite.Serialization;

namespace Blamite.Blam.FourthGen.Resources.Models
{
	public class FourthGenModelSubmesh : IModelSubmesh
	{
		public FourthGenModelSubmesh(StructureValueCollection values)
		{
			Load(values);
		}

		public int ShaderIndex { get; private set; }

		public int IndexBufferStart { get; private set; }

		public int IndexBufferCount { get; private set; }

		public int VertexGroupStart { get; private set; }

		public int VertexGroupCount { get; private set; }

		public int VertexBufferCount { get; private set; }

		private void Load(StructureValueCollection values)
		{
			ShaderIndex = (int) values.GetInteger("shader index");
			IndexBufferStart = (int) values.GetInteger("index buffer start");
			IndexBufferCount = (int) values.GetInteger("index buffer count");
			VertexGroupStart = (int) values.GetInteger("vertex group start");
			VertexGroupCount = (int) values.GetInteger("vertex group count");
			VertexBufferCount = (int) values.GetInteger("vertex buffer count");
		}
	}
}