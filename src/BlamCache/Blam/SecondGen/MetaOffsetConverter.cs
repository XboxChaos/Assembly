using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.SecondGen.Structures;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.SecondGen
{
    public class MetaOffsetConverter : IPointerConverter
    {
        private FileSegment _metaSegment;
        private uint _mask;

        public MetaOffsetConverter(FileSegment metaSegment, uint mask)
        {
            _metaSegment = metaSegment;
            _mask = mask;
        }

        public int PointerToOffset(uint pointer)
        {
            return (int)(pointer - _mask + _metaSegment.Offset);
        }

        public uint OffsetToPointer(int offset)
        {
            return (uint)(offset - _metaSegment.Offset + _mask);
        }
    }
}
