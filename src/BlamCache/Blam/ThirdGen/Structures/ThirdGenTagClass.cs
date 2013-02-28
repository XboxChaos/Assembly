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
using ExtryzeDLL.IO;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Blam.ThirdGen.Structures
{
    public class ThirdGenTagClass : ITagClass
    {
        public ThirdGenTagClass(StructureValueCollection values)
        {
            Load(values);
        }

        private void Load(StructureValueCollection values)
        {
            Magic = (int)values.GetInteger("magic");
            ParentMagic = (int)values.GetInteger("parent magic");
            GrandparentMagic = (int)values.GetInteger("grandparent magic");
            Description = new StringID((int)values.GetIntegerOrDefault("stringid", 0));
        }

        public int Magic { get; set; }
        public int ParentMagic { get; set; }
        public int GrandparentMagic { get; set; }
        public StringID Description { get; set; }
    }
}
