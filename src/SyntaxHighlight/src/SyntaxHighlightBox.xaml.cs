using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Windows.Input;
using System.Collections.Generic;
using System.Threading;

namespace AurelienRibon.Ui.SyntaxHighlightBox {
	public partial class SyntaxHighlightBox : TextBox {

		// --------------------------------------------------------------------
		// Attributes
		// --------------------------------------------------------------------

		public double LineHeight {
			get { return lineHeight; }
			set {
				if (value != lineHeight) {
					lineHeight = value;
					blockHeight = MaxLineCountInBlock * value;
					TextBlock.SetLineStackingStrategy(this, LineStackingStrategy.BlockLineHeight);
					TextBlock.SetLineHeight(this, lineHeight);
				}
			}
		}

		public int MaxLineCountInBlock {
			get { return maxLineCountInBlock; }
			set {
				maxLineCountInBlock = value > 0 ? value : 0;
				blockHeight = value * LineHeight;
			}
		}

		public IHighlighter CurrentHighlighter { get; set; }

		private DrawingControl renderCanvas;
		private ScrollViewer scrollViewer;
		private double lineHeight;
		private int totalLineCount;
		private List<InnerTextBlock> blocks;
		private double blockHeight;
		private int maxLineCountInBlock;

		// --------------------------------------------------------------------
		// Ctor and event handlers
		// --------------------------------------------------------------------

		public SyntaxHighlightBox() {
			InitializeComponent();

			MaxLineCountInBlock = 100;
			LineHeight = FontSize * 1.3;
			totalLineCount = 1;
			blocks = new List<InnerTextBlock>();

			Loaded += (s, e) => {
				renderCanvas = (DrawingControl)Template.FindName("PART_RenderCanvas", this);
				scrollViewer = (ScrollViewer)Template.FindName("PART_ContentHost", this);
                
				scrollViewer.ScrollChanged += OnScrollChanged;

				InvalidateBlocks(0);
				InvalidateVisual();
			};

			SizeChanged += (s, e) => {
				if (e.HeightChanged == false)
					return;
				UpdateBlocks();
				InvalidateVisual();
			};

			TextChanged += (s, e) => {
				UpdateTotalLineCount();
				InvalidateBlocks(e.Changes.First().Offset);
				InvalidateVisual();
			};
		}

		protected override void OnRender(DrawingContext drawingContext) {
			DrawBlocks();
			base.OnRender(drawingContext);
		}

		private void OnScrollChanged(object sender, ScrollChangedEventArgs e) {
			if (e.VerticalChange != 0)
				UpdateBlocks();
			InvalidateVisual();
		}

		// -----------------------------------------------------------
		// Updating & Block managing
		// -----------------------------------------------------------

		private void UpdateTotalLineCount() {
			totalLineCount = TextUtilities.GetLineCount(Text);
		}

		private void UpdateBlocks() {
			if (blocks.Count == 0)
				return;

			// While something is visible after last block...
			while (!blocks.Last().IsLast && blocks.Last().Position.Y + blockHeight - VerticalOffset < ActualHeight) {
				int firstLineIndex = blocks.Last().LineEndIndex + 1;
				int lastLineIndex = firstLineIndex + maxLineCountInBlock - 1;
				lastLineIndex = lastLineIndex <= totalLineCount - 1 ? lastLineIndex : totalLineCount - 1;

				int fisrCharIndex = blocks.Last().CharEndIndex + 1;
				int lastCharIndex = TextUtilities.GetLastCharIndexFromLineIndex(Text, lastLineIndex); // to be optimized (forward search)

				if (lastCharIndex <= fisrCharIndex) {
					blocks.Last().IsLast = true;
					return;
				}

				InnerTextBlock block = new InnerTextBlock(
					fisrCharIndex,
					lastCharIndex,
					blocks.Last().LineEndIndex + 1,
					lastLineIndex,
					LineHeight);
				block.RawText = block.GetSubString(Text);
				block.LineNumbers = GetFormattedLineNumbers(block.LineStartIndex, block.LineEndIndex);
				blocks.Add(block);
				FormatBlock(block, blocks.Count > 1 ? blocks[blocks.Count - 2] : null);
			}
		}

