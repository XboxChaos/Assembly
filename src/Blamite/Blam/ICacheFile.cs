using System;
using System.Collections.Generic;
using Blamite.Blam.Localization;
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
		///		The full file path of the cache file.
		/// </summary>
		string FilePath { get; }

		/// <summary>
		///     The size of the file header.
		/// </summary>
		int HeaderSize { get; }

		/// <summary>
		///     The size of the cache file.
		/// </summary>
		long FileSize { get; }

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
		///     True if the cache file's resource page information is located in the zone tag, rather than play.
		/// </summary>
		bool ZoneOnly { get; }

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
		///     The tag groups stored in the file.
		/// </summary>
		IList<ITagGroup> TagGroups { get; }

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
		/// The simulation definition table for the cache file. Can be <c>null</c> if none.
		/// </summary>
		ISimulationDefinitionTable SimulationDefinitions { get; }

		IList<ITagInterop> TagInteropTable { get; }

		SoundResourceManager SoundGestalt { get; }

		/// <summary>
		///     Saves any changes that were made to the file.
		/// </summary>
		/// <param name="stream">The stream to write changes to.</param>
		void SaveChanges(IStream stream);

		IPointerExpander PointerExpander { get; }

		Endian Endianness { get; }

		EffectInterop EffectInterops { get; }

		/// <summary>
		///     The bsp areas of the cache file.
		///     Can be null.
		/// </summary>
		FileSegmentGroup[] BSPAreas { get; }
	}

	public static class ICacheFileExtensions
	{
		public static uint GenerateChecksum(this ICacheFile cacheFile, IReader reader, bool reverse = false)
		{
			// XOR all of the uint32s in the file after the header
			// based on http://codeescape.com/2009/05/optimized-c-halo-2-map-signing-algorithm/
			uint checksum = 0;
			uint blockSize = 0x10000;
			reader.SeekTo(cacheFile.HeaderSize);

			while (reader.Position < reader.Length)
			{
				int actualSize = (int)Math.Min(blockSize, (uint)(reader.Length - reader.Position));
				int adjustedSize = (actualSize + 3) & ~0x3;
				byte[] block = new byte[adjustedSize];
				reader.ReadBlock(block, 0, actualSize);
				for (int i = 0; i < block.Length; i += 4)
					checksum ^= BitConverter.ToUInt32(block, i);
			}

			// I guess while getting the engine moved over to 360, the checksum stayed little endian for a chunk of dev.
			if (reverse)
			{
				byte[] checksumBytes = BitConverter.GetBytes(checksum);
				Array.Reverse(checksumBytes);
				checksum = BitConverter.ToUInt32(checksumBytes, 0);
			}

			return checksum;
		}
	}
}