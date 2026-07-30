using System;
using System.Collections.Generic;
using System.IO;
using Blamite.IO;
using Blamite.IO.IoStore;
using Blamite.Serialization;
using Blamite.Util;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     The tag table of a Campaign Evolved tag namespace: every Blam tag found across a folder
	///     of mounted IoStore containers.
	/// </summary>
	/// <remarks>
	///     <para>
	///         A Campaign Evolved "cache file" is not one file. It is a set of IoStore
	///         <c>.utoc</c>/<c>.ucas</c> container pairs that together present one tag namespace - a
	///         mod's several <c>_P</c> override sets, or the shipped game's <c>Meteorite/Content/Paks/</c>
	///         (28 pairs) - and later-mounted containers override earlier ones by UE package ID (see
	///         <c>ce-format-spec.md</c> sections 0 and 1). This mounts every <c>.utoc</c> found
	///         directly inside a folder, in ordinal-case-insensitive filename order, and lets each one
	///         overwrite any tag an earlier container already produced for the same package ID -
	///         "later" meaning later in that sort, since nothing about a mod's filenames encodes real
	///         pak-priority numbering the way the game's own <c>pakchunkN</c> scheme would. This is
	///         a simplification, not the game's actual override resolution rule, and is documented as
	///         such rather than silently assumed correct.
	///     </para>
	///     <para>
	///         A tag is any <c>BulkData</c> (IoChunkType 2) chunk whose first 64 bytes look like a
	///         Blam tag header - <c>p[60..64]</c> is <c>BLAM</c> or <c>MALB</c> (section 3.1). Only
	///         those 64 bytes are read here: the group four-CC at +0x30 (byte order given by the
	///         signature at +0x3C) and nothing else. The full payload, header included, is kept
	///         verbatim on the resulting <see cref="FifthGenTag" /> for a tag body parser to read the
	///         rest of.
	///     </para>
	///     <para>
	///         Naming has no single reliable source. A mounted mod's containers carry a directory
	///         index of size zero on every one sampled, and a real container's <c>ContainerHeader</c>
	///         chunk (investigated, not assumed - see <see cref="FifthGenNameOrigin.ContainerHeader" />)
	///         turns out to hold no strings at all. What is tried, in order, and recorded per tag via
	///         <see cref="FifthGenNameOrigin" />:
	///     </para>
	///     <list type="number">
	///         <item><description>the mounting container's own directory index, when it has one;</description></item>
	///         <item><description>the container's ContainerHeader chunk (checked; never actually produces a name);</description></item>
	///         <item><description>the container's own filename with a trailing "_P" stripped, when the container holds exactly one tag;</description></item>
	///         <item><description>the tag's raw package ID, as a last resort.</description></item>
	///     </list>
	/// </remarks>
	public class FifthGenTagTable : TagTable
	{
		private readonly List<FifthGenTag> _tags = new List<FifthGenTag>();
		private readonly List<string> _names = new List<string>();
		private readonly List<FifthGenNameOrigin> _nameOrigins = new List<FifthGenNameOrigin>();
		private readonly Dictionary<ulong, int> _tagIndicesByPackageId = new Dictionary<ulong, int>();
		private readonly List<ITagGroup> _groups = new List<ITagGroup>();
		private readonly Dictionary<int, FifthGenGroupInfo> _groupsByMagic = new Dictionary<int, FifthGenGroupInfo>();
		private readonly List<FifthGenMountedContainer> _containers = new List<FifthGenMountedContainer>();
		private readonly List<string> _warnings = new List<string>();
		private readonly List<ITagInterop> _interops = new List<ITagInterop>();

		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenTagTable" /> class, mounting
		///     every IoStore container found in a folder.
		/// </summary>
		/// <param name="directoryPath">The folder to mount every <c>.utoc</c> file from.</param>
		/// <param name="buildInfo">The engine description supplying the "toc header" layout.</param>
		/// <exception cref="IoStoreException">
		///     Thrown if the folder holds no <c>.utoc</c> files, or if none of the ones it does hold
		///     produce a single Blam tag between them.
		/// </exception>
		public FifthGenTagTable(string directoryPath, EngineDescription buildInfo)
		{
			Load(directoryPath, buildInfo);
		}

		/// <summary>
		///     Gets a read-only list of the tag groups found while mounting.
		/// </summary>
		public IList<ITagGroup> Groups
		{
			get { return _groups; }
		}

		/// <summary>
		///     Gets every container that was successfully mounted, in the order they were mounted -
		///     later entries override earlier ones by package ID.
		/// </summary>
		public IList<FifthGenMountedContainer> MountedContainers
		{
			get { return _containers; }
		}

		/// <summary>
		///     Gets non-fatal problems noticed while mounting - a container that failed to open, or
		///     a package ID that more than one container claimed.
		/// </summary>
		public IList<string> Warnings
		{
			get { return _warnings; }
		}

		/// <summary>
		///     Gets a name source built from the same mounting pass that built this table, one entry
		///     per tag, each recording where its name came from (see <see cref="FifthGenNameOrigin" />).
		/// </summary>
		public FifthGenFileNameSource FileNames { get; private set; }

		public IList<ITagInterop> Interops
		{
			get { return _interops; }
		}

		public override ITag this[int index]
		{
			get { return _tags[index]; }
		}

		public override int Count
		{
			get { return _tags.Count; }
		}

		public override IEnumerator<ITag> GetEnumerator()
		{
			return _tags.GetEnumerator();
		}

		/// <summary>
		///     Campaign Evolved has no notion of a singleton "global" tag distinct from any other -
		///     every tag is just a package - so this always returns <c>null</c>.
		/// </summary>
		public override ITag GetGlobalTag(int magic)
		{
			return null;
		}

		public override ITag AddTag(int groupMagic, uint baseSize, IStream stream)
		{
			throw new NotSupportedException("Adding new Campaign Evolved tags is not supported yet.");
		}

		private void Load(string directoryPath, EngineDescription buildInfo)
		{
			string[] tocPaths = DiscoverContainerPaths(directoryPath);
			if (tocPaths.Length == 0)
			{
				throw new IoStoreException(string.Format(
					"\"{0}\" has no .utoc files to mount as a Campaign Evolved tag namespace.", directoryPath));
			}

			foreach (string tocPath in tocPaths)
			{
				FifthGenMountedContainer mounted = MountContainer(tocPath, buildInfo);
				if (mounted == null)
					continue;

				_containers.Add(mounted);
				ScanContainerForTags(mounted);
			}

			if (_tags.Count == 0)
			{
				throw new IoStoreException(string.Format(
					"Mounted {0} IoStore container(s) under \"{1}\", but none of them held a single Blam tag - " +
					"no BulkData chunk in any of them started with a BLAM/MALB header.",
					_containers.Count, directoryPath));
			}

			FileNames = new FifthGenFileNameSource(_names, _nameOrigins);
		}

		/// <summary>
		///     Finds every <c>.utoc</c> file directly inside a folder, in the order containers should
		///     be mounted (and therefore overridden) in.
		/// </summary>
		/// <param name="directoryPath">The folder to search. Not searched recursively.</param>
		/// <returns>The paths that were found, sorted ordinal-case-insensitively.</returns>
		private static string[] DiscoverContainerPaths(string directoryPath)
		{
			if (!Directory.Exists(directoryPath))
				return new string[0];

			string[] paths = Directory.GetFiles(directoryPath, "*.utoc", SearchOption.TopDirectoryOnly);

			// Ordinal-case-insensitive because the containers this mounts are built on a
			// case-preserving-but-insensitive filesystem (Windows) even when this runs somewhere
			// case-sensitive; sorting any other way would mount in a different order than the game's
			// own filesystem view of the same folder would produce.
			Array.Sort(paths, StringComparer.OrdinalIgnoreCase);
			return paths;
		}

		/// <summary>
		///     Opens one container, recording a warning and returning <c>null</c> instead of throwing
		///     if it turns out not to be a readable IoStore container.
		/// </summary>
		private FifthGenMountedContainer MountContainer(string tocPath, EngineDescription buildInfo)
		{
			try
			{
				IoStoreContainer container = IoStoreContainer.Open(tocPath, buildInfo.Layouts, null);
				return new FifthGenMountedContainer(tocPath, container);
			}
			catch (IoStoreException ex)
			{
				_warnings.Add(string.Format("Skipped \"{0}\": {1}", tocPath, ex.Message));
				return null;
			}
			catch (IOException ex)
			{
				_warnings.Add(string.Format("Skipped \"{0}\": {1}", tocPath, ex.Message));
				return null;
			}
		}

		/// <summary>
		///     Walks a mounted container's chunk table for Blam tags and folds them into the table,
		///     overriding any earlier tag that shares a package ID.
		/// </summary>
		private void ScanContainerForTags(FifthGenMountedContainer mounted)
		{
			IoStoreTableOfContents toc = mounted.Container.TableOfContents;

			// Collected before naming happens below, because the container-filename naming fallback
			// only makes sense when a container maps to exactly one tag - which is true of every
			// container sampled except the ones that deliberately bundle several (see
			// FifthGenNameOrigin.ContainerFilename).
			var found = new List<FoundTag>();
			for (int chunkIndex = 0; chunkIndex < toc.ChunkIds.Count; chunkIndex++)
			{
				if (toc.ChunkIds[chunkIndex].Type != IoChunkType.BulkData)
					continue;

				byte[] payload;
				try
				{
					payload = mounted.Container.ReadChunk(chunkIndex);
				}
				catch (IoStoreException ex)
				{
					_warnings.Add(string.Format("\"{0}\" chunk {1}: {2}", mounted.TocPath, chunkIndex, ex.Message));
					continue;
				}

				int groupMagic;
				if (!TryReadTagHeader(payload, out groupMagic))
					continue;

				found.Add(new FoundTag(chunkIndex, groupMagic, toc.ChunkIds[chunkIndex].PackageId, payload));
			}

			bool singleTagContainer = (found.Count == 1);
			foreach (FoundTag tag in found)
				AddOrOverrideTag(mounted, toc, tag, singleTagContainer);
		}

		private void AddOrOverrideTag(FifthGenMountedContainer mounted, IoStoreTableOfContents toc, FoundTag found,
			bool singleTagContainer)
		{
			var packageId = new FifthGenPackageId(found.PackageId);
			FifthGenGroupInfo group = GetOrAddGroup(found.GroupMagic);
			FifthGenNameOrigin origin;
			string name = DeriveName(mounted, toc, found.ChunkIndex, packageId, singleTagContainer, out origin);

			int existingIndex;
			if (_tagIndicesByPackageId.TryGetValue(found.PackageId, out existingIndex))
			{
				string previousContainer = _tags[existingIndex].Container.TocPath;
				_warnings.Add(string.Format("Package {0} from \"{1}\" overridden by \"{2}\".", packageId,
					Path.GetFileName(previousContainer), Path.GetFileName(mounted.TocPath)));

				var replacement = new FifthGenTag(_tags[existingIndex].Index, group, packageId, found.Payload, mounted);
				_tags[existingIndex] = replacement;

				// The overriding container always wins on data, but not necessarily on the name. A
				// container holding several tags cannot attribute its filename to any one of them and
				// so falls back to a package ID, and that must not discard a real name an earlier
				// single-tag container supplied for the same package. Keep whichever name came from
				// the better source.
				if (NameQuality(origin) >= NameQuality(_nameOrigins[existingIndex]))
				{
					_names[existingIndex] = name;
					_nameOrigins[existingIndex] = origin;
				}
			}
			else
			{
				int index = _tags.Count;
				var tag = new FifthGenTag(new DatumIndex((uint) index), group, packageId, found.Payload, mounted);
				_tags.Add(tag);
				_names.Add(name);
				_nameOrigins.Add(origin);
				_tagIndicesByPackageId.Add(found.PackageId, index);
			}
		}

		/// <summary>
		///     Ranks a name source so that overrides can keep the best name available for a package.
		/// </summary>
		/// <param name="origin">The source a name was derived from.</param>
		/// <returns>A rank where a higher value is a more trustworthy source.</returns>
		/// <remarks>
		///     This deliberately does not use the enum's own ordering, which reads best-to-worst and
		///     would invert the comparison, and which places <see cref="FifthGenNameOrigin.None" />
		///     first even though it is the least useful outcome of all.
		/// </remarks>
		private static int NameQuality(FifthGenNameOrigin origin)
		{
			switch (origin)
			{
				case FifthGenNameOrigin.DirectoryIndex:
					return 3;
				case FifthGenNameOrigin.ContainerHeader:
					return 2;
				case FifthGenNameOrigin.ContainerFilename:
					return 1;
				case FifthGenNameOrigin.PackageIdHex:
					return 0;
				default:
					return -1;
			}
		}

		private FifthGenGroupInfo GetOrAddGroup(int magic)
		{
			FifthGenGroupInfo group;
			if (_groupsByMagic.TryGetValue(magic, out group))
				return group;

			group = new FifthGenGroupInfo(magic);
			_groupsByMagic.Add(magic, group);
			_groups.Add(group);
			return group;
		}

		/// <summary>
		///     Reads the 64-byte header a Blam tag file begins with, just far enough to tell the tag
		///     apart from other <c>.ubulk</c> data and to recover its group.
		/// </summary>
		/// <param name="payload">The chunk's full decompressed bytes.</param>
		/// <param name="groupMagic">The tag's group four-CC, packed the way <see cref="CharConstant" /> expects.</param>
		/// <returns><c>true</c> if <paramref name="payload" /> begins with a Blam tag header.</returns>
		private static bool TryReadTagHeader(byte[] payload, out int groupMagic)
		{
			groupMagic = 0;
			if (payload == null || payload.Length < 64)
				return false;

			using (var reader = new EndianReader(new MemoryStream(payload), Endian.LittleEndian))
			{
				// +0x3C: 'BLAM' read in file order marks a big-endian tag file; 'MALB' - literally
				// 'BLAM' byte-reversed - marks a little-endian one (every real tag sampled is
				// little-endian: 'MALB'). This is the one field in the header whose meaning does not
				// depend on already knowing the file's endianness; it is what tells a reader the
				// endianness in the first place. See ce-format-spec.md section 3.1.
				reader.SeekTo(0x3C);
				string signature = reader.ReadAscii(4);

				Endian tagEndian;
				if (signature == "BLAM")
					tagEndian = Endian.BigEndian;
				else if (signature == "MALB")
					tagEndian = Endian.LittleEndian;
				else
					return false;

				reader.Endianness = tagEndian;
				reader.SeekTo(0x30);
				groupMagic = reader.ReadInt32();
			}

			return true;
		}

		/// <summary>
		///     Works out a display name for a tag, trying each source in
		///     <see cref="FifthGenNameOrigin" /> order and recording which one succeeded.
		/// </summary>
		private static string DeriveName(FifthGenMountedContainer mounted, IoStoreTableOfContents toc, int chunkIndex,
			FifthGenPackageId packageId, bool singleTagContainer, out FifthGenNameOrigin origin)
		{
			string name;

			if (TryGetDirectoryIndexName(toc, chunkIndex, out name))
			{
				origin = FifthGenNameOrigin.DirectoryIndex;
				return name;
			}

			if (TryGetContainerHeaderName(mounted, out name))
			{
				origin = FifthGenNameOrigin.ContainerHeader;
				return name;
			}

			if (singleTagContainer && TryGetContainerFilenameName(mounted, out name))
			{
				origin = FifthGenNameOrigin.ContainerFilename;
				return name;
			}

			origin = FifthGenNameOrigin.PackageIdHex;
			return packageId.ToString();
		}

		private static bool TryGetDirectoryIndexName(IoStoreTableOfContents toc, int chunkIndex, out string name)
		{
			name = null;

			IoDirectoryIndex index = toc.DirectoryIndex;
			if (index == null)
				return false;

			// IoDirectoryIndex only exposes a forward (path -> chunk index) lookup, since that is all
			// a reader normally needs. Naming wants the reverse, and every directory index sampled is
			// empty anyway, so a linear scan over its (possibly zero) paths costs nothing worth
			// avoiding rather than adding a reverse-lookup table to a type this engine does not own.
			foreach (string path in index.Paths)
			{
				int resolvedIndex;
				if (index.TryGetChunkIndex(path, out resolvedIndex) && resolvedIndex == chunkIndex)
				{
					name = NormalizeDirectoryIndexPath(path);
					return true;
				}
			}
			return false;
		}

		private static string NormalizeDirectoryIndexPath(string path)
		{
			string normalized = path.Replace('\\', '/');

			string extension = Path.GetExtension(normalized);
			if (string.Equals(extension, ".ubulk", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(extension, ".uasset", StringComparison.OrdinalIgnoreCase))
				normalized = normalized.Substring(0, normalized.Length - extension.Length);

			const string tagsPrefix = "Game/Tags/";
			int prefixIndex = normalized.IndexOf(tagsPrefix, StringComparison.OrdinalIgnoreCase);
			if (prefixIndex >= 0)
				normalized = normalized.Substring(prefixIndex + tagsPrefix.Length);

			return normalized;
		}

		/// <summary>
		///     See <see cref="FifthGenNameOrigin.ContainerHeader" />: investigated against real
		///     containers, and confirmed to carry no string data at all, so this always returns
		///     <c>false</c>. Kept as a documented, checked dead end rather than removed outright.
		/// </summary>
		private static bool TryGetContainerHeaderName(FifthGenMountedContainer mounted, out string name)
		{
			name = null;
			return false;
		}

		private static bool TryGetContainerFilenameName(FifthGenMountedContainer mounted, out string name)
		{
			string stem = Path.GetFileNameWithoutExtension(mounted.TocPath);
			if (stem.EndsWith("_P", StringComparison.OrdinalIgnoreCase))
				stem = stem.Substring(0, stem.Length - 2);

			if (string.IsNullOrEmpty(stem))
			{
				name = null;
				return false;
			}

			name = stem;
			return true;
		}

		/// <summary>
		///     A confirmed Blam tag found while scanning one container, before it is folded into the
		///     table's package-id-keyed override bookkeeping.
		/// </summary>
		private struct FoundTag
		{
			public FoundTag(int chunkIndex, int groupMagic, ulong packageId, byte[] payload)
			{
				ChunkIndex = chunkIndex;
				GroupMagic = groupMagic;
				PackageId = packageId;
				Payload = payload;
			}

			public readonly int ChunkIndex;
			public readonly int GroupMagic;
			public readonly ulong PackageId;
			public readonly byte[] Payload;
		}
	}
}
