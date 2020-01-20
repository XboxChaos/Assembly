using System.Collections.Generic;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Structures
{
	public class ThirdGenTagInterop : ITagInterop
	{

		public ThirdGenTagInterop(uint pointer, int type)
		{
			Pointer = pointer;
			Type = type;
			
		}

		public ThirdGenTagInterop(StructureValueCollection values, FileSegmentGroup metaArea)
		{
			Load(values, metaArea);
		}

		public uint Pointer { get; set; }
		public int Type { get; set; }

		public StructureValueCollection Serialize()
		{
			var result = new StructureValueCollection();
			result.SetInteger("pointer", Pointer);
			result.SetInteger("type", (uint)Type);
			return result;
		}

		private void Load(StructureValueCollection values, FileSegmentGroup metaArea)
		{
			Pointer = (uint)values.GetInteger("pointer");

			Type = (int)values.GetInteger("type");
		}

	}
}