		private void InvalidateBlocks(int changeOffset) {
			InnerTextBlock blockChanged = null;
			for (int i = 0; i < blocks.Count; i++) {
				if (blocks[i].CharStartIndex <= changeOffset && changeOffset <= blocks[i].CharEndIndex + 1) {
					blockChanged = blocks[i];
					break;
				}
			}

			if (blockChanged == null && changeOffset > 0)
				blockChanged = blocks.Last();

			int fvline = blockChanged != null ? blockChanged.LineStartIndex : 0;
			int lvline = GetIndexOfLastVisibleLine();
			int fvchar = blockChanged != null ? blockChanged.CharStartIndex : 0;
			int lvchar = TextUtilities.GetLastCharIndexFromLineIndex(Text, lvline);

			if (blockChanged != null)
				blocks.RemoveRange(blocks.IndexOf(blockChanged), blocks.Count - blocks.IndexOf(blockChanged));

			int localLineCount = 1;
			int charStart = fvchar;
			int lineStart = fvline;
			for (int i = fvchar; i < Text.Length; i++) {
				if (Text[i] == '\n') {
					localLineCount += 1;
				}
				if (i == Text.Length - 1) {
					string blockText = Text.Substring(charStart);
					InnerTextBlock block = new InnerTextBlock(
						charStart,
						i, lineStart,
						lineStart + TextUtilities.GetLineCount(blockText) - 1,
						LineHeight);
					block.RawText = block.GetSubString(Text);
					block.LineNumbers = GetFormattedLineNumbers(block.LineStartIndex, block.LineEndIndex);
					block.IsLast = true;

					foreach (InnerTextBlock b in blocks)
						if (b.LineStartIndex == block.LineStartIndex)
							throw new Exception();

					blocks.Add(block);
					FormatBlock(block, blocks.Count > 1 ? blocks[blocks.Count - 2] : null);
					break;
				}
				if (localLineCount > maxLineCountInBlock) {
					InnerTextBlock block = new InnerTextBlock(
						charStart,
						i,
						lineStart,
						lineStart + maxLineCountInBlock - 1,
						LineHeight);
					block.RawText = block.GetSubString(Text);
					block.LineNumbers = GetFormattedLineNumbers(block.LineStartIndex, block.LineEndIndex);

					foreach (InnerTextBlock b in blocks)
						if (b.LineStartIndex == block.LineStartIndex)
							throw new Exception();

					blocks.Add(block);
					FormatBlock(block, blocks.Count > 1 ? blocks[blocks.Count - 2] : null);

					charStart = i + 1;
					lineStart += maxLineCountInBlock;
					localLineCount = 1;

					if (i > lvchar)
						break;
				}
			}
		}

		// -----------------------------------------------------------
		// Rendering
		// -----------------------------------------------------------

		private void DrawBlocks() {
			if (!IsLoaded || renderCanvas == null)
				return;

			var dc = renderCanvas.GetContext();
			for (int i = 0; i < blocks.Count; i++) {
				InnerTextBlock block = blocks[i];
				Point blockPos = block.Position;
				double top = blockPos.Y - VerticalOffset;
				double bottom = top + blockHeight;
				if (top < ActualHeight && bottom > 0) {
					try {
						dc.DrawText(block.FormattedText, new Point(2 - HorizontalOffset, block.Position.Y - VerticalOffset));
					} catch {
						// Don't know why this exception is raised sometimes.
						// Reproduce steps:
						// - Sets a valid syntax highlighter on the box.
						// - Copy a large chunk of code in the clipboard.
						// - Paste it using ctrl+v and keep these buttons pressed.
					}
				}
			}
			dc.Close();
		}

		// -----------------------------------------------------------
		// Utilities
		// -----------------------------------------------------------

		/// <summary>
		/// Returns the index of the first visible text line.
		/// </summary>
		public int GetIndexOfFirstVisibleLine() {
			int guessedLine = (int)(VerticalOffset / lineHeight);
			return guessedLine > totalLineCount ? totalLineCount : guessedLine;
		}

		/// <summary>
		/// Returns the index of the last visible text line.
		/// </summary>
		public int GetIndexOfLastVisibleLine() {
			double height = VerticalOffset + ViewportHeight;
			int guessedLine = (int)(height / lineHeight);
			return guessedLine > totalLineCount - 1 ? totalLineCount - 1 : guessedLine;
		}

