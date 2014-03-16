using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Plugins.Fields
{
	/// <summary>
	/// A tag block.
	/// </summary>
	public class TagBlockField : ValuePluginField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TagBlockField"/> class.
		/// </summary>
		/// <param name="entrySize">The size of each entry in the tag block.</param>
		/// <param name="align">A power-of-two to align the field's data to.</param>
		/// <param name="displayName">A display-friendly name for the field.</param>
		/// <param name="offset">The offset of the field from the start of its block.</param>
		/// <param name="id">The field's unique ID string.</param>
		/// <param name="alwaysVisible">If set to <c>true</c>, the field should always be shown in an editor.</param>
		/// <param name="sourceFile">The name of the file that the field originated from, or <c>null</c> for none.</param>
		/// <param name="sourceLine">The zero-based line number that the field originated from, or a negative value for none.</param>
		public TagBlockField(uint entrySize, uint align, string displayName, uint offset, string id, bool alwaysVisible, string sourceFile, int sourceLine)
			: base(displayName, offset, id, alwaysVisible, sourceFile, sourceLine)
		{
			EntrySize = entrySize;
			Alignment = align;
		}

		/// <summary>
		/// Gets the size of each entry in the tag block.
		/// </summary>
		public uint EntrySize { get; private set; }

		/// <summary>
		/// Gets a power-of-two to align the field's data to.
		/// </summary>
		public uint Alignment { get; private set; }

		public override void Accept(IPluginFieldVisitor visitor)
		{
			throw new NotImplementedException();
		}
	}
}
