using System.Linq;
using Blamite.Blam.Resources.BSP;
using Blamite.Blam.Resources.Models;
using Blamite.Blam.ThirdGen.Resources.Models;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Resources.BSP
{
	public class ThirdGenScenarioBSP : IScenarioBSP
	{
		public ThirdGenScenarioBSP(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			Load(values, reader, metaArea, buildInfo);
		}

		public IModelSection[] Sections { get; private set; }

		public BoundingBox[] BoundingBoxes { get; private set; }

		public DatumIndex ModelResourceIndex { get; private set; }

		private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			ModelResourceIndex = new DatumIndex(values.GetInteger("model resource datum index"));

			LoadSections(values, reader, metaArea, buildInfo);
			LoadBoundingBoxes(values, reader, metaArea, buildInfo);
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