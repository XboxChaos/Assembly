using System.Collections.Generic;

namespace Blamite.IO.IoStore
{
	/// <summary>
	///     The optional file path index carried by a .utoc file, mapping cooked file paths to the
	///     chunks holding their contents.
	/// </summary>
	/// <remarks>
	///     <para>
	///         The index is stored as a mount point string followed by three arrays and a string
	///         table:
	///     </para>
	///     <code>
	///     string   mount point
	///     uint32   directory count;  directory count  * { name, first child, next sibling, first file }
	///     uint32   file count;       file count       * { name, next file, user data }
	///     uint32   string count;     string count     * string
	///     </code>
	///     <para>
	///         Directories and files form a linked tree rather than a nested structure: every "name"
	///         is an index into the string table and every link is an index into the matching array,
	///         with <c>0xFFFFFFFF</c> as the "no such entry" sentinel. A file entry's user data is
	///         the chunk's index in the containing table of contents.
	///     </para>
	///     <para>
	///         Not every container has one - override containers store a directory index size of
	///         zero and are addressed purely by chunk ID.
	///     </para>
	/// </remarks>
	public class IoDirectoryIndex
	{
		/// <summary>
		///     The value a directory, file or string index takes when it refers to nothing.
		/// </summary>
		public const uint InvalidIndex = 0xFFFFFFFF;

		private readonly Dictionary<string, uint> _chunkIndicesByPath =
			new Dictionary<string, uint>(System.StringComparer.OrdinalIgnoreCase);

		private DirectoryEntry[] _directories;
		private FileEntry[] _files;
		private string[] _strings;

		private IoDirectoryIndex()
		{
		}

		/// <summary>
		///     Gets the mount point exactly as it is stored, e.g. <c>../../../Meteorite/Content/</c>.
		/// </summary>
		public string MountPoint { get; private set; }

		/// <summary>
		///     Gets the mount point reduced to a path prefix: leading <c>../</c> and <c>/</c>
		///     components removed, trailing separator kept. Every path in <see cref="Paths" />
		///     already starts with this.
		/// </summary>
		public string MountPointPrefix { get; private set; }

		/// <summary>
		///     Gets the number of directories described by the index.
		/// </summary>
		public int DirectoryCount
		{
			get { return _directories.Length; }
		}

		/// <summary>
		///     Gets the number of files described by the index.
		/// </summary>
		public int FileCount
		{
			get { return _files.Length; }
		}

		/// <summary>
		///     Gets every file path in the index, mount point included, using <c>/</c> separators.
		/// </summary>
		public ICollection<string> Paths
		{
			get { return _chunkIndicesByPath.Keys; }
		}

		/// <summary>
		///     Looks up the table-of-contents chunk index a file path resolves to.
		/// </summary>
		/// <param name="path">
		///     The path to look up. Backslashes are accepted, leading separators are ignored, and
		///     matching is case-insensitive. The mount point prefix may be included or left off.
		/// </param>
		/// <param name="chunkIndex">The chunk index the path resolves to, if it was found.</param>
		/// <returns><c>true</c> if the path is present and refers to a valid chunk.</returns>
		public bool TryGetChunkIndex(string path, out int chunkIndex)
		{
			chunkIndex = -1;
			if (string.IsNullOrEmpty(path))
				return false;

			string normalized = NormalizePath(path);
			uint userData;
			if (!_chunkIndicesByPath.TryGetValue(normalized, out userData) &&
				!_chunkIndicesByPath.TryGetValue(MountPointPrefix + normalized, out userData))
				return false;

			// A file entry is allowed to point at nothing at all, which is not a lookup failure in
			// the file's sense but is certainly not a chunk either.
			if (userData == InvalidIndex || userData > int.MaxValue)
				return false;

			chunkIndex = (int) userData;
			return true;
		}

