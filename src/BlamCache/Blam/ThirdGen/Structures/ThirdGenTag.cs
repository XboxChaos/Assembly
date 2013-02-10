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
        public ThirdGenTag(StructureValueCollection values, ushort index, MetaAddressConverter converter, IList<ITagClass> classList)
        {
            Load(values, index, converter, classList);
        }

        void Load(StructureValueCollection values, ushort index, MetaAddressConverter converter, IList<ITagClass> classList)
        {
            int classIndex = (int)values.GetNumber("class index");
            if (classIndex >= 0)
                Class = classList[classIndex];

            ushort salt = (ushort)values.GetNumber("datum index salt");
            Index = new DatumIndex(salt, index);

            MetaLocation = new Pointer(values.GetNumber("memory address"), converter);
        }

        public DatumIndex Index { get; private set; }
        public ITagClass Class { get; set; }
        public Pointer MetaLocation { get; set; }
    }
    
}
