using System;
using System.Diagnostics;
using Antlr4.Runtime;

namespace Blamite.Blam.Scripting.Compiler
{
    public enum CompilerDatumAction
    {
        Open,
        Close,
        Link
    }

    public enum CompilerContextAction
    {
        Enter,
        Exit
    }

    public enum StackAction
    {
        Push,
        Pop
    }

    public class ScriptCompilerLogger
    {
        private readonly TraceListener _listener;

        public ScriptCompilerLogger(TraceListener listener)
        {
            _listener = listener;
        }

        #region Private Functions

        private void WriteEntry(string level, string message)
        {
            _listener.WriteLine($"[{level}], {message}");
        }

        private void WriteContext(string level, ParserRuleContext context, string message)
        {
            string innerMessage = $"Line: {context.Start.Line}, {message}";
            WriteEntry(level, innerMessage);
        }

        private void WriteContext(string level, ParserRuleContext context, CompilerContextAction action)
        {
            string innerMessage = $"Line: {context.Start.Line}, {action}: \"{context.GetText().Trim('"')}\"";
            WriteEntry(level, innerMessage);
        }

        private void WriteContext(string level, ParserRuleContext context, CompilerContextAction action, string name)
        {
            string innerMessage = $"Line: {context.Start.Line}, {action}: \"{name.Trim('"')}\"";
            WriteEntry(level, innerMessage);
        }

        private void WriteContextIndent(string level, ParserRuleContext context, CompilerContextAction action)
        {
            if (action == CompilerContextAction.Exit)
            {
                DecreaseIndent();
            }

            WriteContext(level, context, action);

            if (action == CompilerContextAction.Enter)
            {
                IncreaseIndent();
            }
        }

        private void WriteContextIndent(string level, ParserRuleContext context, CompilerContextAction action, string name)
        {
            if (action == CompilerContextAction.Exit)
            {
                DecreaseIndent();
            }

            WriteContext(level, context, action, name);

            if (action == CompilerContextAction.Enter)
            {
                IncreaseIndent();
            }
        }

        private void IncreaseIndent()
        {
            _listener.IndentLevel++;
        }

        private void DecreaseIndent()
        {
            _listener.IndentLevel--;
        }

        #endregion

        public void Flush()
        {
            _listener.Flush();
        }

        public void NewLine()
        {
            _listener.WriteLine("");
        }

        public void Error(Exception ex)
        {
            int currentIndentLevel = _listener.IndentLevel;
            _listener.IndentLevel = 0;
            WriteEntry("ERROR", ex.Message);
            _listener.IndentLevel = currentIndentLevel;
        }

        public void Error(CompilerException ex)
        {
            int currentIndentLevel = _listener.IndentLevel;
            _listener.IndentLevel = 0;
            string message = $"Line: {ex.Line}, Column: {ex.Column}, {ex.Message}";
            WriteEntry("ERROR", message);
            _listener.IndentLevel = currentIndentLevel;
        }

        public void Error(string message)
        {
            int currentIndentLevel = _listener.IndentLevel;
            _listener.IndentLevel = 0;
            WriteEntry("ERROR", message);
            _listener.IndentLevel = currentIndentLevel;
        }

        public void Information(string message)
        {
            WriteEntry("INFORMATION", message);
        }

        public void Warning(string message)
        {
            WriteEntry("WARNING", message);
        }


        #region Stacks

        public void Datum(int index, CompilerDatumAction action)
        {
            string innerMessage = $"Index: {index}, {action}";
            WriteEntry("DATUM", innerMessage);
        }

        public void Datum(int index, CompilerDatumAction action, string message)
        {
            string innerMessage = $"Index: {index}, {action}, {message}";
            WriteEntry("DATUM", innerMessage);
        }

        public void TypeStack(string type, int index, StackAction action)
        {
            string innerMessage = $"{action}: \"{type}\", Index: {index}";
            WriteEntry("TYPE STACK", innerMessage);
        }

        #endregion


        #region Contexts

        public void Script(BS_ReachParser.ScriptDeclarationContext context, CompilerContextAction action)
        {
            string name = context.scriptID().GetText();
            WriteContextIndent("SCRIPT", context, action, name);
        }

        public void Global(BS_ReachParser.GlobalDeclarationContext context, CompilerContextAction action)
        {
            string name = context.ID().GetText();
            WriteContextIndent("GLOBAL", context, action, name);
        }

        public void Branch(ParserRuleContext context, CompilerContextAction action)
        {
            WriteContextIndent("BRANCH", context, action, "branch");
        }

        public void Cond(ParserRuleContext context, CompilerContextAction action)
        {
            WriteContextIndent("COND", context, action, "cond");
        }

        public void CondGroup(ParserRuleContext context, CompilerContextAction action)
        {
            WriteContextIndent("COND GROUP", context, action, "cond group");
        }

        public void GlobalReference(ParserRuleContext context, CompilerContextAction action)
        {
            WriteContext("GLOBAL REFERENCE", context, action);
        }

        public void Literal(ParserRuleContext context, CompilerContextAction action)
        {
            WriteContext("LITERAL", context, action);
        }

        public void Call(BS_ReachParser.CallContext context, CompilerContextAction action)
        {
            string name = context.callID().GetText();
            WriteContextIndent("CALL", context, action, name);
        }

        #endregion
    }
}