		/// <summary>
		///     Reads a directory index from a stream.
		/// </summary>
		/// <param name="reader">The stream to read from, positioned at the start of the index.</param>
		/// <param name="size">The size of the index in bytes, taken from the .utoc header.</param>
		/// <returns>The directory index that was read.</returns>
		/// <exception cref="IoStoreException">Thrown if the index is malformed or overruns its size.</exception>
		public static IoDirectoryIndex Read(IReader reader, long size)
		{
			long start = reader.Position;
			long end = start + size;
			if (end > reader.Length)
			{
				throw new IoStoreException(string.Format(
					"The directory index claims to be 0x{0:X} bytes at 0x{1:X}, which runs past the end of the " +
					"0x{2:X} byte table of contents.", size, start, reader.Length));
			}

			Endian originalEndianness = reader.Endianness;
			try
			{
				reader.Endianness = Endian.LittleEndian;

				var index = new IoDirectoryIndex();
				index.MountPoint = ReadString(reader, end);
				index.MountPointPrefix = BuildMountPointPrefix(index.MountPoint);

				index._directories = ReadDirectories(reader, end);
				index._files = ReadFiles(reader, end);
				index._strings = ReadStrings(reader, end);

				if (reader.Position != end)
				{
					throw new IoStoreException(string.Format(
						"The directory index ended at 0x{0:X} but its declared size runs to 0x{1:X}.",
						reader.Position, end));
				}

				index.BuildPaths();
				return index;
			}
			finally
			{
				reader.Endianness = originalEndianness;
			}
		}

		/// <summary>
		///     Reads the directory entry array.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="end">The offset the index must not read past.</param>
		/// <returns>The directory entries that were read.</returns>
		private static DirectoryEntry[] ReadDirectories(IReader reader, long end)
		{
			int count = ReadCount(reader, end, 4*sizeof(uint), "directory");
			var entries = new DirectoryEntry[count];
			for (int i = 0; i < count; i++)
			{
				entries[i].NameIndex = reader.ReadUInt32();
				entries[i].FirstChildIndex = reader.ReadUInt32();
				entries[i].NextSiblingIndex = reader.ReadUInt32();
				entries[i].FirstFileIndex = reader.ReadUInt32();
			}
			return entries;
		}

		/// <summary>
		///     Reads the file entry array.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="end">The offset the index must not read past.</param>
		/// <returns>The file entries that were read.</returns>
		private static FileEntry[] ReadFiles(IReader reader, long end)
		{
			int count = ReadCount(reader, end, 3*sizeof(uint), "file");
			var entries = new FileEntry[count];
			for (int i = 0; i < count; i++)
			{
				entries[i].NameIndex = reader.ReadUInt32();
				entries[i].NextFileIndex = reader.ReadUInt32();
				entries[i].UserData = reader.ReadUInt32();
			}
			return entries;
		}

		/// <summary>
		///     Reads the string table.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="end">The offset the index must not read past.</param>
		/// <returns>The strings that were read.</returns>
		private static string[] ReadStrings(IReader reader, long end)
		{
			// Strings are variable length, so the up-front check can only assume the smallest
			// possible string - its four byte length prefix and nothing else. ReadString then checks
			// each string individually as it goes.
			int count = ReadCount(reader, end, sizeof(int), "string");
			var strings = new string[count];
			for (int i = 0; i < count; i++)
				strings[i] = ReadString(reader, end);
			return strings;
		}

		/// <summary>
		///     Reads an array element count and checks that an array of that size actually fits.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="end">The offset the index must not read past.</param>
		/// <param name="elementSize">The minimum size of one element in bytes.</param>
		/// <param name="description">What the array holds, for error messages.</param>
		/// <returns>The element count that was read.</returns>
		private static int ReadCount(IReader reader, long end, int elementSize, string description)
		{
			if (reader.Position + sizeof(uint) > end)
			{
				throw new IoStoreException(string.Format(
					"The directory index ends before its {0} count at 0x{1:X}.", description, reader.Position));
			}

			uint count = reader.ReadUInt32();
			if (count > int.MaxValue || reader.Position + (long) count*elementSize > end)
			{
				throw new IoStoreException(string.Format(
					"The directory index declares {0} {1} entries at 0x{2:X}, which do not fit before its end at " +
					"0x{3:X}.", count, description, reader.Position, end));
			}
			return (int) count;
		}

