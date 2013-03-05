using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.IO
{
    public interface IBaseStream : IDisposable
    {
        bool SeekTo(long offset);
        void Skip(long count);
        bool EOF { get; }
        long Length { get; }
        long Position { get; }
        Endian Endianness { get; set; }

        void Close();
    }
}
