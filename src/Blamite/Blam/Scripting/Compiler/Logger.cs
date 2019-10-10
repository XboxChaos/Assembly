using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Blamite.Blam.Scripting.Compiler
{

    public class Logger
    {
        private string _filePath;
        private StreamWriter _sw;

        public Logger(StreamWriter writer)
        {
            _sw = writer;
        }

        public void WriteLine(string level, string message)
        {
            _sw.WriteLine("{0,-20}{1,-30}", $"[{level}]", message);
        }

        public void WriteNewLine()
        {
                _sw.WriteLine();
        }
    }
}
