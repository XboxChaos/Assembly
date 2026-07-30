using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blamite.Blam.FifthGen.Structures;
using Blamite.Blam.Localization;
using Blamite.Blam.Resources;
using Blamite.Blam.Resources.Sounds;
using Blamite.Blam.Scripting;
using Blamite.Blam.Shaders;
using Blamite.Blam.Util;
using Blamite.IO;
using Blamite.IO.IoStore;
using Blamite.Serialization;

namespace Blamite.Blam.FifthGen
{
	/// <summary>
	///     A Halo: Campaign Evolved tag namespace: every Blam tag mounted from a folder of Unreal
	///     Engine IoStore containers.
	/// </summary>
	/// <remarks>
	///     <para>
	///         Modeled on <see cref="Eldorado.EldoradoCacheFile" />, which is the closest existing
	///         precedent for a "cache file" that is really a set of sidecar files rather than one
	///         monolithic file: subsystems this engine genuinely does not have return <c>null</c>
	///         (or, where the interface expects a collection, an empty one) instead of throwing, and
	///         the dummy no-op implementations already in the codebase
	///         (<see cref="DummyPointerExpander" />, <see cref="DummyResourceMetaLoader" />,
	///         <see cref="DummyLanguagePackLoader" />) stand in for the rest.
	///     </para>
	///     <para>
	///         Where Eldorado's sidecar files sit next to a single "map" file this class is
	///         constructed with a path to, Campaign Evolved's containers <em>are</em> the whole cache
	///         file - there is no separate header to read first. <see cref="FilePath" /> is treated
	///         as either an IoStore container inside the namespace's folder, or the folder itself;
	///         either way, every <c>.utoc</c> directly inside that folder gets mounted (see
	///         <see cref="FifthGenTagTable" />).
	///     </para>
	/// </remarks>
	public class FifthGenCacheFile : ICacheFile
	{
		private readonly EngineDescription _buildInfo;
		private readonly DummyPointerExpander _expander = new DummyPointerExpander();
		private readonly DummyResourceMetaLoader _resourceMetaLoader = new DummyResourceMetaLoader();
		private readonly DummyLanguagePackLoader _languages = new DummyLanguagePackLoader();
		private FifthGenTagTable _tags;
		private SegmentPointer _indexHeaderLocation;

		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenCacheFile" /> class, mounting the
		///     IoStore containers found alongside <paramref name="filePath" />.
		/// </summary>
		/// <param name="reader">
		///     Unused - Campaign Evolved has no single header to read up front, and every container
		///     this mounts is opened fresh by path. Accepted only so this constructor matches the
		///     signature <see cref="CacheFileLoader" /> dispatches to for every other engine.
		/// </param>
		/// <param name="buildInfo">The engine description this build matched during detection.</param>
		/// <param name="filePath">
		///     Either the path to one of the namespace's <c>.utoc</c> files, or the path to the
		///     folder holding them.
		/// </param>
		public FifthGenCacheFile(IReader reader, EngineDescription buildInfo, string filePath)
		{
			FilePath = filePath;
			_buildInfo = buildInfo;
			Allocator = new MetaAllocator(this, 4);
			Load();
		}

		/// <summary>
		///     Gets the folder every <c>.utoc</c> in this tag namespace was mounted from.
		/// </summary>
		public string MountDirectory { get; private set; }

		public string FilePath { get; private set; }

		/// <summary>
		///     Campaign Evolved has no monolithic header, so this is always 0.
		/// </summary>
		public int HeaderSize
		{
			get { return 0; }
		}

		/// <summary>
		///     The combined size in bytes of every mounted container's <c>.utoc</c> and <c>.ucas</c>
		///     files - informational only; there is no single "file" this namespace corresponds to.
		/// </summary>
		public long FileSize { get; private set; }

		/// <summary>
		///     Campaign Evolved has no concept distinguishing a namespace's purpose the way
		///     <see cref="CacheFileType" /> does for a single map's cache file, so this is always
		///     <see cref="CacheFileType.Shared" /> - the closest existing value to "not one specific
		///     map or mode".
		/// </summary>
		public CacheFileType Type
		{
			get { return CacheFileType.Shared; }
		}

		public EngineType Engine
		{
			get { return EngineType.FifthGeneration; }
		}

		public string BuildString
		{
			get { return _buildInfo.BuildVersion; }
		}

		/// <summary>
		///     The name of the folder every container in this namespace was mounted from - the
		///     closest available stand-in for a per-namespace display name.
		/// </summary>
		public string InternalName { get; private set; }

