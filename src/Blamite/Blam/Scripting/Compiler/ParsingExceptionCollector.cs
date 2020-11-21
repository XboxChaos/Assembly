using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Scripting.Compiler
{
    public class ParsingExceptionInformation
    {
        public ParsingExceptionInformation(string message, int line, int column, RecognitionException exception)
        {
            Message = message;
            Line = line;
            Column = column;
            Exception = exception;
        }

        public string Message { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }
        public RecognitionException Exception { get; private set; }
    }

    public class ParsingExceptionCollector : BaseErrorListener, IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
    {
        private readonly List<ParsingExceptionInformation> _exceptions = new List<ParsingExceptionInformation>();

        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            _exceptions.Add(new ParsingExceptionInformation(msg, line, charPositionInLine, e));
        }

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            _exceptions.Add(new ParsingExceptionInformation(msg, line, charPositionInLine, e));
        }
        
        public bool ContainsExceptions { get { return _exceptions.Count > 0; } }

        public int ExceptionCount { get { return _exceptions.Count; } }

        public IEnumerable<ParsingExceptionInformation> GetExceptions()
        {
            return _exceptions;
        }
    }
}
