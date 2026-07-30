using Assembly.MultiPlatform.Services;
using Avalonia.Controls;
using Avalonia.Input;
using Blamite.Util;

namespace Assembly.MultiPlatform.Views.Editors
{
	/// <summary>
	///     Editor for <see cref="MetaFieldKind.FifthGenTagReference" />: a four-CC group box plus a
	///     path box, writing through <c>FifthGenTagReferenceValue.SetReference</c>. Classic
	///     <see cref="Assembly.MultiPlatform.Services.MetaFieldKind.TagReference" /> has no editor of its
	///     own yet (see <see cref="ReadOnlyEditor.ReasonFor" />'s case for it - retargeting a classic
	///     reference means resolving a target back to a datum index, which this pass does not add),
	///     so this is a fresh editor rather than a shape reused from one; the two references also
	///     resolve differently at read time (<see cref="Assembly.MultiPlatform.ViewModels.TagRefTarget" />'s
	///     remarks), for the same underlying reason.
	/// </summary>
	public sealed class FifthGenTagReferenceEditor : FieldEditorBase
	{
		private TextBox _groupBox = null!;
		private TextBox _pathBox = null!;
		private TextBlock _groupErr = null!;
		private FieldEditState? _before;

		protected override void OnBind()
		{
			var edit = Context.Row.Current!;

			_groupBox = new TextBox { Text = GroupText(edit.Int ?? 0), Width = 80, Classes = { "mono" } };
			_groupErr = EditorVisuals.ErrorText();
			_pathBox = new TextBox { Text = edit.Text ?? "", Width = 260 };

			_groupBox.GotFocus += (_, _) => _before ??= Context.Snapshot();
			_pathBox.GotFocus += (_, _) => _before ??= Context.Snapshot();
			_groupBox.LostFocus += (_, _) => FinishGesture();
			_pathBox.LostFocus += (_, _) => FinishGesture();
			foreach (var box in new[] { _groupBox, _pathBox })
			{
				box.KeyDown += (_, e) =>
				{
					if (e.Key == Key.Enter) { FinishGesture(); e.Handled = true; }
					else if (e.Key == Key.Escape) { CancelGesture(); e.Handled = true; }
				};
			}
			_groupBox.TextChanged += (_, _) => ApplyLiveText();
			_pathBox.TextChanged += (_, _) => ApplyLiveText();

			Children.Add(EditorVisuals.LabeledRow("Group (four-CC, blank for a null reference)", _groupBox, _groupErr));
			Children.Add(EditorVisuals.LabeledRow("Path", _pathBox));
			Children.Add(EditorVisuals.Hint(
				"A Campaign Evolved reference resolves by group and path, not by a datum index into this cache - " +
				"retargeting it does not require the target to already be mounted, but a path that names nothing " +
				"mounted will only be honest about that when this tab is reopened and the reference is followed."));
		}

		protected override void OnRowChanged(string? propertyName)
		{
			if (_groupBox.IsFocused || _pathBox.IsFocused) return;
			var edit = Context.Row.Current!;
			_groupBox.Text = GroupText(edit.Int ?? 0);
			_pathBox.Text = edit.Text ?? "";
			EditorVisuals.MarkValid(_groupBox, _groupErr);
		}

		private void ApplyLiveText()
		{
			string groupText = (_groupBox.Text ?? "").Trim();
			if (groupText.Length != 0 && groupText.Length != 4)
			{
				EditorVisuals.MarkInvalid(_groupBox, _groupErr, "a group tag is exactly four characters, or blank for a null reference");
				return;
			}
			EditorVisuals.MarkValid(_groupBox, _groupErr);

			long groupMagic = groupText.Length == 0 ? 0 : CharConstant.FromString(groupText);
			string path = groupMagic == 0 ? "" : (_pathBox.Text ?? "");

			var edit = Context.Row.Current!;
			if (edit.Int == groupMagic && edit.Text == path) return;
			edit.Int = groupMagic;
			edit.Text = path;
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
		}

		private void FinishGesture()
		{
			Context.Commit(_before);
			_before = null;
			var edit = Context.Row.Current!;
			_groupBox.Text = GroupText(edit.Int ?? 0);
			_pathBox.Text = edit.Text ?? "";
		}

		private void CancelGesture()
		{
			Context.Cancel(_before);
			_before = null;
			var edit = Context.Row.Current!;
			_groupBox.Text = GroupText(edit.Int ?? 0);
			_pathBox.Text = edit.Text ?? "";
			EditorVisuals.MarkValid(_groupBox, _groupErr);
		}

		private static string GroupText(long groupMagic) => groupMagic == 0 ? "" : CharConstant.ToString((int)groupMagic);

		public override string? GetClipboardValue()
		{
			var edit = Context.Row.Current;
			if (edit == null) return null;
			return (edit.Int ?? 0) == 0 ? "null" : $"{edit.Text}.{GroupText(edit.Int ?? 0)}";
		}

		public override bool TryPasteValue(string text)
		{
			var t = text.Trim();
			if (t.Equals("null", System.StringComparison.OrdinalIgnoreCase))
			{
				var beforeNull = Context.Snapshot();
				var editNull = Context.Row.Current!;
				editNull.Int = 0;
				editNull.Text = "";
				Context.Row.NotifyEdited();
				Context.Doc.RecomputeDirty();
				Context.Commit(beforeNull);
				_groupBox.Text = "";
				_pathBox.Text = "";
				return true;
			}

			int dot = t.LastIndexOf('.');
			if (dot < 0 || dot != t.Length - 5) return false; // needs a trailing ".xxxx" four-CC

			string path = t[..dot];
			string group = t[(dot + 1)..];

			var before = Context.Snapshot();
			var edit = Context.Row.Current!;
			edit.Int = CharConstant.FromString(group);
			edit.Text = path;
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
			Context.Commit(before);
			_groupBox.Text = group;
			_pathBox.Text = path;
			return true;
		}
	}
}
