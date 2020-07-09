using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blamite.Blam.Scripting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.Editors
{
    public class ScriptCompletion : ICompletionData
    {
        public ScriptCompletion(FunctionInfo info)
        {
            Text = info.Name;
            StringBuilder description = new StringBuilder();
            description.Append($"[FUNCTION] | Type: {info.ReturnType}");
            if(info.ParameterTypes.Length > 0)
            {
                description.Append(" | Parameters: ");
                description.Append(string.Join(", ", info.ParameterTypes));
            }
            Description = description.ToString();
            Priority = 1.0;
        }

        public ScriptCompletion(GlobalInfo info)
        {
            Text = info.Name;
            Description = $"[ENGINE GLOBAL] | Type: {info.ReturnType}";
            Priority = 1.0;
        }

        public System.Windows.Media.ImageSource Image
        {
            get { return null; }
        }

        public string Text { get; private set; }

        public object Content
        {
            get { return Text; }
        }

        public object Description { get; private set; }

        public double Priority { get; private set; }

        public void Complete(TextArea textArea, ISegment completionSegment,
            EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, Text);
        }
    }
}
