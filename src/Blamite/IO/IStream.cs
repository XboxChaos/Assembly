using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.IO
{
    /// <summary>
    /// A stream which can be both read from and written to.
    /// </summary>
    public interface IStream : IReader, IWriter
    {
        // Nothing to see here, move along...
    }
}
