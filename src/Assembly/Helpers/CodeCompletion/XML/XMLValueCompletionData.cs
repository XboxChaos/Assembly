using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Assembly.Helpers.CodeCompletion.XML
{
	/// <summary>
	///     Wraps a CompletableXMLValue for displaying in AvalonEdit's completion window.
	/// </summary>
	public class XMLValueCompletionData : ICompletionData
	{
		private readonly CompletableXMLValue _attributeValue;

		public XMLValueCompletionData(CompletableXMLValue attributeValue)
		{
			_attributeValue = attributeValue;
		}

		public object Description
		{
			get { return _attributeValue.Description; }
		}

		public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
		{
			textArea.Document.Replace(completionSegment, Text);
		}

		public object Content
		{
			get { return _attributeValue.Name; }
		}

		public ImageSource Image
		{
			get { return null; }
		}

		public double Priority
		{
			get { return 0; }
		}

		public string Text
		{
			get { return _attributeValue.Name; }
		}
	}
}