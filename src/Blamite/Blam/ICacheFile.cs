using System.Collections.Generic;
using Blamite.Blam.LanguagePack;
using Blamite.Blam.Resources;
using Blamite.Blam.Resources.Sounds;
using Blamite.Blam.Scripting;
using Blamite.Blam.Shaders;
using Blamite.Blam.Util;
using Blamite.IO;

namespace Blamite.Blam
{
	/// <summary>
	///     A .map file containing cached information about a map in the game.
	/// </summary>
	public interface ICacheFile
	{
		/// <summary>
		///     The size of the file header.
		/// </summary>
		int HeaderSize { get; }

		/// <summary>
		///     The size of the cache file.
		/// </summary>
		uint FileSize { get; }

		/// <summary>
		///     The purpose of the cache file.
		/// </summary>
		CacheFileType Type { get; }

		/// <summary>
		///     The cache file's target engine.
		/// </summary>
		EngineType Engine { get; }

		/// <summary>
		///     The engine version that the cache file is intended for.
		/// </summary>
		string BuildString { get; }

		/// <summary>
		///     The cache file's internal name.
		/// </summary>
		string InternalName { get; }

		/// <summary>
		///     The name of the cache file's scenario tag (can be null if none).
		/// </summary>
		string ScenarioName { get; }

		/// <summary>
		///     The XDK version that the cache file was developed with, or 0 if unknown.
		/// </summary>
		int XDKVersion { get; set; }

		/// <summary>
		///     The meta area of the cache file.
		///     Can be null.
		/// </summary>
		FileSegmentGroup MetaArea { get; }

		/// <summary>
		///     The location of the tag table header in the file.
		///     Can be null.
		/// </summary>
		SegmentPointer IndexHeaderLocation { get; set; }

		/// <summary>
		///     The meta partitions in the cache file.
		/// </summary>
		Partition[] Partitions { get; }

		/// <summary>
		///     The raw table in the cache file.
		///     Can be null.
		/// </summary>
		FileSegment RawTable { get; }

		/// <summary>
		///     The locale area of the cache file.
		///     Can be null.
		/// </summary>
		FileSegmentGroup LocaleArea { get; }

		/// <summary>
		///     The debug string area of the cache file.
		///     Can be null.
		/// </summary>
		FileSegmentGroup StringArea { get; }

		/// <summary>
		///     The string ID index area of the cache file.
		///     Can be null.
		/// </summary>
		FileSegment StringIDIndexTable { get; }

		/// <summary>
		///     The string ID data area of the cache file.
		///     Can be null.
		/// </summary>
		FileSegment StringIDDataTable { get; }

		/// <summary>
		///     The tag name index area of the cache file.
		///     Can be null.
		/// </summary>
		FileSegment FileNameIndexTable { get; }

		/// <summary>
		///     The tag name data area of the cache file.
		///     Can be null.
		/// </summary>
		FileSegment FileNameDataTable { get; }

		/// <summary>
		///     The tag names in the file.
		///     Can be null.
		/// </summary>
		FileNameSource FileNames { get; }

		/// <summary>
		///     The stringIDs in the file.
		///     Can be null.
		/// </summary>
		StringIDSource StringIDs { get; }

		/// <summary>
		///     The cache file's language pack loader.
		/// </summary>
		ILanguagePackLoader Languages { get; }

		/// <summary>
		///     The tag classes stored in the file.
		/// </summary>
		IList<ITagClass> TagClasses { get; }

		/// <summary>
		///     The tags stored in the file.
		/// </summary>
		TagTable Tags { get; }

		/// <summary>
		///     The cache file's resource manager.
		/// </summary>
		IResourceManager Resources { get; }
		
		/// <summary>
		///     The IResourceMetaLoader which can be used to load metadata for resources.
		/// </summary>
		IResourceMetaLoader ResourceMetaLoader { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		ISoundResourceGestalt LoadSoundResourceGestaltData(IReader reader);

		/// <summary>
		///     A collection of segments that the file has been arbitrarily divided into.
		///     For a given cache file, these segments will always be in the same order.
		///     This could be used to compare two cache files and determine sizing differences between them.
		/// </summary>
		IEnumerable<FileSegment> Segments { get; }

		/// <summary>
		///     The cache file's MetaAllocator, which can be used to allocate free meta in the cache file.
		///     Note that this object calls SaveChanges() automatically and changes do not need to be manually saved.
		/// </summary>
		MetaAllocator Allocator { get; }

		/// <summary>
		///     The script files stored in the file.
		/// </summary>
		IScriptFile[] ScriptFiles { get; }

		/// <summary>
		/// The shader streamer for the cache file.
		/// </summary>
		IShaderStreamer ShaderStreamer { get; }

		/// <summary>
		///     Saves any changes that were made to the file.
		/// </summary>
		/// <param name="stream">The stream to write changes to.</param>
		void SaveChanges(IStream stream);
	}
}