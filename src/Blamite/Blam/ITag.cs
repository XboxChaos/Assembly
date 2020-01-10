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

namespace Blamite.Blam
{
	/// <summary>
	///     Represents a single tag in a cache file.
	/// </summary>
	public interface ITag
	{
		/// <summary>
		///     The tag's group. Can be null.
		/// </summary>
		ITagGroup Group { get; set; }

		/// <summary>
		///     The pointer to the tag's metadata. Can be null.
		/// </summary>
		SegmentPointer MetaLocation { get; set; }

		/// <summary>
		///     The tag's datum index.
		/// </summary>
		DatumIndex Index { get; }
	}
}