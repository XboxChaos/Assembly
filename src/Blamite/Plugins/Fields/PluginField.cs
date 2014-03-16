using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Plugins.Fields
{
	/// <summary>
	/// Base class for fields in a plugin.
	/// </summary>
	public abstract class PluginField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PluginField"/> class.
		/// </summary>
		/// <param name="id">The field's unique ID string, or <c>null</c> for none.</param>
		/// <param name="alwaysVisible">If set to <c>true</c>, the field should always be shown in an editor.</param>
		/// <param name="sourceFile">The name of the file that the field originated from, or <c>null</c> for none.</param>
		/// <param name="sourceLine">The zero-based line number that the field originated from, or a negative value for none.</param>
		public PluginField(string id, bool alwaysVisible, string sourceFile, int sourceLine)
		{
			ID = id;
			AlwaysVisible = alwaysVisible;
			SourceFile = sourceFile;
			SourceLine = sourceLine;
		}

		/// <summary>
		/// Gets the field's unique ID string.
		/// Can be <c>null</c> if the field does not have a unique ID associated with it.
		/// </summary>
		public string ID { get; private set; }

		/// <summary>
		/// Gets whether or not the field should always be visible in an editor.
		/// </summary>
		public bool AlwaysVisible { get; private set; }

		/// <summary>
		/// Gets the name of the file that the field originated from.
		/// Can be <c>null</c> if the field did not come from a file.
		/// </summary>
		public string SourceFile { get; private set; }

		/// <summary>
		/// Gets the zero-based number of the line in the text file that the field originated from.
		/// Can be negative if the field did not originate from a line in a text file.
		/// </summary>
		public int SourceLine { get; private set; }

		/// <summary>
		/// Passes the field to a visitor object, calling a method on the visitor that corresponds to the field's type.
		/// </summary>
		/// <param name="visitor">The visitor object to pass the field to.</param>
		public abstract void Accept(IPluginFieldVisitor visitor);
	}
}
