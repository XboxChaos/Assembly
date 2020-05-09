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

		public DatumIndex(ulong value) : this((uint)value) { }

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