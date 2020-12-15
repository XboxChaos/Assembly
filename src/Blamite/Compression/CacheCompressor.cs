using Blamite.Blam;
using Blamite.Blam.SecondGen;
using Blamite.IO;
using Blamite.Serialization;
using Blamite.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// TODO (Dragon): write compress/decompress for CEA 360

namespace Blamite.Compression
{
    public enum CompressionState
    {
        Null,
        Compressed,
        Decompressed
    }

    /// <summary>
    /// A class for handling MCC's cache compression
    /// </summary>
    public static class CacheCompressor
    {
        /// <summary>
        /// Reads in the input file, determines its current compression state, and reverses it by default.
        /// </summary>
        /// <param name="input">Cache file to compress</param>
        /// <param name="engineDb">The engine database to use to process the cache file.</param>
        /// <param name="desiredState">Optional. When set to not null, the default behavior is overridden and skips action if the cache file is already that state.</param>
        /// <returns></returns>
        public static CompressionState HandleCompression(string input, EngineDatabase engineDb, CompressionState desiredState = CompressionState.Null)
        {
            CompressionState state;
            EngineType type;
            EngineDescription engineInfo;
            using (FileStream fileStream = File.OpenRead(input))
            {
                // TODO (Dragon): dunno why we set endian here if we just change it later
                //var reader = new EndianReader(fileStream, Endian.BigEndian);
                var reader = new EndianReader(fileStream, Endian.LittleEndian);

                state = DetermineState(reader, engineDb, out type, out engineInfo);
            }

            if (state == desiredState)
                return state;
            switch (state)
            {
                default:
                case CompressionState.Null:
                    return state;
                case CompressionState.Compressed:
                    {
                        // TODO (Dragon): idk if its better to handle this in a wrapper
                        // TODO (Dragon): hacky and bad i know
                        if (type == EngineType.FirstGeneration && engineInfo.Name == "Halo 1")
                        {
                            DecompressFirstGenCEXbox(input);
                            return CompressionState.Decompressed;
                        }
                        else if (type == EngineType.FirstGeneration && engineInfo.Name == "Halo 1 Beta 2247")
                        {
                            DecompressFirstGenCEXbox(input);
                            return CompressionState.Decompressed;
                        }
                        // TODO (Dragon): hacky and bad i know
                        else if (type == EngineType.FirstGeneration && engineInfo.Name == "Halo 1 Anniversary")
                        {
                            DecompressFirstGenCEA360(input);
                            return CompressionState.Decompressed;
                        }
                        // TODO (Dragon): hacky and bad i know
                        else if (type == EngineType.FirstGeneration && engineInfo.Name == "Halo 1 Anniversary MCC")
                        {
                            DecompressFirstGenCEAMCC(input);
                            return CompressionState.Decompressed;
                        }
                        else if (type == EngineType.SecondGeneration)
                        {
                            DecompressSecondGen(input);
                            return CompressionState.Decompressed;
                        }
                        else
                            return state;
                    }
                case CompressionState.Decompressed:
                    {
                        // TODO (Dragon): idk if its better to handle this in a wrapper
                        // TODO (Dragon): hacky and bad i know
                        if (type == EngineType.FirstGeneration && engineInfo.Name == "Halo 1")
                        {
                            CompressFirstGenCEXbox(input);
                            return CompressionState.Compressed;
                        }
                        else if (type == EngineType.FirstGeneration && engineInfo.Name == "Halo 1 Beta 2247")
                        {
                            CompressFirstGenCEXbox(input);
                            return CompressionState.Compressed;
                        }
                        // TODO (Dragon): hacky and bad i know
                        else if (type == EngineType.FirstGeneration && engineInfo.Name == "Halo 1 Anniversary")
                        {
                            CompressFirstGenCEA360(input);
                            return CompressionState.Compressed;
                        }
                        // TODO (Dragon): hacky and bad i know
                        else if (type == EngineType.FirstGeneration && engineInfo.Name == "Halo 1 Anniversary MCC")
                        {
                            CompressFirstGenCEAMCC(input);
                            return CompressionState.Compressed;
                        }
                        else if (type == EngineType.SecondGeneration)
                        {
                            CompressSecondGen(input);
                            return CompressionState.Compressed;
                        }
                        else
                            return state;
                    }
            }
        }

