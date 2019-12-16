using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.Net.Sockets
{
    public class TestCommand : PokeCommand
    {
        public TestCommand() : base(PokeCommandType.Test)
        {
        }

        public TestCommand(string message) : base(PokeCommandType.Test)
        {
            Message = message;
        }


        public string Message { get; set; }

        public override void Deserialize(Stream stream)
        {
            var length = stream.ReadByte();
            var builder = new StringBuilder();
            var byteArray = new byte[0x1000];
            while (length > 0)
            {
                var read = stream.Read(byteArray, 0, length);
                length -= read;
                var str = Encoding.ASCII.GetString(byteArray, 0, read);
                builder.Append(str);
            }
            Message = builder.ToString();

        }

        public override void Serialize(Stream stream)
        {
            stream.WriteByte((byte)Message.Length);
            var stringBytes = Encoding.ASCII.GetBytes(Message);
            stream.Write(stringBytes, 0, stringBytes.Length);
        }

        public override void Handle(IPokeCommandHandler handle)
        {
            handle.HandleTestCommand(this);
        }
    }
}