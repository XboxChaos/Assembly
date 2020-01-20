using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.Net.Sockets
{
    public class SocketStream : Stream
    {
        private IPokeSessionManager _starter;
        private List<SocketAction> _actions;
        private string _buildName;
        private string _cacheName;

        public SocketStream(IPokeSessionManager starter, string buildName, string cacheName)
        {
            _starter = starter;
            _actions = new List<SocketAction>();
            _buildName = buildName;
            _cacheName = cacheName;
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
                    throw new NotImplementedException();
            }
            return Position;

        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            _starter.SendMemoryCommand(new MemoryCommand(_actions, _buildName, _cacheName));
            _actions.Clear();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Create a new byte array to write
            var writeBuffer = new byte[count];
            Buffer.BlockCopy(buffer, offset, writeBuffer, 0, count);

            // Send a MemoryCommand to the server
            _actions.Add(new SocketAction((Int64)Position, writeBuffer));
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

        public class SocketAction
        {
            public SocketAction(Int64 position, byte[] writeBuffer)
            {
                Position = position;
                Buffer = writeBuffer;
            }

            public byte[] Buffer { get; set; }

            public Int64 Position { get; set; }
        }
    }
}