        // TODO (Dragon): support CEA 360 with LZX
        private static CompressionState DetermineState(IReader reader, EngineDatabase engineDb, out EngineType type, out EngineDescription engineInfo)
        {
            CacheFileVersionInfo version = null;

            // not all compressed maps have a decompressed header
            // so we handle that here
            try
            {
                // Attempt to set the reader's endianness based upon the file's header magic
                reader.SeekTo(0);
                byte[] headerMagic = reader.ReadBlock(4);
                reader.Endianness = CacheFileLoader.DetermineCacheFileEndianness(headerMagic);

                // Load engine version info
                version = new CacheFileVersionInfo(reader);
            }
            catch (ArgumentException e) // map had no header, assume its CEA
            {
                using (MemoryStream ms_header_out = new MemoryStream())
                {
                    // first chunk offset is at 0x4
                    reader.SeekTo(0x4);
                    int first_chunk_offset = reader.ReadInt32();
                    int second_chunk_offset = reader.ReadInt32();
                    int first_chunk_size = second_chunk_offset - first_chunk_offset - 6;

                    reader.SeekTo(first_chunk_offset);

                    // CEA 360 stores an 0xFF, use it for ID
                    byte cea_360_ff_byte = reader.ReadByte();
                    if (cea_360_ff_byte == 0xFF)    // CEA 360
                    {
                        // TODO (Dragon): decompress first chunk to get the header with lzx
                        throw new InvalidOperationException("assembly does not support CEA 360 decompression (missing LZX)");
                    }
                    else // assume CEA MCC
                    {
                        reader.SeekTo(first_chunk_offset + 6);
                        byte[] first_chunk_bytes = reader.ReadBlock(first_chunk_size);
                        using (MemoryStream ms_header_comp = new MemoryStream(first_chunk_bytes))
                        {
                            //ms_header_comp.Write(first_chunk_bytes, 0, first_chunk_size);
                            using (DeflateStream ds = new DeflateStream(ms_header_comp, CompressionMode.Decompress))
                            {
                                ds.CopyTo(ms_header_out);
                            }
                        }
                    }

                    EndianReader header_reader = new EndianReader(ms_header_out, Endian.LittleEndian);
                    version = new CacheFileVersionInfo(header_reader);

                }
            }

            // if version wasnt set its because we couldnt read a proper header, throw an exception
            if (version == null)
            {
                throw new NullReferenceException("Failed to create CacheFileVersionInfo from map header");
            }

            type = version.Engine;
            engineInfo = engineDb.FindEngineByVersion(version.BuildString);

            if (version.Engine == EngineType.FirstGeneration)
            {
                if (engineInfo == null)
                    return CompressionState.Null;

                if (!engineInfo.UsesCompression)
                    return CompressionState.Null;

                return AnalyzeFirstGen(reader, engineInfo);

            }
            else if (version.Engine == EngineType.SecondGeneration)
            {
                if (engineInfo == null)
                    return CompressionState.Null;

                if (!engineInfo.UsesCompression)
                    return CompressionState.Null;

                return AnalyzeSecondGen(reader, engineInfo);
            }
            else
                return CompressionState.Null;
        }

        #region First Generation

        private static CompressionState AnalyzeFirstGen(IReader reader, EngineDescription engineInfo)
        {
            reader.SeekTo(0);
            StructureValueCollection headerValues = StructureReader.ReadStructure(reader, engineInfo.Layouts.GetLayout("header"));

            var metaOffset = (int)headerValues.GetInteger("meta offset");

            if (metaOffset >= reader.Length)
                return CompressionState.Compressed;


            reader.SeekTo(metaOffset);

            StructureValueCollection tagTableValues = StructureReader.ReadStructure(reader, engineInfo.Layouts.GetLayout("meta header"));

            if ((uint)tagTableValues.GetInteger("magic") != CharConstant.FromString("tags"))
                return CompressionState.Compressed;

            return CompressionState.Decompressed;
        }

        // TODO (Dragon): using these wrappers may make things cleaner
        // NOTE: wrappers because first gen has multiple methods of compression
        private static void CompressFirstGen(string file)
        {
            // TODO (Dragon): check for and compress CE Xbox
            // TODO (Dragon): check for and compress CEA 360
            // TODO (Dragon): check for and compress CEA MCC
        }

