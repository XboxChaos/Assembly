using Assembly.MultiPlatform.Services;
using Avalonia.Controls;
using Avalonia.Input;

namespace Assembly.MultiPlatform.Views.Editors
{
	/// <summary>
	///     Editor for <see cref="MetaFieldKind.FifthGenStringId" />: free text, unlike its classic
	///     <see cref="StringIdEditor" /> counterpart. A classic stringID is an index into a
	///     cache-wide table, so only a string already in that table can be picked; a Campaign
	///     Evolved stringID carries its own literal text in a section of its own
	///     (<c>FifthGenStringIDValue</c>'s remarks), so retyping it needs no table lookup and no
	///     allocator - the section is simply re-encoded to whatever length the new text needs.
	/// </summary>
	public sealed class FifthGenStringIdEditor : FieldEditorBase
	{
		private TextBox _box = null!;
		private TextBlock _idLabel = null!;
		private FieldEditState? _before;

		protected override void OnBind()
		{
			var edit = Context.Row.Current!;

			_box = new TextBox { Text = edit.Text ?? "", Width = 260 };
			_idLabel = EditorVisuals.Hint("");

			_box.GotFocus += (_, _) => _before = Context.Snapshot();
			_box.LostFocus += (_, _) => FinishGesture();
			_box.KeyDown += (_, e) =>
			{
				if (e.Key == Key.Enter) { FinishGesture(); e.Handled = true; }
				else if (e.Key == Key.Escape) { CancelGesture(); e.Handled = true; }
			};
			_box.TextChanged += (_, _) => ApplyLiveText();

			Children.Add(EditorVisuals.LabeledRow("Text", _box));
			Children.Add(_idLabel);
			Children.Add(EditorVisuals.Hint(
				"This is a Campaign Evolved stringID: its literal text lives in a section of its own, so it can be " +
				"freely retyped rather than picked from an existing table like a classic stringID. The field's " +
				"inline id is left exactly as parsed - nothing in this codebase has derived what hash, if any, the " +
				"runtime expects it to agree with the text on for this generation."));

			RefreshIdLabel();
		}

		protected override void OnRowChanged(string? propertyName)
		{
			if (_box.IsFocused) return;
			_box.Text = Context.Row.Current!.Text ?? "";
			RefreshIdLabel();
		}

		private void RefreshIdLabel() => _idLabel.Text = $"Inline id (unchanged by editing the text): {Context.Row.DisplayValue}";

		private void ApplyLiveText()
		{
			var text = _box.Text ?? "";
			var edit = Context.Row.Current!;
			if (edit.Text == text) return;
			edit.Text = text;
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
		}

		private void FinishGesture()
		{
			Context.Commit(_before);
			_before = null;
		}

		private void CancelGesture()
		{
			Context.Cancel(_before);
			_before = null;
			_box.Text = Context.Row.Current!.Text ?? "";
		}

		public override string? GetClipboardValue() => Context.Row.Current?.Text;

		public override bool TryPasteValue(string text)
		{
			var before = Context.Snapshot();
			var edit = Context.Row.Current!;
			edit.Text = text;
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
			Context.Commit(before);
			_box.Text = text;
			return true;
		}
	}
}
