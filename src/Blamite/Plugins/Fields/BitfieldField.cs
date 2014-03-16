using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Blamite.Plugins.Fields
{
	public enum BitfieldFieldType
	{
		Bitfield8,
		Bitfield16,
		Bitfield32
	}

	public class BitfieldFieldBit
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BitfieldFieldBit"/> class.
		/// </summary>
		/// <param name="name">The name of the bit.</param>
		/// <param name="position">The position of the bit. 0 = LSB.</param>
		public BitfieldFieldBit(string name, int position)
		{
			Name = name;
			Position = position;
		}

		/// <summary>
		/// Gets the name of the bit.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the position of the bit. 0 = LSB.
		/// </summary>
		public int Position { get; private set; }
	}

	public class BitfieldField : ValuePluginField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BitfieldField" /> class.
		/// </summary>
		/// <param name="type">The field's type.</param>
		/// <param name="bits">The bits in the bitfield.</param>
		/// <param name="displayName">A display-friendly name for the field.</param>
		/// <param name="offset">The offset of the field from the start of its block.</param>
		/// <param name="id">The field's unique ID string.</param>
		/// <param name="alwaysVisible">If set to <c>true</c>, the field should always be shown in an editor.</param>
		/// <param name="sourceFile">The name of the file that the field originated from, or <c>null</c> for none.</param>
		/// <param name="sourceLine">The zero-based line number that the field originated from, or a negative value for none.</param>
		public BitfieldField(BitfieldFieldType type, IEnumerable<BitfieldFieldBit> bits, string displayName, uint offset, string id, bool alwaysVisible, string sourceFile, int sourceLine)
			: base(displayName, offset, id, alwaysVisible, sourceFile, sourceLine)
		{
			Type = type;
			Bits = new ReadOnlyCollection<BitfieldFieldBit>(bits.ToList());
		}

		/// <summary>
		/// Gets the field's type.
		/// </summary>
		public BitfieldFieldType Type { get; private set; }

		/// <summary>
		/// Gets the bits in the bitfield.
		/// </summary>
		public ReadOnlyCollection<BitfieldFieldBit> Bits { get; private set; }

		public override void Accept(IPluginFieldVisitor visitor)
		{
			throw new NotImplementedException();
		}
	}
}