        private static void DecompressFirstGen(string file)
        {
            // TODO (Dragon): check for and decompress CE Xbox
            // TODO (Dragon): check for and decompress CEA 360
            // TODO (Dragon): check for and decompress CEA MCC
        }

        #region Original Xbox

        // TODO (Dragon): see if theres a good way to minimize memory consumption
        private static void CompressFirstGenCEXbox(string file)
        {
            using (MemoryStream msOutput = new MemoryStream())
            {
                using (FileStream fsInput = new FileStream(file, FileMode.Open))
                {
                    using (BinaryReader brInput = new BinaryReader(fsInput))
                    {
                        //header is uncompressed
                        msOutput.Write(brInput.ReadBytes(0x800), 0, 0x800);

                        int realsize = (int)fsInput.Length - 0x800;
                        byte[] chunkData = new byte[realsize];

                        fsInput.Seek(0x800, SeekOrigin.Begin);
                        fsInput.Read(chunkData, 0, realsize);

                        msOutput.WriteByte((byte)0x78);
                        msOutput.WriteByte((byte)0x9C);

                        using (DeflateStream ds = new DeflateStream(msOutput, CompressionMode.Compress, true))
                        {
                            realsize = fsInput.Read(chunkData, 0, chunkData.Length);
                            ds.Write(chunkData, 0, chunkData.Length);
                        }

                        // NOTE: actual zlib has an adler-32 checksum trailer on the end
                        uint adler = Adler32.Calculate(chunkData);

                        // write the bytes
                        msOutput.WriteByte((byte)((adler & 0xFF000000) >> 24));
                        msOutput.WriteByte((byte)((adler & 0xFF0000) >> 16));
                        msOutput.WriteByte((byte)((adler & 0xFF00) >> 8));
                        msOutput.WriteByte((byte)(adler & 0xFF));

                        // CE xbox has some padding on the end to a 0x800 alignment
                        int pad_size = 0x800 - ((int)msOutput.Length % 0x800);
                        byte[] padding = new byte[pad_size];
                        msOutput.Write(padding, 0, pad_size);
                    }
                }
                File.WriteAllBytes(file, msOutput.ToArray());
            }
        }

        // TODO (Dragon): see if theres a good way to minimize memory consumption
        private static void DecompressFirstGenCEXbox(string file)
        {
            using (MemoryStream msOutput = new MemoryStream())
            {
                using (FileStream fsInput = new FileStream(file, FileMode.Open))
                {
                    using (BinaryReader brInput = new BinaryReader(fsInput))
                    {
                        //header is uncompressed
                        msOutput.Write(brInput.ReadBytes(0x800), 0, 0x800);

                        fsInput.Seek(0x8, SeekOrigin.Begin); // TODO (Dragon): dont hardcode offset 
                        int realsize = brInput.ReadInt32() - 0x800;
                        byte[] chunkData = new byte[realsize];

                        fsInput.Seek(0x802, SeekOrigin.Begin);
                        using (DeflateStream ds = new DeflateStream(fsInput, CompressionMode.Decompress, true))
                        {
                            realsize = ds.Read(chunkData, 0, chunkData.Length);
                        }
                        msOutput.Write(chunkData, 0, realsize);
                    }
                }
                File.WriteAllBytes(file, msOutput.ToArray());
            }
        }
        #endregion

        #region CEA 360

        // TODO (Dragon): add some kinda lzx and add CEA 360 support
        private static void CompressFirstGenCEA360(string file)
        {
            throw new InvalidOperationException("assembly does not support CEA 360 compression (missing LZX)");
        }

        private static void DecompressFirstGenCEA360(string file)
        {
            throw new InvalidOperationException("assembly does not support CEA 360 decompression (missing LZX)");
        }
        #endregion

        #region CEA MCC

