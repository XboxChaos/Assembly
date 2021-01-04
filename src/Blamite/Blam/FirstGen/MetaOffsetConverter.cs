using Blamite.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.FirstGen
{
    class MetaOffsetConverter : IPointerConverter
    {
        private readonly uint _mask;
        private readonly FileSegment _metaSegment;

        public MetaOffsetConverter(FileSegment metaSegment, uint mask)
        {
            _metaSegment = metaSegment;
            _mask = mask;
        }

        public int PointerToOffset(long pointer)
        {
            return PointerToOffset(pointer, _metaSegment.Offset);
        }

        public int PointerToOffset(long pointer, int areaStartOffset)
        {
            return (int)(pointer - _mask + areaStartOffset);
        }

        public long OffsetToPointer(int offset)
        {
            return OffsetToPointer(offset, _metaSegment.Offset);
        }

        public long OffsetToPointer(int offset, int areaStartOffset)
        {
            return (uint)(offset - areaStartOffset + _mask);
        }
    }
}
