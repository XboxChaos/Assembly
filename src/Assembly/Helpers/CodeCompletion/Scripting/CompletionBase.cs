using System;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace Assembly.Helpers.CodeCompletion.Scripting
{
    public abstract class CompletionBase : ICompletionData
    {
        public System.Windows.Media.ImageSource Image { get; set; }

        public string Text { get; set; }

        public object Content
        {
            get { return Text; }
        }

        public object Description { get; set; }

        public double Priority { get; set; }

        public void Complete(TextArea textArea, ISegment completionSegment,
            EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, Text);
        }
    }
}
