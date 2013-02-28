using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Resources;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Blam.ThirdGen.Resources
{
    public class ThirdGenResource : IResource
    {
        private ThirdGenResourceSegment _segment;

        public ThirdGenResource(StructureValueCollection values, ushort index, ThirdGenTagTable tags, ThirdGenResourceLayoutTable layoutInfo)
        {
            Load(values, index, tags, layoutInfo);
        }

        /// <summary>
        /// Gets the datum index of the resource.
        /// </summary>
        public DatumIndex Index { get; private set; }

        /// <summary>
        /// Gets the tag associated with the resource.
        /// </summary>
        public ITag ParentTag { get; private set; }

        /// <summary>
        /// Gets the primary resource page that the resource belongs to. Can be null.
        /// </summary>
        public IResourcePage PrimaryPage
        {
            get { return (_segment != null) ? _segment.PrimaryPage : null; }
        }

        /// <summary>
        /// Gets the offset of the resource data within the primary page. Can be -1.
        /// </summary>
        public int PrimaryOffset
        {
            get { return (_segment != null) ? _segment.PrimaryOffset : -1; }
        }

        /// <summary>
        /// Gets the secondary resource page that the resource belongs to. Can be null.
        /// </summary>
        public IResourcePage SecondaryPage
        {
            get { return (_segment != null) ? _segment.SecondaryPage : null; }
        }

        /// <summary>
        /// Gets the offset of the resource data within the secondary page. Can be -1.
        /// </summary>
        public int SecondaryOffset
        {
            get { return (_segment != null) ? _segment.SecondaryOffset : -1; }
        }

        private void Load(StructureValueCollection values, ushort index, ThirdGenTagTable tags, ThirdGenResourceLayoutTable layoutInfo)
        {
            Index = new DatumIndex((ushort)values.GetInteger("datum index salt"), index);

            DatumIndex tagIndex = new DatumIndex(values.GetInteger("parent tag datum index"));
            if (tagIndex.IsValid)
                ParentTag = tags.Tags[tagIndex.Index];

            int segmentIndex = (int)values.GetInteger("segment index");
            if (segmentIndex >= 0 && segmentIndex < layoutInfo.Segments.Length)
                _segment = layoutInfo.Segments[segmentIndex];
        }
    }
}
