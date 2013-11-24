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

using Blamite.IO;

namespace Blamite.Blam
{
	/// <summary>
	///     A value type representing a datum index.
	/// </summary>
	public struct DatumIndex
	{
		/// <summary>
		///     A null (invalid) datum index.
		/// </summary>
		public static readonly DatumIndex Null = new DatumIndex(0xFFFFFFFF);

		/// <summary>
		///     The index part of the datum index.
		/// </summary>
		public readonly ushort Index;

		/// <summary>
		///     The salt part of the datum index.
		/// </summary>
		public readonly ushort Salt;

		public DatumIndex(uint value)
		{
			Salt = (ushort) ((value >> 16) & 0xFFFF);
			Index = (ushort) (value & 0xFFFF);
		}

		public DatumIndex(ushort salt, ushort index)
		{
			Salt = salt;
			Index = index;
		}

		/// <summary>
		///     The datum index as a 32-bit unsigned value.
		/// </summary>
		public uint Value
		{
			get { return (uint) ((Salt << 16) | Index); }
		}

		/// <summary>
		///     Whether or not this datum index points to valid data.
		/// </summary>
		public bool IsValid
		{
			get { return (Salt != 0xFFFF || Index != 0xFFFF); }
		}

		/// <summary>
		///     Reads a DatumIndex from a stream and returns it.
		/// </summary>
		/// <param name="reader">The IReader to read from.</param>
		/// <returns>The DatumIndex that was read.</returns>
		public static DatumIndex ReadFrom(IReader reader)
		{
			return new DatumIndex(reader.ReadUInt32());
		}

		/// <summary>
		///     Writes the DatumIndex out to an IWriter.
		/// </summary>
		/// <param name="writer">The IWriter to write to.</param>
		public void WriteTo(IWriter writer)
		{
			writer.WriteUInt32(Value);
		}

		public override bool Equals(object obj)
		{
			return (obj is DatumIndex) && (this == (DatumIndex) obj);
		}

		public override int GetHashCode()
		{
			return (int) Value;
		}

		public static bool operator ==(DatumIndex x, DatumIndex y)
		{
			return (x.Salt == y.Salt && x.Index == y.Index);
		}

		public static bool operator !=(DatumIndex x, DatumIndex y)
		{
			return !(x == y);
		}

		public override string ToString()
		{
			return "0x" + Value.ToString("X8");
		}
	}
}