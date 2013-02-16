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
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.ThirdGen
{
    /// <summary>
    /// Provides methods for converting between memory addresses stored in cache files and file offsets.
    /// </summary>
    public class MetaAddressConverter : IPointerConverter
    {
        private FileSegment _metaSegment;
        private uint _virtualBase;

        /// <summary>
        /// Constructs a new MetaAddressConverter.
        /// </summary>
        /// <param name="metaSegment">The FileSegment where meta is stored.</param>
        /// <param name="virtualBase">The virtual base address of the meta.</param>
        public MetaAddressConverter(FileSegment metaSegment, uint virtualBase)
        {
            _metaSegment = metaSegment;
            _virtualBase = virtualBase;
            metaSegment.Resized += MetaResized;
        }

        void MetaResized(object sender, SegmentResizedEventArgs e)
        {
            // The meta segment grows downward in memory,
            // so change the virtual base inversely
            _virtualBase -= (uint)(e.NewSize - e.OldSize);
        }

        public int PointerToOffset(uint pointer)
        {
            return (int)(pointer - _virtualBase + _metaSegment.Offset);
        }

        public uint OffsetToPointer(int offset)
        {
            return (uint)(offset - _metaSegment.Offset + _virtualBase);
        }
    }
}
