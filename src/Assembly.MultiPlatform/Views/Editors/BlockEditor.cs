using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;

namespace Assembly.MultiPlatform.Views.Editors
{
	/// <summary>
	///     Editor for <see cref="Assembly.MultiPlatform.Services.MetaFieldKind.TagBlock" /> - shared by
	///     a classic tag block and a Campaign Evolved block/array/struct container field (see
	///     <see cref="Assembly.MultiPlatform.ViewModels.TagDocumentViewModel.GetFifthGenContainerDef" />,
	///     which reuses the same <see cref="Assembly.MultiPlatform.Services.MetaFieldKind.TagBlock" />
	///     kind for its own containers). Element navigation itself
	///     (<see cref="Assembly.MultiPlatform.ViewModels.TagDocumentViewModel.SetElementIndex" />) is
	///     the same call either way; only the address/size line differs, since a fifth-generation
	///     tag has no cache-relative file offset to show (see
	///     <see cref="Assembly.MultiPlatform.ViewModels.MetaRowViewModel.ElementsBaseFileOffset" />).
	/// </summary>
	public sealed class BlockEditor : FieldEditorBase
	{
		private TextBlock _summary = null!;
		private TextBlock _address = null!;
		private TextBox _indexBox = null!;
		private Button _first = null!, _prev = null!, _next = null!, _last = null!;
		private ToggleButton _expand = null!;

		protected override void OnBind()
		{
			var row = Context.Row;

			_summary = new TextBlock { Classes = { "mono" }, FontSize = 12 };

			var nav = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
			_first = new Button { Content = "|<" };
			_prev = new Button { Content = "< Prev" };
			_indexBox = new TextBox { Width = 60, HorizontalContentAlignment = HorizontalAlignment.Center };
			_next = new Button { Content = "Next >" };
			_last = new Button { Content = ">|" };

			_first.Click += (_, _) => Jump(0);
			_prev.Click += (_, _) => Jump(row.ElementIndex - 1);
			_next.Click += (_, _) => Jump(row.ElementIndex + 1);
			_last.Click += (_, _) => Jump(row.ElementCount - 1);
			_indexBox.KeyDown += (_, e) =>
			{
				if (e.Key != Key.Enter) return;
				if (int.TryParse(_indexBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
					Jump(i - 1); // shown 1-based, stored 0-based
				e.Handled = true;
			};
			_indexBox.LostFocus += (_, _) => RefreshDisplay(); // discard an unsubmitted jump-to-index edit

			nav.Children.Add(_first);
			nav.Children.Add(_prev);
			nav.Children.Add(_indexBox);
			nav.Children.Add(_next);
			nav.Children.Add(_last);

			_address = EditorVisuals.Hint("");

			_expand = new ToggleButton();
			_expand.Click += (_, _) => Context.Doc.ToggleExpand(row);

			Children.Add(_summary);
			Children.Add(nav);
			Children.Add(_address);
			Children.Add(_expand);
			Children.Add(EditorVisuals.Hint(Context.Doc.IsFifthGen
				? "This tag is self-describing: its block/array/struct elements were already parsed from the payload's own 'bdat' chunk when the tag was opened, not resolved through a cache pointer."
				: "Element navigation reads the block's live count/pointer from the cache and resolves the pointer through the cache's own meta-area converter - not a canned list."));

			// Only a genuine "block" field is a resizable collection - a fixed-size "array" has no
			// add/remove concept at all (FifthGenArrayValue.Elements has no such API), and a
			// "struct" container always has exactly one, inlined instance. See
			// TagDocumentViewModel.GetFifthGenContainerDef for where this shape label comes from.
			if (Context.Doc.IsFifthGen && row.Def.KindLabelOverride == "block")
			{
				Children.Add(EditorVisuals.Hint(
					"Adding or removing elements is not wired up here yet. Blamite's FifthGenTagBlock.AddElement/" +
					"InsertElement/RemoveElementAt exist and FifthGenTagStruct.CreateDefault can build a blank " +
					"element, but nothing in this build has exercised that path end to end, so it is left off " +
					"rather than offered untested."));
			}

			RefreshDisplay();
		}

		private void Jump(int index) => Context.Doc.SetElementIndex(Context.Row, index);

		protected override void OnRowChanged(string? propertyName) => RefreshDisplay();

		private void RefreshDisplay()
		{
			var row = Context.Row;
			_summary.Text = row.ElementSummary;
			_indexBox.Text = row.ElementCount == 0 ? "0" : (row.ElementIndex + 1).ToString(CultureInfo.InvariantCulture);

			bool hasElements = row.ElementCount > 0;
			_first.IsEnabled = hasElements && row.ElementIndex > 0;
			_prev.IsEnabled = hasElements && row.ElementIndex > 0;
			_next.IsEnabled = hasElements && row.ElementIndex < row.ElementCount - 1;
			_last.IsEnabled = hasElements && row.ElementIndex < row.ElementCount - 1;
			_indexBox.IsEnabled = hasElements;

			if (!hasElements)
			{
				_address.Text = "no elements";
			}
			else if (row.ElementsBaseFileOffset < 0)
			{
				_address.Text = Context.Doc.IsFifthGen
					? $"element size: 0x{row.Def.EntrySize:X} bytes  |  address: n/a (Campaign Evolved tags have no cache-relative address space)"
					: $"element size: 0x{row.Def.EntrySize:X} bytes  |  address: unresolved (pointer did not convert through the cache's meta area)";
			}
			else
			{
				long addr = row.ElementsBaseFileOffset + (long)row.ElementIndex * row.Def.EntrySize;
				_address.Text = $"element size: 0x{row.Def.EntrySize:X} bytes  |  address: 0x{addr:X8}";
			}

			_expand.Content = row.IsExpanded ? "Collapse in table" : "Expand in table";
			_expand.IsChecked = row.IsExpanded;
		}

		public override string? GetClipboardValue() => Context.Row.ElementSummary;
	}
}
