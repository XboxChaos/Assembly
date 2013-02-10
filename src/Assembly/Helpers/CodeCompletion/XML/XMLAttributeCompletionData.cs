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
    /// Wraps a CompletableXMLAttribute for displaying in AvalonEdit's completion window.
    /// </summary>
    public class XMLAttributeCompletionData : ICompletionData
    {
        private CompletableXMLAttribute _attribute;
        private XMLCodeCompleter _completer;

        public XMLAttributeCompletionData(CompletableXMLAttribute attribute, XMLCodeCompleter completer)
        {
            _attribute = attribute;
            _completer = completer;
        }

        public object Description
        {
            get { return _attribute.Description; }
        }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, Text);
            textArea.Caret.Column -= 1; // Move the cursor inside the quotes that get inserted

            // Get the current line and try to suggest values
            DocumentLine currentLine = textArea.Document.GetLineByNumber(textArea.Caret.Line);
            string lineText = textArea.Document.GetText(currentLine.Offset, currentLine.Length);
            _completer.CompleteAttributeValue(lineText, textArea.Caret.Offset - currentLine.Offset);
        }

        public object Content
        {
            get { return _attribute.Name; }
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
            get { return _attribute.Name + "=\"\""; }
        }
    }
}
