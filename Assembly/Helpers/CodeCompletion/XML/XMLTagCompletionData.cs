using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Assembly.Helpers.CodeCompletion.XML
{
    /// <summary>
    /// Wraps a CompletableXMLTag for displaying in AvalonEdit's completion window.
    /// </summary>
    public class XMLTagCompletionData : ICompletionData
    {
        private CompletableXMLTag _tag;
        private XMLCodeCompleter _completer;

        public XMLTagCompletionData(CompletableXMLTag tag, XMLCodeCompleter completer)
        {
            _tag = tag;
            _completer = completer;
        }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, Text);

            // Get the current line and try to suggest attributes
            DocumentLine currentLine = textArea.Document.GetLineByNumber(textArea.Caret.Line);
            string lineText = textArea.Document.GetText(currentLine.Offset, currentLine.Length);
            _completer.CompleteAttributeName(lineText, textArea.Caret.Offset - currentLine.Offset);
        }

        public object Description
        {
            get { return _tag.Description; }
        }

        public object Content
        {
            get { return _tag.Name; }
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
            get { return _tag.Name + " "; }
        }
    }
}
