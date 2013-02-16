using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.IO;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Util;
using System.IO;

namespace ExtryzeDLL.Blam.ThirdGen.Structures
{
    public class ThirdGenLanguage : ILanguage
    {
        private AESKey _encryptionKey;
        private StructureLayout _pointerLayout;
        private FileSegmentGroup _localeArea;
        private LocaleSymbolCollection _symbols;
        private int _sizeAlign;

        public ThirdGenLanguage(StructureValueCollection values, FileSegmenter segmenter, FileSegmentGroup localeArea, BuildInformation buildInfo)
        {
            _pointerLayout = buildInfo.GetLayout("locale index table entry");
            _encryptionKey = buildInfo.LocaleKey;
            _symbols = buildInfo.LocaleSymbols;
            _localeArea = localeArea;
            _sizeAlign = (_encryptionKey != null) ? AES.BlockSize : 1;
            Load(values, segmenter, localeArea);
        }

        public int StringCount { get; private set; }
        public FileSegment LocaleIndexTable { get; private set; }
        public FileSegment LocaleData { get; private set; }
        public SegmentPointer LocaleIndexTableLocation { get; set; }
        public SegmentPointer LocaleDataLocation { get; set; }
        public byte[] IndexTableHash { get; private set; }
        public byte[] StringDataHash { get; private set; }

        public StructureValueCollection Serialize()
        {
            StructureValueCollection result = new StructureValueCollection();
            result.SetNumber("string count", (uint)StringCount);
            result.SetNumber("locale table size", LocaleData != null ? (uint)LocaleData.Size : 0);

            if (LocaleIndexTableLocation != null)
                result.SetNumber("locale index table offset", LocaleIndexTableLocation.AsPointer());
            if (LocaleDataLocation != null)
                result.SetNumber("locale data index offset", LocaleDataLocation.AsPointer());

            if (IndexTableHash != null)
                result.SetRaw("index table hash", IndexTableHash);
            if (StringDataHash != null)
                result.SetRaw("string data hash", StringDataHash);
            return result;
        }

        private void Load(StructureValueCollection values, FileSegmenter segmenter, FileSegmentGroup localeArea)
        {
            StringCount = (int)values.GetNumber("string count");
            if (StringCount > 0)
            {
                // Index table offset, segment, and pointer
                int localeIndexTableOffset = localeArea.PointerToOffset(values.GetNumber("locale index table offset"));
                LocaleIndexTable = segmenter.WrapSegment(localeIndexTableOffset, StringCount * 8, 8, SegmentResizeOrigin.End);
                LocaleIndexTableLocation = localeArea.AddSegment(LocaleIndexTable);

                // Data offset, segment, and pointer
                int localeDataOffset = localeArea.PointerToOffset(values.GetNumber("locale data index offset"));
                int localeDataSize = (int)values.GetNumber("locale table size");
                LocaleData = segmenter.WrapSegment(localeDataOffset, localeDataSize, _sizeAlign, SegmentResizeOrigin.End);
                LocaleDataLocation = localeArea.AddSegment(LocaleData);

                // Load hashes if they exist
                if (values.HasRaw("index table hash"))
                    IndexTableHash = values.GetRaw("index table hash");
                if (values.HasRaw("string data hash"))
                    StringDataHash = values.GetRaw("string data hash");
            }
        }

        public LocaleTable LoadStrings(IReader reader)
        {
            LocaleTable result = new LocaleTable(this);
            if (StringCount == 0)
                return result;

            byte[] stringData = ReadLocaleData(reader);
            using (EndianReader stringReader = new EndianReader(new MemoryStream(stringData), Endian.BigEndian))
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
                    result.Strings.Add(new Locale(id, locale));
                    //result.Add(ReplaceSymbols(locale));
                }
            }
            return result;
        }

        public void SaveStrings(IStream stream, LocaleTable locales)
        {
            MemoryStream offsetData = new MemoryStream();
            MemoryStream stringData = new MemoryStream();
            IWriter offsetWriter = new EndianWriter(offsetData, Endian.BigEndian);
            IWriter stringWriter = new EndianWriter(stringData, Endian.BigEndian);

            try
            {
                // Write the string and offset data to buffers
                foreach (Locale locale in locales.Strings)
                {
                    WriteLocalePointer(offsetWriter, locale.ID, (int)stringWriter.Position);
                    stringWriter.WriteUTF8(locale.Value);
                }

                // Round the size of the string data up
                int dataSize = (int)((stringData.Position + _sizeAlign - 1) & ~(_sizeAlign - 1));
                stringData.SetLength(dataSize);

                // Update the two locale data hashes if we need to
                // (the hash arrays are set to null if the build doesn't need them)
                if (IndexTableHash != null)
                    IndexTableHash = SHA1.Transform(offsetData.GetBuffer(), 0, (int)offsetData.Length);
                if (StringDataHash != null)
                    StringDataHash = SHA1.Transform(stringData.GetBuffer(), 0, dataSize);

                // Make sure there's free space for the offset table and then write it to the file
                LocaleIndexTable.Resize((int)offsetData.Length, stream);
                stream.SeekTo(LocaleIndexTableLocation.AsOffset());
                stream.WriteBlock(offsetData.GetBuffer(), 0, (int)offsetData.Length);

                // Encrypt the string data if necessary
                byte[] strings = stringData.GetBuffer();
                if (_encryptionKey != null)
                    strings = AES.Encrypt(strings, 0, dataSize, _encryptionKey.Key, _encryptionKey.IV);

                // Make sure there's free space for the string data and then write it to the file
                LocaleData.Resize(dataSize, stream);
                stream.SeekTo(LocaleDataLocation.AsOffset());
                stream.WriteBlock(strings, 0, dataSize);

                // Update the string count and recalculate the language table offsets
                StringCount = locales.Strings.Count;
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
            id = new StringID((int)values.GetNumber("stringid"));
            offset = (int)values.GetNumber("offset");
        }

        private void WriteLocalePointer(IWriter writer, StringID id, int offset)
        {
            StructureValueCollection values = new StructureValueCollection();
            values.SetNumber("stringid", (uint)id.Value);
            values.SetNumber("offset", (uint)offset);
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

        private string ReplaceSymbols(string locale)
        {
            // :)
            return _symbols.ReplaceSymbols(locale);
        }
    }
}
