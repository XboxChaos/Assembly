using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.ThirdGen.Structures
{
    public class ThirdGenVersionInfo
    {
        public ThirdGenVersionInfo(IReader reader)
        {
            reader.SeekTo(0x4);
            Version = reader.ReadInt32();

            reader.SeekTo(0x11C);
            BuildString = reader.ReadAscii();
        }

        public int Version { get; private set; }
        public string BuildString { get; private set; }
    }
}
