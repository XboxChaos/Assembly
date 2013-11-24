using System.Linq;
using Blamite.Blam.Resources.Models;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Resources.Models
{
	public class ThirdGenRenderModel : IRenderModel
	{
		public ThirdGenRenderModel(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			Load(values, reader, metaArea, buildInfo);
		}

		public IModelRegion[] Regions { get; private set; }

		public IModelSection[] Sections { get; private set; }

		public BoundingBox[] BoundingBoxes { get; private set; }

		public DatumIndex ModelResourceIndex { get; private set; }

		private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			ModelResourceIndex = new DatumIndex(values.GetInteger("resource datum index"));

			LoadRegions(values, reader, metaArea, buildInfo);
			LoadSections(values, reader, metaArea, buildInfo);
			LoadBoundingBoxes(values, reader, metaArea, buildInfo);
		}

		private void LoadRegions(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			var count = (int) values.GetInteger("number of regions");
			uint address = values.GetInteger("region table address");
			StructureLayout layout = buildInfo.Layouts.GetLayout("model region");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

			Regions = (from entry in entries
				select new ThirdGenModelRegion(entry, reader, metaArea, buildInfo)).ToArray();
		}

		private void LoadSections(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			var count = (int) values.GetInteger("number of sections");
			uint address = values.GetInteger("section table address");
			StructureLayout layout = buildInfo.Layouts.GetLayout("model section");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, count, address, layout, metaArea);

			Sections = (from entry in entries
				select new ThirdGenModelSection(entry, reader, metaArea, buildInfo)).ToArray();
		}

		private void LoadBoundingBoxes(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			var count = (int) values.GetInteger("number of bounding boxes");
			uint address = values.GetInteger("bounding box table address");
			StructureLayout layout = buildInfo.Layouts.GetLayout("model bounding box");
			StructureValueCollection[] entries = ReflexiveReader.ReadReflexive(reader, 1, address, layout, metaArea);

			BoundingBoxes = (from entry in entries
				select BoundingBox.Deserialize(entry)).ToArray();
		}
	}
}