        private static void CompressFirstGenCEAMCC(string file)
        {
            using (MemoryStream msOutput = new MemoryStream())
            {
                using (BinaryWriter bwOutput = new BinaryWriter(msOutput))
                {
                    using (FileStream fsInput = new FileStream(file, FileMode.Open))
                    {
                        List<int> chunk_offsets = new List<int>();

                        int datalength = (int)fsInput.Length;
                        int chunkcount = (((datalength + 0x1FFFF) & ~0x1FFFF) / 0x20000);

                        bwOutput.Write(chunkcount);

                        int datastart = 0x40000;
                        msOutput.Position = datastart;

                        for (int i = 0; i < chunkcount; i++)
                        {
                            chunk_offsets.Add((int)bwOutput.BaseStream.Position);
                            int size = 0x20000;
                            if (i == chunkcount - 1)
                                size = (int)fsInput.Length % 0x20000;

                            bwOutput.Write(size);
                            bwOutput.Write((byte)0x28);
                            bwOutput.Write((byte)0x15);

                            using (DeflateStream ds = new DeflateStream(msOutput, CompressionLevel.Fastest, true))
                            {
                                byte[] chunkData = new byte[size];
                                fsInput.Read(chunkData, 0, size);
                                ds.Write(chunkData, 0, chunkData.Length);
                            }
                        }

                        msOutput.Position = 0x4;
                        for (int i = 0; i < chunkcount; i++)
                        {
                            bwOutput.Write(chunk_offsets[i]);
                        }
                    }
                    File.WriteAllBytes(file, msOutput.ToArray());
                }
            }
        }

        private static void DecompressFirstGenCEAMCC(string file)
        {
            using (MemoryStream msOutput = new MemoryStream())
            {
                using (FileStream fsInput = new FileStream(file, FileMode.Open))
                {
                    using (BinaryReader brInput = new BinaryReader(fsInput))
                    {
                        int chunk_count = brInput.ReadInt32();

                        List<int> chunk_offsets = new List<int>();

                        for (int i = 0; i < chunk_count; i++)
                        {
                            int offset = brInput.ReadInt32();

                            if (offset >= fsInput.Length)
                                throw new ArgumentException("Chunk " + i + " has an offset past the end of the file.");

                            chunk_offsets.Add(offset);
                        }

                        for (int i = 0; i < chunk_offsets.Count(); i++)
                        {
                            fsInput.Seek(chunk_offsets[i], SeekOrigin.Begin);

                            int realsize = brInput.ReadInt32();

                            fsInput.Seek(chunk_offsets[i] + 6, SeekOrigin.Begin);

                            byte[] chunkData = new byte[realsize];

                            using (DeflateStream ds = new DeflateStream(fsInput, CompressionMode.Decompress, true))
                            {
                                ds.Read(chunkData, 0, chunkData.Length);
                            }

                            msOutput.Write(chunkData, 0, realsize);
                        }
                    }
                }
                File.WriteAllBytes(file, msOutput.ToArray());
            }
        }
        #endregion
        #endregion

        #region Second Generation
        private static CompressionState AnalyzeSecondGen(IReader reader, EngineDescription engineInfo)
        {
            // H2 header is uncompressed, so the cache file needs to be loaded enough to check if the tag table is readable
            var segmenter = new FileSegmenter(engineInfo.SegmentAlignment);

            reader.SeekTo(0);
            StructureValueCollection headerValues = StructureReader.ReadStructure(reader, engineInfo.Layouts.GetLayout("header"));

            var metaOffset = (int)headerValues.GetInteger("meta offset");
            var metaSize = (int)headerValues.GetInteger("meta size");
            uint metaOffsetMask = (uint)headerValues.GetInteger("meta offset mask");

            var metaSegment = new FileSegment(
                segmenter.DefineSegment(metaOffset, metaSize, 0x200, SegmentResizeOrigin.Beginning), segmenter);
            var MetaArea = new FileSegmentGroup(new MetaOffsetConverter(metaSegment, metaOffsetMask));
            MetaArea.AddSegment(metaSegment);

            if (MetaArea.Offset >= reader.Length)
                return CompressionState.Compressed;

            reader.SeekTo(MetaArea.Offset);
            StructureValueCollection tagTableValues = StructureReader.ReadStructure(reader, engineInfo.Layouts.GetLayout("meta header"));

            if ((uint)tagTableValues.GetInteger("magic") != CharConstant.FromString("tags"))
                return CompressionState.Compressed;

            return CompressionState.Decompressed;
        }

