using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Plugins.Fields
{
	public enum StringFieldType
	{
		Ascii,
		Utf16
	}

	/// <summary>
	/// String data embedded in a tag.
	/// </summary>
	public class StringField : ValuePluginField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StringField" /> class.
		/// </summary>
		/// <param name="type">The type of the string.</param>
		/// <param name="maxLength">The maximum length that the string can have, in characters.</param>
		/// <param name="displayName">A display-friendly name for the field.</param>
		/// <param name="offset">The offset of the field from the start of its block.</param>
		/// <param name="id">The field's unique ID string.</param>
		/// <param name="alwaysVisible">If set to <c>true</c>, the field should always be shown in an editor.</param>
		/// <param name="sourceFile">The name of the file that the field originated from, or <c>null</c> for none.</param>
		/// <param name="sourceLine">The zero-based line number that the field originated from, or a negative value for none.</param>
		public StringField(StringFieldType type, uint maxLength, string displayName, uint offset, string id, bool alwaysVisible, string sourceFile, int sourceLine)
			: base(displayName, offset, id, alwaysVisible, sourceFile, sourceLine)
		{
			Type = type;
			MaxLength = maxLength;
		}

		/// <summary>
		/// Gets the type of the string.
		/// </summary>
		public StringFieldType Type { get; private set; }

		/// <summary>
		/// Gets the maximum length that the string can have, in characters.
		/// </summary>
		public uint MaxLength { get; private set; }

		public override void Accept(IPluginFieldVisitor visitor)
		{
			throw new NotImplementedException();
		}
	}
}
