using System.Collections.Generic;
using System.IO;
using Blamite.Blam.LanguagePack;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.ThirdGen.LanguagePack
{
	public class ThirdGenLanguage
	{
		private readonly AESKey _encryptionKey;
		private readonly StructureLayout _pointerLayout;
		private readonly int _sizeAlign;

		public ThirdGenLanguage(GameLanguage language, StructureValueCollection values, FileSegmenter segmenter,
			FileSegmentGroup localeArea, EngineDescription buildInfo)
		{
			Language = language;
			_pointerLayout = buildInfo.Layouts.GetLayout("locale index table entry");
			_encryptionKey = buildInfo.LocaleKey;
			_sizeAlign = (_encryptionKey != null) ? AES.BlockSize : 1;
			Load(values, segmenter, localeArea);
		}

		public GameLanguage Language { get; private set; }
		public int StringCount { get; private set; }
		public FileSegment LocaleIndexTable { get; private set; }
		public FileSegment LocaleData { get; private set; }
		public SegmentPointer LocaleIndexTableLocation { get; set; }
		public SegmentPointer LocaleDataLocation { get; set; }
		public byte[] IndexTableHash { get; private set; }
		public byte[] StringDataHash { get; private set; }

		public StructureValueCollection Serialize()
		{
			var result = new StructureValueCollection();
			result.SetInteger("string count", (uint) StringCount);
			result.SetInteger("locale table size", LocaleData != null ? (uint) LocaleData.Size : 0);

			if (LocaleIndexTableLocation != null)
				result.SetInteger("locale index table offset", LocaleIndexTableLocation.AsPointer());
			if (LocaleDataLocation != null)
				result.SetInteger("locale data index offset", LocaleDataLocation.AsPointer());

			if (IndexTableHash != null)
				result.SetRaw("index table hash", IndexTableHash);
			if (StringDataHash != null)
				result.SetRaw("string data hash", StringDataHash);

			return result;
		}

		private void Load(StructureValueCollection values, FileSegmenter segmenter, FileSegmentGroup localeArea)
		{
			StringCount = (int) values.GetInteger("string count");
			if (StringCount > 0)
			{
				// Index table offset, segment, and pointer
				int localeIndexTableOffset = localeArea.PointerToOffset(values.GetInteger("locale index table offset"));
				LocaleIndexTable = segmenter.WrapSegment(localeIndexTableOffset, StringCount*8, 8, SegmentResizeOrigin.End);
				LocaleIndexTableLocation = localeArea.AddSegment(LocaleIndexTable);

				// Data offset, segment, and pointer
				int localeDataOffset = localeArea.PointerToOffset(values.GetInteger("locale data index offset"));
				var localeDataSize = (int) values.GetInteger("locale table size");
				LocaleData = segmenter.WrapSegment(localeDataOffset, localeDataSize, _sizeAlign, SegmentResizeOrigin.End);
				LocaleDataLocation = localeArea.AddSegment(LocaleData);

				// Load hashes if they exist
				if (values.HasRaw("index table hash"))
					IndexTableHash = values.GetRaw("index table hash");
				if (values.HasRaw("string data hash"))
					StringDataHash = values.GetRaw("string data hash");
			}
		}

		public List<LocalizedString> LoadStrings(IReader reader)
		{
			var result = new List<LocalizedString>();
			if (StringCount == 0)
				return result;

			byte[] stringData = ReadLocaleData(reader);
			using (var stringReader = new EndianReader(new MemoryStream(stringData), Endian.BigEndian))
			{
				reader.SeekTo(LocaleIndexTableLocation.AsOffset());

				// Read each locale
				for (int i = 0; i < StringCount; i++)
				{
					// Read the offset and stringID
					StringID id;
					int offset;
					ReadLocalePointer(reader, out id, out offset);

					if (offset >= stringReader.Length)
						break; // Bad table - bail out so we don't end up in a huge memory-hogging loop

					stringReader.SeekTo(offset);
					string locale = stringReader.ReadUTF8();
					result.Add(new LocalizedString(id, locale));
				}
			}
			return result;
		}

		public void SaveStrings(IStream stream, List<LocalizedString> locales)
		{
			var offsetData = new MemoryStream();
			var stringData = new MemoryStream();
			var offsetWriter = new EndianWriter(offsetData, Endian.BigEndian);
			var stringWriter = new EndianWriter(stringData, Endian.BigEndian);

			try
			{
				// Write the string and offset data to buffers
				foreach (LocalizedString locale in locales)
				{
					WriteLocalePointer(offsetWriter, locale.Key, (int) stringWriter.Position);
					stringWriter.WriteUTF8(locale.Value);
				}

				// Round the size of the string data up
				var dataSize = (int) ((stringData.Position + _sizeAlign - 1) & ~(_sizeAlign - 1));
				stringData.SetLength(dataSize);

				// Update the two locale data hashes if we need to
				// (the hash arrays are set to null if the build doesn't need them)
				if (IndexTableHash != null)
					IndexTableHash = SHA1.Transform(offsetData.GetBuffer(), 0, (int) offsetData.Length);
				if (StringDataHash != null)
					StringDataHash = SHA1.Transform(stringData.GetBuffer(), 0, dataSize);

				// Make sure there's free space for the offset table and then write it to the file
				LocaleIndexTable.Resize((int) offsetData.Length, stream);
				stream.SeekTo(LocaleIndexTableLocation.AsOffset());
				stream.WriteBlock(offsetData.GetBuffer(), 0, (int) offsetData.Length);

				// Encrypt the string data if necessary
				byte[] strings = stringData.GetBuffer();
				if (_encryptionKey != null)
					strings = AES.Encrypt(strings, 0, dataSize, _encryptionKey.Key, _encryptionKey.IV);

				// Make sure there's free space for the string data and then write it to the file
				LocaleData.Resize(dataSize, stream);
				stream.SeekTo(LocaleDataLocation.AsOffset());
				stream.WriteBlock(strings, 0, dataSize);

				// Update the string count and recalculate the language table offsets
				StringCount = locales.Count;
			}
			finally
			{
				offsetWriter.Close();
				stringWriter.Close();
			}
		}

		private void ReadLocalePointer(IReader reader, out StringID id, out int offset)
		{
			StructureValueCollection values = StructureReader.ReadStructure(reader, _pointerLayout);
			id = new StringID(values.GetInteger("stringid"));
			offset = (int) values.GetInteger("offset");
		}

		private void WriteLocalePointer(IWriter writer, StringID id, int offset)
		{
			var values = new StructureValueCollection();
			values.SetInteger("stringid", id.Value);
			values.SetInteger("offset", (uint) offset);
			StructureWriter.WriteStructure(values, _pointerLayout, writer);
		}

		private byte[] ReadLocaleData(IReader reader)
		{
			// Read the string data
			reader.SeekTo(LocaleDataLocation.AsOffset());
			byte[] stringData = reader.ReadBlock(LocaleData.Size);

			// Decrypt it if necessary
			if (_encryptionKey != null)
				stringData = AES.Decrypt(stringData, _encryptionKey.Key, _encryptionKey.IV);

			return stringData;
		}
	}
}