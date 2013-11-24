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

namespace Blamite.Flexibility
{
	/// <summary>
	///     The different types of basic values that a structure field can hold.
	/// </summary>
	public enum StructureValueType
	{
		Byte,
		SByte,
		UInt16, // ushort
		Int16, // short
		UInt32, // uint
		Int32, // int
		Asciiz, // Null-terminated ASCII string
		Float32
	}
}