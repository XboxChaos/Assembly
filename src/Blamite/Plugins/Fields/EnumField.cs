using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Blamite.Plugins.Fields
{
	public enum EnumFieldType
	{
		Enum8,
		Enum16,
		Enum32
	}

	public class EnumFieldOption
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EnumFieldOption"/> class.
		/// </summary>
		/// <param name="name">The name of the option.</param>
		/// <param name="val">The value of the option.</param>
		public EnumFieldOption(string name, int val)
		{
			Name = name;
			Value = val;
		}

		/// <summary>
		/// Gets the name of the option.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the value of the option.
		/// </summary>
		public int Value { get; private set; }
	}

	public class EnumField : ValuePluginField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EnumField" /> class.
		/// </summary>
		/// <param name="type">The field's type.</param>
		/// <param name="options">The options in the enum.</param>
		/// <param name="displayName">A display-friendly name for the field.</param>
		/// <param name="offset">The offset of the field from the start of its block.</param>
		/// <param name="id">The field's unique ID string.</param>
		/// <param name="alwaysVisible">If set to <c>true</c>, the field should always be shown in an editor.</param>
		/// <param name="sourceFile">The name of the file that the field originated from, or <c>null</c> for none.</param>
		/// <param name="sourceLine">The zero-based line number that the field originated from, or a negative value for none.</param>
		public EnumField(EnumFieldType type, IEnumerable<EnumFieldOption> options, string displayName, uint offset, string id, bool alwaysVisible, string sourceFile, int sourceLine)
			: base(displayName, offset, id, alwaysVisible, sourceFile, sourceLine)
		{
			Type = type;
			Bits = new ReadOnlyCollection<EnumFieldOption>(options.ToList());
		}

		/// <summary>
		/// Gets the field's type.
		/// </summary>
		public EnumFieldType Type { get; private set; }

		/// <summary>
		/// Gets the bits in the bitfield.
		/// </summary>
		public ReadOnlyCollection<EnumFieldOption> Bits { get; private set; }

		public override void Accept(IPluginFieldVisitor visitor)
		{
			throw new NotImplementedException();
		}
	}
}
