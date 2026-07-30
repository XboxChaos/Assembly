using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Assembly.Avalonia.Views.Editors
{
	/// <summary>
	///     Editor for <see cref="Assembly.Avalonia.Services.MetaFieldKind.StringId" />/
	///     <see cref="Assembly.Avalonia.Services.MetaFieldKind.OldStringId" />. A stringID is an
	///     index into the cache's own string table, not free text: writing an arbitrary new string
	///     would mean growing that table, which needs the allocator this pass does not implement
	///     (see <see cref="Assembly.Avalonia.ViewModels.TagDocumentViewModel.SearchStringIds" />'s
	///     own remarks). This editor keeps that restriction but makes searching the existing table
	///     a proper type-ahead instead of a separate search box plus result list, and says plainly
	///     why free typing will not stick.
	/// </summary>
	public sealed class StringIdEditor : FieldEditorBase
	{
		private AutoCompleteBox _box = null!;
		private TextBlock _current = null!;

		protected override void OnBind()
		{
			_current = new TextBlock { Classes = { "mono", "accent" }, FontSize = 11.5, TextWrapping = TextWrapping.Wrap };

			_box = new AutoCompleteBox
			{
				PlaceholderText = "Search existing string IDs...",
				FilterMode = AutoCompleteFilterMode.None, // the async populator already filters server-side (see below)
				MinimumPrefixLength = 0,
				AsyncPopulator = async (search, token) => await PopulateAsync(search, token),
				HorizontalAlignment = HorizontalAlignment.Stretch
			};
			_box.SelectionChanged += (_, _) =>
			{
				if (_box.SelectedItem is not string s) return;
				var sid = Context.Doc.FindStringId(s);
				if (sid == null) return; // should not happen - only strings the table returned are offered

				var edit = Context.Row.Current!;
				if (edit.Int == sid.Value) return;

				var before = Context.Snapshot();
				edit.Int = sid.Value;
				Context.Row.NotifyEdited();
				Context.Doc.RecomputeDirty();
				Context.Commit(before);
				RefreshCurrent();
			};

			Children.Add(EditorVisuals.LabeledRow("Current", _current));
			Children.Add(EditorVisuals.LabeledRow("Set to an existing string ID", _box));
			Children.Add(EditorVisuals.Hint(
				"Only strings already in this cache's string table can be picked here - adding a brand-new " +
				"string would need the table itself to grow, which this build does not support writing."));

			RefreshCurrent();
		}

		private Task<IEnumerable<object>> PopulateAsync(string? search, CancellationToken token)
		{
			var results = Context.Doc.SearchStringIds(search ?? "", 50).Cast<object>();
			return Task.FromResult(results);
		}

		protected override void OnRowChanged(string? propertyName) => RefreshCurrent();

		private void RefreshCurrent() => _current.Text = Context.Row.DisplayValue;

		public override string? GetClipboardValue() => Context.Row.DisplayValue;

		public override bool TryPasteValue(string text)
		{
			var t = text.Trim().Trim('"');
			var sid = Context.Doc.FindStringId(t);
			if (sid == null) return false;

			var edit = Context.Row.Current!;
			if (edit.Int == sid.Value) return true;

			var before = Context.Snapshot();
			edit.Int = sid.Value;
			Context.Row.NotifyEdited();
			Context.Doc.RecomputeDirty();
			Context.Commit(before);
			RefreshCurrent();
			return true;
		}
	}
}
