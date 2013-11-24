using System.Linq;
using Blamite.Blam.Resources.Models;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Resources.Models
{
	public class ThirdGenModelSection : IModelSection
	{
		public ThirdGenModelSection(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			Load(values, reader, metaArea, buildInfo);
		}

		public IModelSubmesh[] Submeshes { get; private set; }

		public IModelVertexGroup[] VertexGroups { get; private set; }

		public int VertexFormat { get; private set; }

		public int ExtraElementsPerVertex { get; private set; }

		public ExtraVertexElementType ExtraElementsType { get; private set; }

		private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			VertexFormat = (int) values.GetInteger("vertex format");
			ExtraElementsPerVertex = (int) values.GetInteger("extra elements per vertex");
			ExtraElementsType = (ExtraVertexElementType) values.GetInteger("extra element type");

			LoadSubmeshes(values, reader, metaArea, buildInfo);
			LoadVertexGroups(values, reader, metaArea, buildInfo, Submeshes);
		}

		private void LoadSubmeshes(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			var count = (int) values.GetInteger("number of submeshes");
			uint address = values.GetInteger("submesh table address");
			StructureLayout layout = buildInfo.Layouts.GetLayout("model submesh");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

			Submeshes = (from entry in entries
				select new ThirdGenModelSubmesh(entry)).ToArray();
		}

		private void LoadVertexGroups(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo, IModelSubmesh[] submeshes)
		{
			var count = (int) values.GetInteger("number of vertex groups");
			uint address = values.GetInteger("vertex group table address");
			StructureLayout layout = buildInfo.Layouts.GetLayout("model vertex group");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

			VertexGroups = (from entry in entries
				select new ThirdGenModelVertexGroup(entry, submeshes)).ToArray();
		}
	}
}