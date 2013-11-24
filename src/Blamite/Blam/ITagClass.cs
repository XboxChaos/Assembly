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

namespace Blamite.Blam
{
	/// <summary>
	///     Information about a single tag class in a cache file.
	/// </summary>
	public interface ITagClass
	{
		/// <summary>
		///     The class's magic as a character string constant.
		/// </summary>
		int Magic { get; set; }

		/// <summary>
		///     The parent class's magic, or -1 if none.
		/// </summary>
		int ParentMagic { get; set; }

		/// <summary>
		///     The magic of the parent class's parent, or -1 if none.
		/// </summary>
		int GrandparentMagic { get; set; }

		/// <summary>
		///     The stringID describing the class's purpose, if available.
		/// </summary>
		StringID Description { get; set; }
	}
}