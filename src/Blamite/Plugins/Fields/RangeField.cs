using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Plugins.Fields
{
	public enum RangeFieldType
	{
		UInt8,
		Int8,
		UInt16,
		Int16,
		UInt32,
		Int32,
		Float32
	}

	public class RangeField : ValuePluginField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RangeField" /> class.
		/// </summary>
		/// <param name="type">The type of the field.</param>
		/// <param name="min">The minimum value that the field can hold.</param>
		/// <param name="max">The maximum value that the field can hold.</param>
		/// <param name="displayName">A display-friendly name for the field.</param>
		/// <param name="offset">The offset of the field from the start of its block.</param>
		/// <param name="id">The field's unique ID string.</param>
		/// <param name="alwaysVisible">If set to <c>true</c>, the field should always be shown in an editor.</param>
		/// <param name="sourceFile">The name of the file that the field originated from, or <c>null</c> for none.</param>
		/// <param name="sourceLine">The zero-based line number that the field originated from, or a negative value for none.</param>
		public RangeField(RangeFieldType type, double min, double max, string displayName, uint offset, string id, bool alwaysVisible, string sourceFile, int sourceLine)
			: base(displayName, offset, id, alwaysVisible, sourceFile, sourceLine)
		{
			Type = type;
			MinValue = min;
			MaxValue = max;
		}

		/// <summary>
		/// Gets the type of the field.
		/// </summary>
		public RangeFieldType Type { get; private set; }

		/// <summary>
		/// Gets the minimum value that the field can hold.
		/// </summary>
		public double MinValue { get; private set; }

		/// <summary>
		/// Gets the maximum value that the field can hold.
		/// </summary>
		public double MaxValue { get; private set; }

		public override void Accept(IPluginFieldVisitor visitor)
		{
			throw new NotImplementedException();
		}
	}
}