		/// <summary>
		///     A mounted namespace can hold any number of <c>scnr</c> tags (or none); there is no
		///     single canonical scenario the way one cache file has one. Always <c>null</c> - use
		///     <c>Tags.FindTagsByGroup("scnr")</c> to enumerate them.
		/// </summary>
		public string ScenarioName
		{
			get { return null; }
		}

		public int XDKVersion
		{
			get { return 0; }
		}

		/// <summary>
		///     Unknown for any mounted namespace - nothing in an IoStore container records when it
		///     was built.
		/// </summary>
		public DateTime? BuildDate
		{
			get { return null; }
		}

		public bool ZoneOnly
		{
			get { return false; }
		}

		/// <summary>
		///     There is no shared address space across a set of mounted containers for a meta area to
		///     describe. Always <c>null</c>.
		/// </summary>
		public FileSegmentGroup MetaArea
		{
			get { return null; }
		}

		public SegmentPointer IndexHeaderLocation
		{
			get { return _indexHeaderLocation; }
			set { _indexHeaderLocation = value; }
		}

		public Partition[] Partitions
		{
			get { return null; }
		}

		public FileSegment RawTable
		{
			get { return null; }
		}

		public FileSegmentGroup LocaleArea
		{
			get { return null; }
		}

		public FileSegmentGroup StringArea
		{
			get { return null; }
		}

		public FileSegment StringIDIndexTable
		{
			get { return null; }
		}

		public FileSegment StringIDDataTable
		{
			get { return null; }
		}

		public FileSegment FileNameIndexTable
		{
			get { return null; }
		}

		public FileSegment FileNameDataTable
		{
			get { return null; }
		}

		public FileNameSource FileNames
		{
			get { return _tags.FileNames; }
		}

		/// <summary>
		///     A CE tag's <c>string id</c> fields carry their literal string alongside them (a
		///     <c>tgsi</c> section per field) rather than indexing into a shared table - there is no
		///     global StringID table for Campaign Evolved at all (<c>ce-format-spec.md</c> section
		///     3.9). Always <c>null</c>.
		/// </summary>
		public StringIDSource StringIDs
		{
			get { return null; }
		}

		public IList<ITagGroup> TagGroups
		{
			get { return _tags.Groups; }
		}

		public IResourceManager Resources
		{
			get { return null; }
		}

		public TagTable Tags
		{
			get { return _tags; }
		}

		public FileSegmentGroup[] BSPAreas
		{
			get { return null; }
		}

		public ILanguagePackLoader Languages
		{
			get { return _languages; }
		}

		public IResourceMetaLoader ResourceMetaLoader
		{
			get { return _resourceMetaLoader; }
		}

		/// <summary>
		///     There is no single file for a namespace's contents to be divided into segments of.
		///     Always empty.
		/// </summary>
		public IEnumerable<FileSegment> Segments
		{
			get { return Enumerable.Empty<FileSegment>(); }
		}

		public MetaAllocator Allocator { get; private set; }

		public IScriptFile[] ScriptFiles
		{
			get { return new IScriptFile[0]; }
		}

		public IShaderStreamer ShaderStreamer
		{
			get { return null; }
		}

		public ISimulationDefinitionTable SimulationDefinitions
		{
			get { return null; }
		}

		public IList<ITagInterop> TagInteropTable
		{
			get { return _tags.Interops; }
		}

		public SoundResourceManager SoundGestalt
		{
			get { return null; }
		}

		public IPointerExpander PointerExpander
		{
			get { return _expander; }
		}

		/// <summary>
		///     Every Campaign Evolved container observed is little-endian; a tag's own header
		///     additionally self-declares its endianness (BLAM/MALB), which is what
		///     <see cref="FifthGenTagTable" /> actually reads by.
		/// </summary>
		public Endian Endianness
		{
			get { return Endian.LittleEndian; }
		}

		public EffectInterop EffectInterops
		{
			get { return null; }
		}

