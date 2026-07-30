using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Assembly.MultiPlatform.Views.Editors
{
	/// <summary>
	///     Common plumbing for every typed field editor: each one <em>is</em> a
	///     <see cref="StackPanel" /> (added directly into <see cref="PropertiesPanel" />'s content
	///     area, matching the codebase's convention of building the sidebar in code rather than
	///     through DataTemplates - see the old MainWindow.BuildEditor this replaces) that reacts to
	///     its own row changing out from under it for a reason other than typing into itself: an
	///     Undo/Redo, a "Revert" click, "Revert All", or a save reseeding the clean baseline. All
	///     of those go through <see cref="MetaRowViewModel.NotifyEdited" />, so subscribing once
	///     here and dispatching to <see cref="OnRowChanged" /> is enough for every subclass.
	/// </summary>
	public abstract class FieldEditorBase : StackPanel, IFieldEditor
	{
		protected FieldEditorBase()
		{
			Orientation = Orientation.Vertical;
			Spacing = 8;
		}

		protected FieldEditorContext Context { get; private set; } = null!;

		public void Bind(FieldEditorContext context)
		{
			Context = context;
			Context.Row.PropertyChanged += OnRowPropertyChanged;
			OnBind();
		}

		/// <summary>Build this editor's controls against <see cref="Context" />, which is set before this is called.</summary>
		protected abstract void OnBind();

		/// <summary>
		///     Called whenever the bound row raises a property change - refresh whatever this
		///     editor displays from <c>Context.Row.Current</c> here. Editors that only ever change
		///     their own row through themselves (nothing external can touch this kind's state) may
		///     leave this empty; most should not, since Undo/Redo/Revert All can retarget any row.
		/// </summary>
		protected virtual void OnRowChanged(string? propertyName)
		{
		}

		private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e) => OnRowChanged(e.PropertyName);

		public virtual void Dispose()
		{
			if (Context != null)
				Context.Row.PropertyChanged -= OnRowPropertyChanged;
		}

		public virtual string? GetClipboardValue() => null;
		public virtual bool TryPasteValue(string text) => false;
	}
}