		/// <summary>
		/// Formats and Highlights the text of a block.
		/// </summary>
		private void FormatBlock(InnerTextBlock currentBlock, InnerTextBlock previousBlock) {
			currentBlock.FormattedText = GetFormattedText(currentBlock.RawText);
			if (CurrentHighlighter != null) {
				ThreadPool.QueueUserWorkItem(p => {
					int previousCode = previousBlock != null ? previousBlock.Code : -1;
					currentBlock.Code = CurrentHighlighter.Highlight(currentBlock.FormattedText, previousCode);
				});
			}
		}

		/// <summary>
		/// Returns a formatted text object from the given string
		/// </summary>
		private FormattedText GetFormattedText(string text) {
			FormattedText ft = new FormattedText(
				text,
				System.Globalization.CultureInfo.InvariantCulture,
				FlowDirection.LeftToRight,
				new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
				FontSize,
				Brushes.Black);

			ft.Trimming = TextTrimming.None;
			ft.LineHeight = lineHeight;

			return ft;
		}

		/// <summary>
		/// Returns a string containing a list of numbers separated with newlines.
		/// </summary>
		private FormattedText GetFormattedLineNumbers(int firstIndex, int lastIndex) {
			string text = "";
			for (int i = firstIndex + 1; i <= lastIndex + 1; i++)
				text += i.ToString() + "\n";
			text = text.Trim();

			FormattedText ft = new FormattedText(
				text,
				System.Globalization.CultureInfo.InvariantCulture,
				FlowDirection.LeftToRight,
				new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
				FontSize,
				new SolidColorBrush(Color.FromRgb(0x21, 0xA1, 0xD8)));

			ft.Trimming = TextTrimming.None;
			ft.LineHeight = lineHeight;
			ft.TextAlignment = TextAlignment.Right;

			return ft;
		}

		/// <summary>
		/// Returns the width of a text once formatted.
		/// </summary>
		private double GetFormattedTextWidth(string text) {
			FormattedText ft = new FormattedText(
				text,
				System.Globalization.CultureInfo.InvariantCulture,
				FlowDirection.LeftToRight,
				new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
				FontSize,
				Brushes.Black);

			ft.Trimming = TextTrimming.None;
			ft.LineHeight = lineHeight;

			return ft.Width;
		}

		// -----------------------------------------------------------
		// Dependency Properties
		// -----------------------------------------------------------

		public static readonly DependencyProperty IsLineNumbersMarginVisibleProperty = DependencyProperty.Register(
			"IsLineNumbersMarginVisible", typeof(bool), typeof(SyntaxHighlightBox), new PropertyMetadata(true));

		public bool IsLineNumbersMarginVisible {
			get { return (bool)GetValue(IsLineNumbersMarginVisibleProperty); }
			set { SetValue(IsLineNumbersMarginVisibleProperty, value); }
		}

		// -----------------------------------------------------------
		// Classes
		// -----------------------------------------------------------

		private class InnerTextBlock {
			public string RawText { get; set; }
			public FormattedText FormattedText { get; set; }
			public FormattedText LineNumbers { get; set; }
			public int CharStartIndex { get; private set; }
			public int CharEndIndex { get; private set; }
			public int LineStartIndex { get; private set; }
			public int LineEndIndex { get; private set; }
			public Point Position { get { return new Point(0, LineStartIndex * lineHeight); } }
			public bool IsLast { get; set; }
			public int Code { get; set; }

			private double lineHeight;

			public InnerTextBlock(int charStart, int charEnd, int lineStart, int lineEnd, double lineHeight) {
				CharStartIndex = charStart;
				CharEndIndex = charEnd;
				LineStartIndex = lineStart;
				LineEndIndex = lineEnd;
				this.lineHeight = lineHeight;
				IsLast = false;

			}

			public string GetSubString(string text) {
				return text.Substring(CharStartIndex, CharEndIndex - CharStartIndex + 1);
			}

			public override string ToString() {
				return string.Format("L:{0}/{1} C:{2}/{3} {4}",
					LineStartIndex,
					LineEndIndex,
					CharStartIndex,
					CharEndIndex,
					FormattedText.Text);
			}
		}
	}
}
