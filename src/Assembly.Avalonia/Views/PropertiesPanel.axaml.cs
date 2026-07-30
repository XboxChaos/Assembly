using System.ComponentModel;
using Assembly.Avalonia.Services;
using Assembly.Avalonia.ViewModels;
using Assembly.Avalonia.Views.Editors;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Assembly.Avalonia.Views
{
	/// <summary>
	///     The properties (value) sidebar: renders whichever typed editor
	///     <see cref="MetaRowViewModel.Editor" /> calls for the currently selected row of
	///     <see cref="Document" />'s active tab, plus the chrome every field shares - a header
	///     (name/kind/offset/tooltip), an original-vs-current/Revert footer, Copy/Paste, and a
	///     document-scoped undo/redo stack. Replaces the old MainWindow.BuildEditor, which stuffed
	///     all of this straight into MainWindow's code-behind; the per-kind logic itself now lives
	///     under Views/Editors/ instead of one large switch.
	/// </summary>
	public partial class PropertiesPanel : UserControl
	{
		public static readonly StyledProperty<TagDocumentViewModel?> DocumentProperty =
			AvaloniaProperty.Register<PropertiesPanel, TagDocumentViewModel?>(nameof(Document));

		public TagDocumentViewModel? Document
		{
			get => GetValue(DocumentProperty);
			set => SetValue(DocumentProperty, value);
		}

		static PropertiesPanel()
		{
			DocumentProperty.Changed.AddClassHandler<PropertiesPanel>((panel, _) => panel.OnDocumentChanged());
		}

		private static readonly KeyGesture UndoGesture = KeyGesture.Parse("Cmd+Z");
		private static readonly KeyGesture RedoGesture = KeyGesture.Parse("Cmd+Shift+Z");

		private readonly StackPanel _historyBar;
		private readonly Button _undoButton;
		private readonly Button _redoButton;
		private readonly TextBlock _historyLabel;
		private readonly StackPanel _fieldHost;

		private TagDocumentViewModel? _subscribedDoc;
		private EditHistory? _subscribedHistory;
		private IFieldEditor? _activeEditor;
		private MetaRowViewModel? _footerRow;
		private PropertyChangedEventHandler? _footerHandler;

		public PropertiesPanel()
		{
			InitializeComponent();

			_historyBar = this.FindControl<StackPanel>("HistoryBar")!;
			_undoButton = this.FindControl<Button>("UndoButton")!;
			_redoButton = this.FindControl<Button>("RedoButton")!;
			_historyLabel = this.FindControl<TextBlock>("HistoryLabel")!;
			_fieldHost = this.FindControl<StackPanel>("FieldHost")!;

			_undoButton.Click += (_, _) => { if (Document != null) EditHistory.For(Document).Undo(Document); };
			_redoButton.Click += (_, _) => { if (Document != null) EditHistory.For(Document).Redo(Document); };

			Refresh();
		}

		private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (Document == null || e.Handled) return;

			if (RedoGesture.Matches(e)) { EditHistory.For(Document).Redo(Document); e.Handled = true; }
			else if (UndoGesture.Matches(e)) { EditHistory.For(Document).Undo(Document); e.Handled = true; }
		}

		private void OnDocumentChanged()
		{
			if (_subscribedDoc != null) _subscribedDoc.PropertyChanged -= OnDocPropertyChanged;
			_subscribedDoc = Document;
			if (_subscribedDoc != null) _subscribedDoc.PropertyChanged += OnDocPropertyChanged;

			RebindHistory();
			Refresh();
		}

		private void OnDocPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(TagDocumentViewModel.SelectedRow))
				Refresh();
		}

		private void RebindHistory()
		{
			if (_subscribedHistory != null) _subscribedHistory.Changed -= RefreshHistoryBar;
			_subscribedHistory = Document == null ? null : EditHistory.For(Document);
			if (_subscribedHistory != null) _subscribedHistory.Changed += RefreshHistoryBar;
			RefreshHistoryBar();
		}

		private void RefreshHistoryBar()
		{
			_historyBar.IsVisible = Document != null;
			_undoButton.IsEnabled = _subscribedHistory?.CanUndo == true;
			_redoButton.IsEnabled = _subscribedHistory?.CanRedo == true;
			_historyLabel.Text = _subscribedHistory == null
				? ""
				: $"{_subscribedHistory.UndoCount} to undo, {_subscribedHistory.RedoCount} to redo";
		}

		private void Refresh()
		{
			_activeEditor?.Dispose();
			_activeEditor = null;
			if (_footerRow != null && _footerHandler != null) _footerRow.PropertyChanged -= _footerHandler;
			_footerRow = null;
			_footerHandler = null;

			_fieldHost.Children.Clear();

			var doc = Document;
			var row = doc?.SelectedRow;
			if (doc == null || row == null)
			{
				_fieldHost.Children.Add(EditorVisuals.Hint("Select a field in the table to edit its value here."));
				return;
			}

			_fieldHost.Children.Add(BuildHeader(row));

			var context = new FieldEditorContext(doc, row);
			var editor = CreateEditor(row.Editor);
			editor.Bind(context);
			_activeEditor = editor;
			_fieldHost.Children.Add((Control)editor);

			_fieldHost.Children.Add(BuildFooter(context, editor, row));
		}

		private static IFieldEditor CreateEditor(EditorKind kind) => kind switch
		{
			EditorKind.Integer => new IntegerEditor(),
			EditorKind.Float or EditorKind.Vector2 or EditorKind.Vector3 or EditorKind.Vector4 or EditorKind.RangeFloat => new FloatVectorEditor(),
			EditorKind.RangeInt16 => new ShortRangeEditor(),
			EditorKind.Enum => new EnumEditor(),
			EditorKind.Flags => new FlagsEditor(),
			EditorKind.Color => new ColorEditor(),
			EditorKind.Ascii or EditorKind.Utf16 => new TextFieldEditor(),
			EditorKind.StringId => new StringIdEditor(),
			EditorKind.FifthGenStringId => new FifthGenStringIdEditor(),
			EditorKind.FifthGenTagReference => new FifthGenTagReferenceEditor(),
			EditorKind.Block => new BlockEditor(),
			_ => new ReadOnlyEditor()
		};

		private static Control BuildHeader(MetaRowViewModel row)
		{
			var panel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 0, 0, 4) };

			var nameBlock = new TextBlock { Text = row.Name, FontSize = 14 };
			var boldFont = Res.Font("MetroFontSemiBold");
			if (boldFont != null) nameBlock.FontFamily = boldFont;
			panel.Children.Add(nameBlock);

			panel.Children.Add(new TextBlock { Text = row.KindLabel, Classes = { "label" }, FontSize = 11 });
			panel.Children.Add(new SelectableTextBlock { Text = row.OffsetLabel, Classes = { "mono", "dim" }, FontSize = 10.5 });
			if (!string.IsNullOrWhiteSpace(row.Def.Tooltip))
				panel.Children.Add(new TextBlock { Text = row.Def.Tooltip, Classes = { "label" }, FontSize = 10.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });
			panel.Children.Add(EditorVisuals.Separator());
			return panel;
		}

		private Control BuildFooter(FieldEditorContext context, IFieldEditor editor, MetaRowViewModel row)
		{
			var panel = new StackPanel { Spacing = 6, Margin = new Thickness(0, 4, 0, 0) };
			var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
			buttons.Children.Add(CopyButton(editor));
			buttons.Children.Add(RawCopyButton(context));

			if (row.Def.IsEditable)
			{
				var originalLabel = new TextBlock { Classes = { "label" }, FontSize = 10.5, TextWrapping = TextWrapping.Wrap };
				var currentLabel = new TextBlock { Classes = { "mono", "accent" }, FontSize = 10.5, TextWrapping = TextWrapping.Wrap };
				var dirtyLabel = new TextBlock { Classes = { "label" }, FontSize = 10.5 };
				var revert = new Button { Content = "Revert" };

				void RefreshFooter()
				{
					originalLabel.Text = "Original: " + row.DisplayValue;
					currentLabel.Text = "Current: " + (editor.GetClipboardValue() ?? "(none)");
					dirtyLabel.Text = row.IsDirty ? "* modified (not saved)" : "unchanged";
					revert.IsEnabled = row.IsDirty;
				}
				RefreshFooter();

				_footerHandler = (_, e) =>
				{
					if (e.PropertyName is nameof(MetaRowViewModel.IsDirty) or nameof(MetaRowViewModel.DisplayValue))
						RefreshFooter();
				};
				row.PropertyChanged += _footerHandler;
				_footerRow = row;

				revert.Click += (_, _) => { row.Revert(); context.Doc.RecomputeDirty(); };
				buttons.Children.Add(revert);
				buttons.Children.Add(PasteButton(editor));

				panel.Children.Add(originalLabel);
				panel.Children.Add(currentLabel);
				panel.Children.Add(buttons);
				panel.Children.Add(dirtyLabel);
			}
			else
			{
				panel.Children.Add(buttons);
			}

			return panel;
		}

		private Button CopyButton(IFieldEditor editor)
		{
			var btn = new Button { Content = "Copy Value" };
			btn.Click += async (_, _) =>
			{
				var text = editor.GetClipboardValue();
				if (text == null) return;
				var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
				if (clipboard != null) await clipboard.SetTextAsync(text);
			};
			return btn;
		}

		private Button PasteButton(IFieldEditor editor)
		{
			var btn = new Button { Content = "Paste" };
			btn.Click += async (_, _) =>
			{
				var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
				var text = clipboard == null ? null : await clipboard.TryGetTextAsync();
				if (string.IsNullOrEmpty(text)) return;
				editor.TryPasteValue(text);
			};
			return btn;
		}

		private Button RawCopyButton(FieldEditorContext context)
		{
			bool available = context.TryReadRawBytes(out var bytes) && bytes.Length > 0;
			var btn = new Button { Content = "Copy Raw Bytes (hex)", IsEnabled = available };
			ToolTip.SetTip(btn, available
				? "Copies this field's raw on-disk bytes as space-separated hex."
				: context.RawBytesUnavailableReason);

			btn.Click += async (_, _) =>
			{
				if (!context.TryReadRawBytes(out var fresh) || fresh.Length == 0) return;
				var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
				if (clipboard != null) await clipboard.SetTextAsync(HexView.ToHexLine(fresh));
			};
			return btn;
		}
	}
}
