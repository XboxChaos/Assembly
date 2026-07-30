using System;

namespace Assembly.MultiPlatform.Views.Editors
{
	/// <summary>
	///     Implemented by every typed field editor <see cref="PropertiesPanel" /> can host. Each
	///     editor is itself an Avalonia control (see <see cref="FieldEditorBase" />) added directly
	///     into the panel's content area - this interface is the seam <see cref="PropertiesPanel" />
	///     uses to bind it to a row and to drive the shared Copy/Paste toolbar without needing to
	///     know which concrete editor it is looking at.
	/// </summary>
	public interface IFieldEditor : IDisposable
	{
		/// <summary>Wires this editor up to a specific row. Called once, immediately after construction and before the control is shown.</summary>
		void Bind(FieldEditorContext context);

		/// <summary>Canonical text form of the field's current value, for the "Copy Value" action.
		/// Null if this editor kind has nothing sensible to copy.</summary>
		string? GetClipboardValue();

		/// <summary>Attempts to parse <paramref name="text" /> (typically what <see cref="GetClipboardValue" />
		/// itself produced, but anything hand-typed is fair game too) into this field's edit
		/// state and commit it as one undo step. Must leave the row untouched and return false if
		/// the text cannot be understood - never guess or partially apply it.</summary>
		bool TryPasteValue(string text);
	}
}
