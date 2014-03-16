using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Plugins.Fields
{
	/// <summary>
	/// A block of data in a tag.
	/// </summary>
	public class RawField : ValuePluginField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RawField" /> class.
		/// </summary>
		/// <param name="size">Size of the data block in bytes.</param>
		/// <param name="displayName">A display-friendly name for the field.</param>
		/// <param name="offset">The offset of the field from the start of its block.</param>
		/// <param name="id">The field's unique ID string.</param>
		/// <param name="alwaysVisible">If set to <c>true</c>, the field should always be shown in an editor.</param>
		/// <param name="sourceFile">The name of the file that the field originated from, or <c>null</c> for none.</param>
		/// <param name="sourceLine">The zero-based line number that the field originated from, or a negative value for none.</param>
		public RawField(uint size, string displayName, uint offset, string id, bool alwaysVisible, string sourceFile, int sourceLine)
			: base(displayName, offset, id, alwaysVisible, sourceFile, sourceLine)
		{
			Size = size;
		}

		/// <summary>
		/// Gets the size of the data block in bytes.
		/// </summary>
		public uint Size { get; private set; }

		public override void Accept(IPluginFieldVisitor visitor)
		{
			throw new NotImplementedException();
		}
	}
}
