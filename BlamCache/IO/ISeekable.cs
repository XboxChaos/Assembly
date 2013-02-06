using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.IO
{
    public interface ISeekable : IDisposable
    {
        bool SeekTo(long offset);
        void Skip(long count);
        bool EOF { get; }
        long Length { get; }
        long Position { get; }

        void Close();
    }
}