        private static void CompressSecondGen(string file)
        {
            using (MemoryStream msOutput = new MemoryStream())
            {
                using (BinaryWriter bwOutput = new BinaryWriter(msOutput))
                {
                    using (FileStream fsInput = new FileStream(file, FileMode.Open))
                    {
                        List<Tuple<int, int>> Chunks = new List<Tuple<int, int>>();

                        //header is uncompressed
                        byte[] header = new byte[0x1000];
                        fsInput.Read(header, 0, 0x1000);
                        msOutput.Write(header, 0, 0x1000);

                        int datalength = (int)fsInput.Length - 0x1000;
                        int chunkcount = ((datalength + 0x3FFFF) & ~0x3FFFF) / 0x40000;

                        int datastart = 0x3000;
                        msOutput.Position = datastart;

                        while (fsInput.Position < fsInput.Length)
                        {
                            int size = 0x40000;
                            if (fsInput.Length - fsInput.Position < size)
                                size = datalength % 0x40000;

                            int start = (int)msOutput.Position;

                            // 1) deflatestream doesnt write a header.
                            // 2) this specific header (x2815) is whats used internally, and h2 mcc wont load without it, regardless of the actual data
                            bwOutput.Write((short)5416);

                            using (DeflateStream ds = new DeflateStream(msOutput, CompressionMode.Compress, true))
                            {
                                byte[] chunkData = new byte[size];
                                fsInput.Read(chunkData, 0, size);
                                ds.Write(chunkData, 0, chunkData.Length);
                            }

                            int complength = (int)msOutput.Position - start;
                            Chunks.Add(new Tuple<int, int>(complength, start));

                            //each chunk is padded
                            long remainder = complength % 0x80;
                            msOutput.Seek(0x80 - remainder, SeekOrigin.Current);
                        }

                        msOutput.Position = 0x1000;
                        for (int i = 0; i < chunkcount; i++)
                        {
                            bwOutput.Write(Chunks[i].Item1);
                            bwOutput.Write(Chunks[i].Item2);
                        }
                    }
                    File.WriteAllBytes(file, msOutput.ToArray());
                }
            }
        }

        private static void DecompressSecondGen(string file)
        {
            using (MemoryStream msOutput = new MemoryStream())
            {
                using (FileStream fsInput = new FileStream(file, FileMode.Open))
                {
                    using (BinaryReader brInput = new BinaryReader(fsInput))
                    {
                        //header is uncompressed
                        msOutput.Write(brInput.ReadBytes(0x1000), 0, 0x1000);

                        List<Tuple<int, int>> Chunks = new List<Tuple<int, int>>();

                        for (int i = 0; i < 0x400; i++)
                        {
                            int csize = brInput.ReadInt32();
                            int offset = brInput.ReadInt32();

                            if (csize == 0)
                                break;

                            if (offset >= fsInput.Length)
                                throw new ArgumentException("Chunk " + i + " has an offset past the end of the file.");

                            Chunks.Add(new Tuple<int, int>(csize, offset));
                        }

                        //Decompress and write each chunk
                        for (int i = 0; i < Chunks.Count; i++)
                        {
                            //check for faux-compression some other tools use
                            if (Chunks[i].Item1 < 0)
                            {
                                int invertedSize = -Chunks[i].Item1;
                                byte[] aaa = new byte[invertedSize];

                                fsInput.Seek(Chunks[i].Item2, SeekOrigin.Begin);

                                int readSize = fsInput.Read(aaa, 0, invertedSize);

                                msOutput.Write(aaa, 0, readSize);
                            }
                            else
                            {
                                fsInput.Seek(Chunks[i].Item2 + 2, SeekOrigin.Begin);

                                int realsize = 0x40000;
                                byte[] chunkData = new byte[realsize];

                                using (DeflateStream ds = new DeflateStream(fsInput, CompressionMode.Decompress, true))
                                {
                                    realsize = ds.Read(chunkData, 0, chunkData.Length);
                                }

                                msOutput.Write(chunkData, 0, realsize);
                            }
                        }
                    }
                }
                File.WriteAllBytes(file, msOutput.ToArray());
            }
        }
        #endregion
    }
}