		/// <summary>
		///     Reads one length-prefixed string.
		/// </summary>
		/// <param name="reader">The stream to read from, already set to little-endian.</param>
		/// <param name="end">The offset the string must not read past.</param>
		/// <returns>The string that was read, without its terminator.</returns>
		/// <remarks>
		///     The prefix is a signed length whose sign selects the encoding, and which counts the
		///     terminating null in both cases:
		///     <list type="bullet">
		///         <item><description>0 - the string is empty and no bytes follow.</description></item>
		///         <item><description>positive - that many <em>bytes</em> of ASCII follow.</description></item>
		///         <item><description>negative - the negated value is a count of UTF-16LE <em>code units</em>, so twice as many bytes follow.</description></item>
		///     </list>
		///     The stream is advanced by exactly the declared field size regardless of where the
		///     terminator actually turns up, so an embedded null cannot desynchronize the parse.
		/// </remarks>
		private static string ReadString(IReader reader, long end)
		{
			if (reader.Position + sizeof(int) > end)
				throw new IoStoreException(string.Format("A directory index string at 0x{0:X} has no length.", reader.Position));

			int length = reader.ReadInt32();
			if (length == 0)
				return string.Empty;

			long start = reader.Position;
			long byteCount;
			if (length > 0)
				byteCount = length;
			else if (length == int.MinValue)
				throw new IoStoreException(string.Format("A directory index string at 0x{0:X} has an unusable length.", start));
			else
				byteCount = -(long) length*2;

			if (start + byteCount > end)
			{
				throw new IoStoreException(string.Format(
					"A directory index string at 0x{0:X} claims 0x{1:X} bytes, which run past the end of the index " +
					"at 0x{2:X}.", start, byteCount, end));
			}

			string value = length > 0 ? reader.ReadAscii((int) byteCount) : reader.ReadUTF16((int) byteCount);

			// The fixed-size string readers stop at the first null and do not necessarily leave the
			// stream at the end of the field, so put it there explicitly.
			reader.SeekTo(start + byteCount);
			return value;
		}

		/// <summary>
		///     Reduces a stored mount point to a usable path prefix.
		/// </summary>
		/// <param name="mountPoint">The mount point as stored in the index.</param>
		/// <returns>The mount point with its leading relative components stripped.</returns>
		private static string BuildMountPointPrefix(string mountPoint)
		{
			if (string.IsNullOrEmpty(mountPoint))
				return string.Empty;

			string prefix = mountPoint.Replace('\\', '/');

			// Mount points are written relative to the cooked directory, e.g. "../../../" or
			// "../../../Meteorite/Content/". Dropping the relative part leaves a prefix that lines
			// up across every container in the game.
			while (prefix.StartsWith("../", System.StringComparison.Ordinal))
				prefix = prefix.Substring(3);
			prefix = prefix.TrimStart('/');

			if (prefix.Length > 0 && !prefix.EndsWith("/", System.StringComparison.Ordinal))
				prefix += "/";
			return prefix;
		}

		/// <summary>
		///     Normalizes a path for lookup.
		/// </summary>
		/// <param name="path">The path to normalize.</param>
		/// <returns>The path using forward slashes and no leading separator.</returns>
		private static string NormalizePath(string path)
		{
			return path.Replace('\\', '/').TrimStart('/');
		}

