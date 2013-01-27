using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.IO
{
    /// <summary>
    /// An IStreamManager which can open streams on a file.
    /// </summary>
    public class FileStreamManager : IStreamManager
    {
        private FileInfo _file;

        /// <summary>
        /// Constructs a new FileStreamManager.
        /// </summary>
        /// <param name="path">The path to the file which streams should be opened on.</param>
        public FileStreamManager(string path)
        {
            _file = new FileInfo(path);
        }

        /// <summary>
        /// Constructs a new FileStreamManager.
        /// </summary>
        /// <param name="file">Information about the file which streams should be opened on.</param>
        public FileStreamManager(FileInfo file)
        {
            _file = file;
        }

        /// <summary>
        /// Opens the file for reading.
        /// </summary>
        /// <returns>The stream that was opened.</returns>
        public Stream OpenRead()
        {
            return _file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <summary>
        /// Opens the file for writing.
        /// </summary>
        /// <returns>The stream that was opened.</returns>
        public Stream OpenWrite()
        {
            return _file.OpenWrite();
        }

        /// <summary>
        /// Opens the file for reading and writing.
        /// </summary>
        /// <returns>The stream that was opened.</returns>
        public Stream OpenReadWrite()
        {
            return _file.Open(FileMode.Open, FileAccess.ReadWrite);
        }
    }
}
