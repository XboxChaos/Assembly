using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Resources;
using ExtryzeDLL.Blam.Resources.Models;
using ExtryzeDLL.Blam.ThirdGen.Resources.Models;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;
using ExtryzeDLL.Util;

namespace ExtryzeDLL.Blam.ThirdGen.Resources
{
    public class ThirdGenResourceMetaLoader : IResourceMetaLoader
    {
        private BuildInformation _buildInfo;
        private FileSegmentGroup _metaArea;

        public ThirdGenResourceMetaLoader(BuildInformation buildInfo, FileSegmentGroup metaArea)
        {
            _buildInfo = buildInfo;
            _metaArea = metaArea;
        }

        public bool SupportsRenderModels
        {
            get { return _buildInfo.HasLayout("render model"); }
        }

        public IRenderModel LoadRenderModelMeta(ITag modeTag, IReader reader)
        {
            if (modeTag.MetaLocation == null || modeTag.Class == null || modeTag.Class.Magic != ModeMagic)
                throw new ArgumentException("modeTag does not point to metadata for a renderable model");

            if (!SupportsRenderModels)
                throw new NotSupportedException("Render model metadata loading is not supported for the cache file's engine.");

            reader.SeekTo(modeTag.MetaLocation.AsOffset());
            var layout = _buildInfo.GetLayout("render model");
            var values = StructureReader.ReadStructure(reader, layout);
            return new ThirdGenRenderModel(values, reader, _metaArea, _buildInfo);
        }

        private static int ModeMagic = CharConstant.FromString("mode");
    }
}
