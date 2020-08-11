using System;
using Blamite;
using Antlr4.Runtime;
using Blamite.Blam.Scripting.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime.Misc;

namespace Blamite.Blam.Scripting.Compiler
{
    [Serializable]
    public class CompilerException: Exception
    {
        public int Line { get; set; }

        public int Column { get; set; }

        public string Text { get; set; }

        public int TextLength { get { return Text.Length; } }

        public CompilerException() : base()
        {
        }

        public CompilerException(string message, ParserRuleContext context) : base(message)
        {
            SetCommonProperties(context);
            Text = GetOriginalText(context);
        }

        public CompilerException(string message, BS_ReachParser.CallContext context) : base(message)
        {
            SetCommonProperties(context);
            Text = context.functionID().GetText();
        }

        public CompilerException(string message, BS_ReachParser.BranchContext context) : base(message)
        {
            SetCommonProperties(context);
            Text = "branch";
        }

        public CompilerException(string message, string text, int line, int column) : base(message)
        {
            Line = line;
            Column = column;
            Text = text;
        }

        public CompilerException( string message, Exception innerException) : base(message, innerException)
        {
        }

        private void SetCommonProperties(ParserRuleContext context)
        {
            Line = context.Start.Line;
            Column = context.Start.Column;
        }

        private string GetOriginalText(ParserRuleContext context)
        {
            int start = context.Start.StartIndex;
            int stop = context.Stop.StopIndex;
            Interval interval = new Interval(start, stop);
            return context.Start.TokenSource.InputStream.GetText(interval);
        }
    }
}
