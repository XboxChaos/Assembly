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
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.Blam;
using ExtryzeDLL.Blam.Util;

namespace ExtryzeDLL.Blam.ThirdGen.Structures
{
    public class ThirdGenTag : ITag
    {
        public ThirdGenTag(StructureValueCollection values, ushort index, FileSegmentGroup metaArea, IList<ITagClass> classList)
        {
            Load(values, index, metaArea, classList);
        }

        void Load(StructureValueCollection values, ushort index, FileSegmentGroup metaArea, IList<ITagClass> classList)
        {
            uint address = values.GetNumber("memory address");
            if (address != 0 && address != 0xFFFFFFFF)
            {
                int classIndex = (int)values.GetNumber("class index");
                Class = classList[classIndex];

                ushort salt = (ushort)values.GetNumber("datum index salt");
                Index = new DatumIndex(salt, index);

                MetaLocation = SegmentPointer.FromPointer(address, metaArea);
            }
        }

        public DatumIndex Index { get; private set; }
        public ITagClass Class { get; set; }
        public SegmentPointer MetaLocation { get; set; }
    }
    
}
