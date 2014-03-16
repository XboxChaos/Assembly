using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Plugins.Fields
{
	/// <summary>
	/// Base class for plugin fields which represent a value stored in tag data.
	/// </summary>
	public abstract class ValuePluginField : PluginField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ValuePluginField"/> class.
		/// The field will be given an ID, display name, offset, and visibility status.
		/// </summary>
		/// <param name="displayName">A display-friendly name for the field.</param>
		/// <param name="offset">The offset of the field from the start of its block.</param>
		/// <param name="id">The field's unique ID string.</param>
		/// <param name="alwaysVisible">If set to <c>true</c>, the field should always be shown in an editor.</param>
		/// <param name="sourceFile">The name of the file that the field originated from, or <c>null</c> for none.</param>
		/// <param name="sourceLine">The zero-based line number that the field originated from, or a negative value for none.</param>
		public ValuePluginField(string displayName, uint offset, string id, bool alwaysVisible, string sourceFile, int sourceLine)
			: base(id, alwaysVisible, sourceFile, sourceLine)
		{
			DisplayName = displayName;
			Offset = offset;
		}

		/// <summary>
		/// Gets a display-friendly name for the field.
		/// </summary>
		public string DisplayName { get; private set; }

		/// <summary>
		/// Gets the offset of the field from the start of its block.
		/// </summary>
		public uint Offset { get; private set; }
	}
}
