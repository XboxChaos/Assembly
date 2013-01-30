/* Copyright 2012 Aaron Dierking, TJ Tunnell, Jordan Mueller, Alex Reed
 * 
 * This file is part of ExtryzeDLL.
 * 
 * Extryze is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Extryze is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ExtryzeDLL.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.Blam.Util;

namespace ExtryzeDLL.Blam.ThirdGen
{
    /// <summary>
    /// Provides methods for converting between memory addresses stored in cache files and file offsets.
    /// </summary>
    public class MetaAddressConverter : PointerConverter
    {
        private ThirdGenHeader _header;

        /// <summary>
        /// Constructs a new AddressConverter.
        /// </summary>
        public MetaAddressConverter(ThirdGenHeader header)
        {
            _header = header;
        }

        public uint AddressMask
        {
            get { return _header.VirtualBaseAddress - _header.MetaOffset; }
        }

        public override uint PointerToOffset(uint pointer)
        {
            return pointer - AddressMask;
        }

        public override uint PointerToAddress(uint pointer)
        {
            return pointer;
        }

        public override uint OffsetToPointer(uint offset)
        {
            return offset + AddressMask;
        }

        public override uint AddressToPointer(uint address)
        {
            return address;
        }
    }
}
