using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.Net.Sockets
{
    public class SocketStream : Stream
    {
        private IPokeCommandStarter _starter;

        public SocketStream(IPokeCommandStarter starter)
        {
            _starter = starter;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;

                case SeekOrigin.Current:
                    Position += offset;
                    break;

                case SeekOrigin.End:
                    Position = 0x100000000 - offset;
                    break;
            }
            return Position;

        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Create a new byte array to write
            var writeBuffer = new byte[count];
            Buffer.BlockCopy(buffer, offset, writeBuffer, 0, count);

            // Send a MemoryCommand to the server
            var memory = new MemoryCommand((Int64)Position, writeBuffer);
            _starter.StartMemoryCommand(memory);
            Position += count;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return 0x100000000; }
        }

        public override long Position { get; set; }
    }
}
