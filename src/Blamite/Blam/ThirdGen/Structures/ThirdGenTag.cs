/* Copyright 2012 Aaron Dierking, TJ Tunnell, Jordan Mueller, Alex Reed
 * 
 * This file is part of Blamite.
 * 
 * Blamite is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Blamite is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Blamite.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using Blamite.Serialization;
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
			IList<ITagClass> classList, IPointerExpander expander)
		{
			Load(values, index, metaArea, classList, expander);
		}

		public DatumIndex Index { get; private set; }
		public ITagClass Class { get; set; }
		public SegmentPointer MetaLocation { get; set; }

		public StructureValueCollection Serialize(IList<ITagClass> classList, IPointerExpander expander)
		{
			var result = new StructureValueCollection();

			uint cont = 0;
			if (MetaLocation != null)
				cont = expander.Contract(MetaLocation.AsPointer());

			result.SetInteger("memory address", cont);
			result.SetInteger("class index", (Class != null) ? (uint) classList.IndexOf(Class) : 0xFFFFFFFF);
			result.SetInteger("datum index salt", Index.Salt);
			return result;
		}

		private void Load(StructureValueCollection values, ushort index, FileSegmentGroup metaArea, IList<ITagClass> classList, IPointerExpander expander)
		{
			uint address = (uint)values.GetInteger("memory address");
			if (address != 0 && address != 0xFFFFFFFF)
			{
				long expanded = expander.Expand(address);

				MetaLocation = SegmentPointer.FromPointer(expanded, metaArea);
			}

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