using System;
using Blamite.Blam.Resources;
using Blamite.Blam.Resources.BSP;
using Blamite.Blam.Resources.Models;
using Blamite.Blam.Resources.Sounds;
using Blamite.Blam.ThirdGen.Resources.BSP;
using Blamite.Blam.ThirdGen.Resources.Models;
using Blamite.Flexibility;
using Blamite.IO;
using Blamite.Util;
using Blamite.Blam.ThirdGen.Resources.Sounds;

namespace Blamite.Blam.ThirdGen.Resources
{
	public class ThirdGenResourceMetaLoader : IResourceMetaLoader
	{
		private static readonly int ModeMagic = CharConstant.FromString("mode");
		private static readonly int SbspMagic = CharConstant.FromString("sbsp");
		private static readonly int SndMagic = CharConstant.FromString("snd!");
		private readonly EngineDescription _buildInfo;
		private readonly FileSegmentGroup _metaArea;

		public ThirdGenResourceMetaLoader(EngineDescription buildInfo, FileSegmentGroup metaArea)
		{
			_buildInfo = buildInfo;
			_metaArea = metaArea;
		}

		public bool SupportsRenderModels
		{
			get { return _buildInfo.Layouts.HasLayout("render model"); }
		}

		public IRenderModel LoadRenderModelMeta(ITag modeTag, IReader reader)
		{
			if (modeTag.MetaLocation == null || modeTag.Class == null || modeTag.Class.Magic != ModeMagic)
				throw new ArgumentException("modeTag does not point to metadata for a renderable model");
			if (!SupportsRenderModels)
				throw new NotSupportedException("Render model metadata loading is not supported for the cache file's engine.");

			reader.SeekTo(modeTag.MetaLocation.AsOffset());
			var layout = _buildInfo.Layouts.GetLayout("render model");
			var values = StructureReader.ReadStructure(reader, layout);
			return new ThirdGenRenderModel(values, reader, _metaArea, _buildInfo);
		}

		public bool SupportsScenarioBsps
		{
			get { return _buildInfo.Layouts.HasLayout("scenario structure bsp"); }
		}

		public IScenarioBSP LoadScenarioBspMeta(ITag sbspTag, IReader reader)
		{
			if (sbspTag.MetaLocation == null || sbspTag.Class == null || sbspTag.Class.Magic != SbspMagic)
				throw new ArgumentException("sbspTag does not point to metadata for a scenario BSP");
			if (!SupportsScenarioBsps)
				throw new NotSupportedException("Scenario BSP metadata loading is not supported for the cache file's engine.");

			reader.SeekTo(sbspTag.MetaLocation.AsOffset());
			var layout = _buildInfo.Layouts.GetLayout("scenario structure bsp");
			var values = StructureReader.ReadStructure(reader, layout);
			return new ThirdGenScenarioBSP(values, reader, _metaArea, _buildInfo);
		}

		public bool SupportsSounds
		{
			get { return _buildInfo.Layouts.HasLayout("sound"); }
		}

		public ISound LoadSoundMeta(ITag sndTag, IReader reader)
		{
			if (sndTag.MetaLocation == null || sndTag.Class == null || sndTag.Class.Magic != SndMagic)
				throw new ArgumentException("sndTag");
			if (!SupportsSounds)
				throw new NotSupportedException("Sound metadata loading is not supported for the cache file's engine.");

			reader.SeekTo(sndTag.MetaLocation.AsOffset());
			var layout = _buildInfo.Layouts.GetLayout("sound");
			var values = StructureReader.ReadStructure(reader, layout);
			return new ThirdGenSound(values, reader, _metaArea, _buildInfo);
		}
	}
}