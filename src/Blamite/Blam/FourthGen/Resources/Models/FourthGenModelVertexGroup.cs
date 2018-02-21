﻿using Blamite.Blam.Resources.Models;
using Blamite.Serialization;

namespace Blamite.Blam.FourthGen.Resources.Models
{
	public class FourthGenModelVertexGroup : IModelVertexGroup
	{
		public FourthGenModelVertexGroup(StructureValueCollection values, IModelSubmesh[] submeshes)
		{
			Load(values, submeshes);
		}

		public int IndexBufferStart { get; private set; }

		public int IndexBufferCount { get; private set; }

		public int VertexBufferCount { get; private set; }

		public IModelSubmesh Submesh { get; private set; }

		private void Load(StructureValueCollection values, IModelSubmesh[] submeshes)
		{
			IndexBufferStart = (int) values.GetInteger("index buffer start");
			IndexBufferCount = (int) values.GetInteger("index buffer count");
			VertexBufferCount = (int) values.GetInteger("vertex buffer count");

			var submeshIndex = (int) values.GetInteger("parent submesh index");
			Submesh = submeshes[submeshIndex];
		}
	}
}