using System;
using System.IO;
using Blamite.IO;

namespace Assembly.Helpers.Net.Sockets
{
    public class MemoryCommand : PokeCommand
    {
        public Int64 Offset { get; set; }
        public byte[] Data { get; set; }

        public MemoryCommand() : base(PokeCommandType.Memory)
        {

        }

        public MemoryCommand(long offset, byte[] data) : base(PokeCommandType.Memory)
        {

            Offset = offset;
            Data = data;
        }

        public override void Deserialize(Stream stream)
        {

            using (var reader = new EndianReader(stream, Endian.BigEndian))
            {
                Offset = reader.ReadInt64();
                var size = reader.ReadInt32();
                Data = reader.ReadBlock(size);
            }
        }

        public override void Handle(IPokeCommandHandler handle)
        {
            // route through halomap local rteProvider I think
            handle.HandleMemoryCommand(this);
        }

        public override void Serialize(Stream stream)
        {
            using (var writer = new EndianWriter(stream, Endian.BigEndian))
            {
                writer.WriteInt64(Offset);
                writer.WriteInt32(Data.Length);
                writer.WriteBlock(Data);
            }
        }
        public class Model
        {
            public uint Offset { get; set; }
            public byte[] Data { get; set; }
        }
    }
}
