using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Serialization;
using Blamite.Util;
using System;
using System.IO;

namespace Blamite.RTE.FirstGen
{

    // TODO (Dragon): clean this up
    public class MapPointerReader
    {
        private readonly long _baseAddress;
        private long _mapHeaderAddress;

        public MapPointerReader(ProcessMemoryStream stream, EngineDescription engineInfo, long pointer)
        {
            _baseAddress = (long)stream.BaseProcess.MainModule.BaseAddress;
            _mapHeaderAddress = _baseAddress + pointer;

            GetLayoutConstants(engineInfo);

            var reader = new EndianReader(stream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
            ReadMapPointers32(reader);
            ReadMapHeader(reader);
            ProcessMapHeader();
        }

        public MapPointerReader(ProcessModuleMemoryStream stream, EngineDescription engineInfo, long pointer)
        {
            _baseAddress = (long)stream.BaseModule.BaseAddress;
            _mapHeaderAddress = _baseAddress + pointer;

            GetLayoutConstants(engineInfo);

            var reader = new EndianReader(stream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
            ReadMapPointers64(reader);
            ReadMapHeader(reader);
            ProcessMapHeader();
        }

        /// <summary>
        ///     Gets the address of the meta for the currently-loaded (non-shared) map.
        /// </summary>
        public long CurrentMetaAddress { get; private set; }

        /// <summary>
        ///     Gets the address of the meta for the currently-loaded shared map.
        ///     If no shared map is loaded, this will be the same as <see cref="CurrentMetaAddress" />.
        /// </summary>
        public long SharedMetaAddress { get; private set; }

        /// <summary>
        ///     Gets the type of the map that is currently loaded.
        /// </summary>
        public CacheFileType MapType { get; private set; }

        /// <summary>
        ///     Gets the name of the map that is currently loaded.
        /// </summary>
        public string MapName { get; private set; }

        /// <summary>
        ///     Gets the scenario name of the map that is currently loaded.
        /// </summary>
        public string ScenarioName { get; private set; }

        /// <summary>
        ///     Gets the full header of the map that is currently loaded.
        /// </summary>
        public byte[] MapHeader { get; private set; }

        // The size of the map header in bytes
        private int MapHeaderSize;

        // Offset (from the start of the map header) of the int32 indicating the map type
        private int MapTypeOffset;

        // Offset (from the start of the map header) of the ASCII string indicating the map name
        private int MapNameOffset;

        // Offset (from the start of the map header) of the ASCII string indicating the scenario name
        //private int ScenarioNameOffset;

        private void GetLayoutConstants(EngineDescription engineInfo)
        {
            MapHeaderSize = engineInfo.HeaderSize;

            var layout = engineInfo.Layouts.GetLayout("header");
            MapTypeOffset = layout.GetFieldOffset("type");
            MapNameOffset = layout.GetFieldOffset("internal name");
            //ScenarioNameOffset = layout.GetFieldOffset("scenario name");
        }

        // TODO (Dragon): this isnt right
        private void ReadMapPointers32(IReader reader)
        {
            // The shared meta pointer is immediately before the map header
            //reader.SeekTo(_mapHeaderAddress - 4);
            //SharedMetaAddress = reader.ReadUInt32();

            // The current meta pointer is immediately after the main header
            reader.SeekTo(_mapHeaderAddress + MapHeaderSize);
            CurrentMetaAddress = reader.ReadUInt32();
        }

        private void ReadMapPointers64(IReader reader)
        {
            // The shared meta pointer is immediately before the map header
            //reader.SeekTo(_mapHeaderAddress - 8);
            //SharedMetaAddress = reader.ReadInt64();

            // The current meta pointer is after the main header + 0x0C
            reader.SeekTo(_mapHeaderAddress + MapHeaderSize + 0x0C);
            CurrentMetaAddress = reader.ReadInt64();
        }

        private void ReadMapHeader(IReader reader)
        {
            reader.SeekTo(_mapHeaderAddress);
            MapHeader = reader.ReadBlock(MapHeaderSize);
        }

        private void ProcessMapHeader()
        {
            using (IReader reader = new EndianStream(new MemoryStream(MapHeader), Endian.LittleEndian))
            {
                reader.SeekTo(MapTypeOffset);
                MapType = (CacheFileType)reader.ReadInt32();

                reader.SeekTo(MapNameOffset);
                MapName = reader.ReadAscii();

                //reader.SeekTo(ScenarioNameOffset);
                //ScenarioName = reader.ReadAscii();
            }
        }

        private static readonly int MapHeaderMagic = CharConstant.FromString("head");

    }
}
