using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.ThirdGen
{
    public class BasedPointerConverter : IPointerConverter
    {
        private uint _basePointer;
        private int _defaultStartOffset;

        public BasedPointerConverter(uint basePointer, int defaultStartOffset)
        {
            _basePointer = basePointer;
            _defaultStartOffset = defaultStartOffset;
        }

        public int PointerToOffset(uint pointer)
        {
            return (int)(pointer - _basePointer + _defaultStartOffset);
        }

        public int PointerToOffset(uint pointer, int areaStartOffset)
        {
            return (int)(pointer - _basePointer + areaStartOffset);
        }

        public uint OffsetToPointer(int offset)
        {
            return (uint)(offset - _defaultStartOffset + _basePointer);
        }

        public uint OffsetToPointer(int offset, int areaStartOffset)
        {
            return (uint)(offset - areaStartOffset + _basePointer);
        }
    }
}
