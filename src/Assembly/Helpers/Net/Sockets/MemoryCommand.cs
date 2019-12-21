using System;
using System.Collections.Generic;
using System.IO;
using Blamite.IO;

namespace Assembly.Helpers.Net.Sockets
{
    public class MemoryCommand : PokeCommand
    {
        public List<SocketStream.SocketAction> Actions { get; set; }

        public MemoryCommand() : base(PokeCommandType.Memory)
        {
            Actions = new List<SocketStream.SocketAction>();
        }

        public MemoryCommand(List<SocketStream.SocketAction> actions) : base(PokeCommandType.Memory)
        {
            Actions = actions;
        }

        public override void Deserialize(Stream stream)
        {
            using (var reader = new EndianReader(stream, Endian.BigEndian))
            {
                var count = reader.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    var position = reader.ReadInt64();
                    var size = reader.ReadInt32();
                    var buffer = reader.ReadBlock(size);
                    Actions.Add(new SocketStream.SocketAction(position, buffer));
                }
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
                var count = Actions.Count;
                writer.WriteInt32(count);

                foreach (var action in Actions)
                {
                    writer.WriteInt64(action.Position);
                    writer.WriteInt32(action.Buffer.Length);
                    writer.WriteBlock(action.Buffer);
                }
            }
        }
        public class Model
        {
            public uint Offset { get; set; }
            public byte[] Data { get; set; }
        }
    }
}
