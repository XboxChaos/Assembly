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

using System.Collections.Generic;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Structures
{
	public class ThirdGenTag : ITag
	{
		public ThirdGenTag(DatumIndex index, ITagClass tagClass, SegmentPointer metaLocation)
		{
			Index = index;
			Class = tagClass;
			MetaLocation = metaLocation;
		}

		public ThirdGenTag(StructureValueCollection values, ushort index, FileSegmentGroup metaArea,
			IList<ITagClass> classList)
		{
			Load(values, index, metaArea, classList);
		}

		public DatumIndex Index { get; private set; }
		public ITagClass Class { get; set; }
		public SegmentPointer MetaLocation { get; set; }

		public StructureValueCollection Serialize(IList<ITagClass> classList)
		{
			var result = new StructureValueCollection();
			result.SetInteger("memory address", (MetaLocation != null) ? MetaLocation.AsPointer() : 0);
			result.SetInteger("class index", (Class != null) ? (uint) classList.IndexOf(Class) : 0xFFFFFFFF);
			result.SetInteger("datum index salt", Index.Salt);
			return result;
		}

		private void Load(StructureValueCollection values, ushort index, FileSegmentGroup metaArea, IList<ITagClass> classList)
		{
			uint address = values.GetInteger("memory address");
			if (address != 0 && address != 0xFFFFFFFF)
				MetaLocation = SegmentPointer.FromPointer(address, metaArea);

			var classIndex = (int) values.GetInteger("class index");
			if (classIndex >= 0 && classIndex < classList.Count)
				Class = classList[classIndex];

			var salt = (ushort) values.GetInteger("datum index salt");
			if (salt != 0xFFFF)
				Index = new DatumIndex(salt, index);
			else
				Index = DatumIndex.Null;
		}
	}
}