		/// <summary>
		///     Walks the directory tree and records the full path of every file entry.
		/// </summary>
		/// <remarks>
		///     The walk is iterative. Cooked Unreal trees are deep enough that recursion is a real
		///     stack-overflow risk, and a corrupt index could make it unbounded. The visit counters
		///     below turn a cyclic link list into an exception instead of a hang.
		/// </remarks>
		private void BuildPaths()
		{
			if (_directories.Length == 0)
				return;

			var pending = new Stack<PendingDirectory>();
			pending.Push(new PendingDirectory(0, MountPointPrefix));

			int directoriesVisited = 0;
			while (pending.Count > 0)
			{
				if (++directoriesVisited > _directories.Length)
					throw new IoStoreException("The directory index's directory tree contains a cycle.");

				PendingDirectory current = pending.Pop();
				DirectoryEntry directory = _directories[current.Index];

				AddFiles(directory.FirstFileIndex, current.Path);

				// Children are a sibling-linked list hanging off the first child.
				uint childIndex = directory.FirstChildIndex;
				int childrenVisited = 0;
				while (childIndex != InvalidIndex)
				{
					if (childIndex >= _directories.Length)
					{
						throw new IoStoreException(string.Format(
							"The directory index refers to directory {0}, but only {1} exist.", childIndex,
							_directories.Length));
					}
					if (++childrenVisited > _directories.Length)
						throw new IoStoreException("The directory index's sibling list contains a cycle.");

					DirectoryEntry child = _directories[childIndex];
					pending.Push(new PendingDirectory(childIndex, current.Path + GetName(child.NameIndex) + "/"));
					childIndex = child.NextSiblingIndex;
				}
			}
		}

		/// <summary>
		///     Records every file in one directory's file list.
		/// </summary>
		/// <param name="firstFileIndex">The index of the directory's first file entry.</param>
		/// <param name="directoryPath">The full path of the directory, ending in a separator.</param>
		private void AddFiles(uint firstFileIndex, string directoryPath)
		{
			uint fileIndex = firstFileIndex;
			int filesVisited = 0;
			while (fileIndex != InvalidIndex)
			{
				if (fileIndex >= _files.Length)
				{
					throw new IoStoreException(string.Format(
						"The directory index refers to file {0}, but only {1} exist.", fileIndex, _files.Length));
				}
				if (++filesVisited > _files.Length)
					throw new IoStoreException("The directory index's file list contains a cycle.");

				FileEntry file = _files[fileIndex];
				_chunkIndicesByPath[directoryPath + GetName(file.NameIndex)] = file.UserData;
				fileIndex = file.NextFileIndex;
			}
		}

		/// <summary>
		///     Resolves a name index against the index's string table.
		/// </summary>
		/// <param name="nameIndex">The name index to resolve.</param>
		/// <returns>The name, or an empty string if the entry is unnamed.</returns>
		private string GetName(uint nameIndex)
		{
			// The root directory is unnamed, which is what makes the mount point the whole prefix.
			if (nameIndex == InvalidIndex)
				return string.Empty;
			if (nameIndex >= _strings.Length)
			{
				throw new IoStoreException(string.Format(
					"The directory index refers to string {0}, but only {1} exist.", nameIndex, _strings.Length));
			}
			return _strings[nameIndex];
		}

		/// <summary>
		///     One directory entry as it is stored on disk.
		/// </summary>
		private struct DirectoryEntry
		{
			public uint NameIndex;
			public uint FirstChildIndex;
			public uint NextSiblingIndex;
			public uint FirstFileIndex;
		}

		/// <summary>
		///     One file entry as it is stored on disk.
		/// </summary>
		private struct FileEntry
		{
			public uint NameIndex;
			public uint NextFileIndex;
			public uint UserData;
		}

		/// <summary>
		///     A directory which has been reached but not yet walked, and the path it sits at.
		/// </summary>
		private struct PendingDirectory
		{
			public PendingDirectory(uint index, string path)
			{
				Index = index;
				Path = path;
			}

			public readonly uint Index;
			public readonly string Path;
		}
	}
}
