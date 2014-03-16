using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Plugins.Fields
{
	public enum BasicFieldType
	{
		UInt8,
		Int8,
		UInt16,
		Int16,
		UInt32,
		Int32,
		Float32,
		StringID,
		Vector3,
		Radians,
		Undefined
	}

	public class BasicField : ValuePluginField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BasicField"/> class.
		/// </summary>
		/// <param name="type">The field's type.</param>
		/// <param name="displayName">A display-friendly name for the field.</param>
		/// <param name="offset">The offset of the field from the start of its block.</param>
		/// <param name="id">The field's unique ID string.</param>
		/// <param name="alwaysVisible">If set to <c>true</c>, the field should always be shown in an editor.</param>
		/// <param name="sourceFile">The name of the file that the field originated from, or <c>null</c> for none.</param>
		/// <param name="sourceLine">The zero-based line number that the field originated from, or a negative value for none.</param>
		public BasicField(BasicFieldType type, string displayName, uint offset, string id, bool alwaysVisible, string sourceFile, int sourceLine)
			: base(displayName, offset, id, alwaysVisible, sourceFile, sourceLine)
		{
			Type = type;
		}

		/// <summary>
		/// Gets the field's type.
		/// </summary>
		public BasicFieldType Type { get; private set; }

		public override void Accept(IPluginFieldVisitor visitor)
		{
			throw new NotImplementedException();
		}
	}
}
