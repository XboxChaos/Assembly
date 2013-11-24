using System.IO;

namespace Blamite.IO
{
	/// <summary>
	///     An IStreamManager which can open streams on a file.
	/// </summary>
	public class FileStreamManager : IStreamManager
	{
		private readonly Endian _endian;
		private readonly FileInfo _file;

		/// <summary>
		///     Constructs a new FileStreamManager.
		/// </summary>
		/// <param name="path">The path to the file which streams should be opened on.</param>
		/// <param name="endian">The endianness of the file.</param>
		public FileStreamManager(string path, Endian endian)
			: this(new FileInfo(path), endian)
		{
		}

		/// <summary>
		///     Constructs a new FileStreamManager.
		/// </summary>
		/// <param name="file">Information about the file which streams should be opened on.</param>
		/// <param name="endian">The endianness of the file.</param>
		public FileStreamManager(FileInfo file, Endian endian)
		{
			_file = file;
			_endian = endian;
		}

		/// <summary>
		///     Opens the file for reading.
		/// </summary>
		/// <returns>The stream that was opened.</returns>
		public IReader OpenRead()
		{
			return new EndianReader(_file.Open(FileMode.Open, FileAccess.Read, FileShare.Read), _endian);
		}

		/// <summary>
		///     Opens the file for writing.
		/// </summary>
		/// <returns>The stream that was opened.</returns>
		public IWriter OpenWrite()
		{
			return new EndianWriter(_file.OpenWrite(), _endian);
		}

		/// <summary>
		///     Opens the file for reading and writing.
		/// </summary>
		/// <returns>The stream that was opened.</returns>
		public IStream OpenReadWrite()
		{
			return new EndianStream(_file.Open(FileMode.Open, FileAccess.ReadWrite), _endian);
		}
	}
}