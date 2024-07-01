using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.Eldorado.Structures
{
	/// <summary>
	///     A cache file header whose layout can be changed.
	/// </summary>
	public class EldoradoCacheHeader
	{
		private FileSegment _eofSegment;

		public EldoradoCacheHeader(StructureValueCollection values, EngineDescription info, FileSegmenter segmenter)
		{
			BuildString = info.BuildVersion;
			HeaderSize = info.HeaderSize;
			Load(values, segmenter);
		}

		public int HeaderSize { get; private set; }

		public uint FileSize
		{
			get { return _eofSegment.Offset; }
		}

		public CacheFileType Type { get; private set; }
		public string InternalName { get; private set; }
		public string ScenarioName { get; private set; }

		public string BuildString { get; private set; }

		public FileSegmentGroup MetaArea { get; private set; }
		public SegmentPointer IndexHeaderLocation { get; set; }

		public FileSegmentGroup StringArea { get; private set; }

		public int StringIDCount { get; set; }
		public FileSegment StringIDIndexTable { get; private set; }
		public SegmentPointer StringIDIndexTableLocation { get; set; }
		public FileSegment StringIDData { get; private set; }
		public SegmentPointer StringIDDataLocation { get; set; }

		public FileSegment StringBlock { get; private set; }
		public SegmentPointer StringBlockLocation { get; set; }

		/// <summary>
		///     Serializes the header's values, storing them into a StructureValueCollection.
		/// </summary>
		/// <param name="localeArea">The locale area of the cache file. Can be null.</param>
		/// <param name="localePointerMask">The value to add to locale pointers to translate them to file offsets.</param>
		/// <returns>The resulting StructureValueCollection.</returns>
		public StructureValueCollection Serialize(FileSegmentGroup localeArea)
		{
			var values = new StructureValueCollection();

			values.SetInteger("file size", FileSize);
			values.SetInteger("type", (uint)Type);
			values.SetString("internal name", InternalName);
			values.SetString("scenario name", ScenarioName);

			return values;
		}

		private void Load(StructureValueCollection values, FileSegmenter segmenter)
		{
			segmenter.DefineSegment(0, (uint)HeaderSize, 1, SegmentResizeOrigin.Beginning); // Define a segment for the header
			_eofSegment = segmenter.WrapEOF((uint)values.GetInteger("file size"));

			Type = (CacheFileType)values.GetInteger("type");
			InternalName = values.GetString("internal name");
			ScenarioName = values.GetString("scenario name");
		}

	}
}