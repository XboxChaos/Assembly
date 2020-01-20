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

using Blamite.IO;

namespace Blamite.Blam.ThirdGen
{
	/// <summary>
	///     Provides methods for converting between memory addresses stored in cache files and file offsets.
	/// </summary>
	public class MetaAddressConverter : IPointerConverter
	{
		private readonly FileSegment _metaSegment;
		private long _virtualBase;

		/// <summary>
		///     Constructs a new MetaAddressConverter.
		/// </summary>
		/// <param name="metaSegment">The FileSegment where meta is stored.</param>
		/// <param name="virtualBase">The virtual base address of the meta.</param>
		public MetaAddressConverter(FileSegment metaSegment, long virtualBase)
		{
			_metaSegment = metaSegment;
			_virtualBase = virtualBase;
			metaSegment.Resized += MetaResized;
		}

		public int PointerToOffset(long pointer)
		{
			return PointerToOffset(pointer, _metaSegment.Offset);
		}

		public int PointerToOffset(long pointer, int areaStartOffset)
		{
			return (int) (pointer - _virtualBase + areaStartOffset);
		}

		public long OffsetToPointer(int offset)
		{
			return OffsetToPointer(offset, _metaSegment.Offset);
		}

		public long OffsetToPointer(int offset, int areaStartOffset)
		{
			return (offset - areaStartOffset + _virtualBase);
		}

		private void MetaResized(object sender, SegmentResizedEventArgs e)
		{
			// The meta segment grows downward in memory,
			// so change the virtual base inversely
			_virtualBase -= (uint) (e.NewSize - e.OldSize);
		}
	}
}