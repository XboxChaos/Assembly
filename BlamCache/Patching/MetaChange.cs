using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Patching
{
    /// <summary>
    /// Represents a change that should be made to a cache file's tag metadata.
    /// </summary>
    public class MetaChange
    {
        public MetaChange(uint address, byte[] data)
        {
            Address = address;
            Data = data;
        }

        /// <summary>
        /// The memory address that the change is at.
        /// </summary>
        public uint Address { get; private set; }

        /// <summary>
        /// The data that should be written to the address.
        /// </summary>
        public byte[] Data { get; private set; }
    }
}