		/// <summary>
		///     Writes every tag carrying a pending edit back into its owning container.
		/// </summary>
		/// <param name="stream">
		///     Unused. Unlike a classic engine's single cache file, a Campaign Evolved namespace has no one stream
		///     changes could be written to in the first place (see <see cref="FifthGenCacheFile" />'s own remarks); each
		///     edited tag instead carries the change itself, in <see cref="FifthGenTag.PendingEdit" />, and this writes
		///     it into that tag's own <c>.utoc</c>/<c>.ucas</c> pair. Accepted only so this matches
		///     <see cref="ICacheFile.SaveChanges" />'s signature.
		/// </param>
		/// <remarks>
		///     A caller that has parsed a tag's <see cref="FifthGenTag.RawPayload" /> into a <see cref="FifthGenTagFile" />
		///     and edited it through the mutation methods on <see cref="FifthGenTagValue" />, <see cref="FifthGenTagStruct" />
		///     and <see cref="FifthGenTagBlock" /> assigns that instance to the tag's <see cref="FifthGenTag.PendingEdit" />
		///     and then calls this. Each such tag is: serialised with <see cref="FifthGenTagWriter" />; written into its
		///     owning container with <see cref="IoStoreContainerWriter" />, which rewrites the whole
		///     <c>.utoc</c>/<c>.ucas</c> pair rather than patching either in place (see that class's remarks for why); and
		///     has its <see cref="FifthGenTag.RawPayload" /> updated to match, so reading it straight back afterward -
		///     without re-mounting anything - already sees the saved bytes.
		/// </remarks>
		public void SaveChanges(IStream stream)
		{
			foreach (ITag genericTag in _tags)
			{
				var tag = genericTag as FifthGenTag;
				if (tag?.PendingEdit != null)
					SaveTag(tag);
			}
		}

		public void SaveTagNames(IStream stream)
		{
			throw new NotSupportedException("Saving Campaign Evolved tag names is not supported yet.");
		}

		/// <summary>
		///     Serialises one tag's pending edit and writes it into the container that currently owns its data.
		/// </summary>
		private void SaveTag(FifthGenTag tag)
		{
			byte[] newPayload = FifthGenTagWriter.Write(tag.PendingEdit);

			FifthGenMountedContainer mounted = tag.Container;
			IoChunkId chunkId = FindBulkDataChunkId(mounted.Container.TableOfContents, tag.PackageId.Value);

			// The container's own open streams have to be closed before its files are rewritten on disk - see
			// FifthGenMountedContainer.SetContainer - and reopened once the rewrite has finished, or failed, either
			// way: a namespace left with a permanently-closed container after one tag's write throws would take every
			// other tag sharing it down too.
			mounted.SetContainer(null);
			try
			{
				IoStoreContainerWriter.ReplaceChunk(mounted.TocPath, _buildInfo.Layouts, chunkId, newPayload);
			}
			finally
			{
				mounted.SetContainer(IoStoreContainer.Open(mounted.TocPath, _buildInfo.Layouts, null));
			}

			tag.RawPayload = newPayload;
			tag.PendingEdit = null;
		}

		/// <summary>
		///     Finds the <c>BulkData</c> chunk sibling to a package's tag data - the one <see cref="FifthGenTagTable" />
		///     originally read <see cref="FifthGenTag.RawPayload" /> from - by package ID, the way
		///     <see cref="FifthGenTagTable" /> itself does while mounting.
		/// </summary>
		private static IoChunkId FindBulkDataChunkId(IoStoreTableOfContents toc, ulong packageId)
		{
			foreach (IoChunkId id in toc.ChunkIds)
			{
				if (id.PackageId == packageId && id.Type == IoChunkType.BulkData)
					return id;
			}
			throw new IoStoreException(string.Format(
				"No BulkData chunk for package 0x{0:X16} was found in the container this tag was read from.", packageId));
		}

		private void Load()
		{
			MountDirectory = ResolveMountDirectory(FilePath);
			InternalName = Path.GetFileName(MountDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

			_tags = new FifthGenTagTable(MountDirectory, _buildInfo);
			FileSize = ComputeMountedSize(_tags.MountedContainers);
		}

		/// <summary>
		///     Works out which folder to mount every <c>.utoc</c> from: <paramref name="path" />'s
		///     own folder if it names a file, or <paramref name="path" /> itself if it already names
		///     a folder.
		/// </summary>
		private static string ResolveMountDirectory(string path)
		{
			if (Directory.Exists(path))
				return path;

			string directory = Path.GetDirectoryName(Path.GetFullPath(path));
			if (string.IsNullOrEmpty(directory))
			{
				throw new IoStoreException(string.Format(
					"\"{0}\" has no containing folder to mount Campaign Evolved containers from.", path));
			}
			return directory;
		}

		private static long ComputeMountedSize(IEnumerable<FifthGenMountedContainer> containers)
		{
			long total = 0;
			foreach (FifthGenMountedContainer mounted in containers)
			{
				total += SafeFileLength(mounted.TocPath);
				total += SafeFileLength(Path.ChangeExtension(mounted.TocPath, ".ucas"));
			}
			return total;
		}

		private static long SafeFileLength(string path)
		{
			try
			{
				return new FileInfo(path).Length;
			}
			catch (IOException)
			{
				return 0;
			}
		}
	}
}
