using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.IO;
using XDevkit;

namespace XBDMCommunicator
{
    public class Xbdm
    {
        /// <summary>
        /// Create a new instance of the XBDM Communicator
        /// </summary>
        /// <param name="deviceIdent">The Name of IP of the XBox Console running xbdm.</param>
        /// <param name="openConnection">Open a connection to the XBox Console</param>
        public Xbdm(string deviceIdent, bool openConnection = false)
        {
            DeviceIdent = deviceIdent;
            _xboxMemoryStream = new XboxMemoryStream(this);

            if (openConnection)
                Connect();
        }


        // Private Modifiers
        private IXboxManager _xboxManager;
        private readonly XboxMemoryStream _xboxMemoryStream;
        private XboxConsole _xboxConsole;
        private IXboxDebugTarget _xboxDebugTarget;
	    private uint _xboxConnectionCode;


	    // Public Modifiers
	    public string DeviceIdent { get; private set; }
	    public string XboxType { get; private set; }
	    public bool IsConnected { get; private set; }

	    public XboxMemoryStream MemoryStream
        { 
            get { return _xboxMemoryStream; }
            //set { _xboxMemoryStream = value; }
        }


        // Public Functions
        /// <summary>
        /// Update the Xbox Device Ident (XDK Name or IP)
        /// </summary>
        /// <param name="deviceIdent">The new XBox XDK Name or IP</param>
        public void UpdateDeviceIdent(string deviceIdent)
        {
            if (DeviceIdent != deviceIdent)
                Disconnect();
            DeviceIdent = deviceIdent;
        }

        /// <summary>
        /// Open a connection to the XBox Console
        /// </summary>
        /// <returns>true if the connection was successful.</returns>
        public bool Connect()
        {
            if (!IsConnected)
            {
                try
                {
                    _xboxManager = new XboxManager();
                    _xboxConsole = _xboxManager.OpenConsole(DeviceIdent);
                    _xboxDebugTarget = _xboxConsole.DebugTarget;
                    _xboxConnectionCode = _xboxConsole.OpenConnection(null);
                }
                catch
                {
                    _xboxManager = null;
                    _xboxConsole = null;
                    _xboxDebugTarget = null;
                    return false;
                }

                try { XboxType = _xboxConsole.ConsoleType.ToString(); }
                catch { XboxType = "Unable to get."; }

                IsConnected = true;
            }
            return true;
        }

        /// <summary>
        /// Close the connection to the XBox Console
        /// </summary>
        public void Disconnect()
        {
	        if (!IsConnected) return;

	        if (_xboxConsole != null)
		        _xboxConsole.CloseConnection(_xboxConnectionCode);

	        _xboxManager = null;
	        _xboxDebugTarget = null;
	        _xboxConsole = null;
	        IsConnected = false;
        }

        /// <summary>
        /// Send a string-based command, such as "bye", "reboot", "go", "stop"
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <returns>The responce from the console, or null if sending the command failed.</returns>
        public string SendStringCommand(string command)
        {
            if (!Connect())
                return null;
            
            string response;
            _xboxConsole.SendTextCommand(_xboxConnectionCode, command, out response);
            // Alex: Personally I feel that we should always return the response here and leave it up to the caller to check it
            // -- Aaron
            //if (!(response.Contains("202") | response.Contains("203")))
                return response;
            /*else
                throw new Exception("String command wasn't accepted by the Xbox 360 Console. It might not be valid or the Xbox is just being annoying. The response was:\n" + response);*/
        }

        /// <summary>
        /// Freeze the XBox Console
        /// </summary>
        public void Freeze()
        {
            if (!Connect())
                return;

            SendStringCommand("stop");
        }

        /// <summary>
        /// UnFreeze the XBox Console
        /// </summary>
        public void Unfreeze()
        {
            if (!Connect())
                return;

            SendStringCommand("go");
        }    
    
        /// <summary>
        /// Reboot the XBox Console
        /// </summary>
        /// <param name="rebootType">The type of Reboot to do (Cold or Title)</param>
        public void Reboot(RebootType rebootType)
        {
            if (!Connect())
                return;

            switch (rebootType)
            {
                case RebootType.Cold:
                    SendStringCommand("reboot");
                    break;
                case RebootType.Title:
                    SendStringCommand("reboot");
                    break;
            }
        }

        /// <summary>
        /// Shutdown the XBox Console
        /// </summary>
        public void Shutdown()
        {
            if (!Connect())
                return;

            // Tell console to go bye-bye
            SendStringCommand("bye");

            Disconnect();
        }

        /// <summary>
        /// Save a screenshot from the XBox Console
        /// </summary>
        /// <param name="savePath">The location to save the image to.</param>
        /// <param name="freezeDuring">Do you want to freeze while the screenshot is being taken.</param>
        public bool GetScreenshot(string savePath, bool freezeDuring = false)
        {
            if (!Connect())
                return false;

            // Stop the Console
            if (freezeDuring)
                Freeze();

            // Screensnap that console
            _xboxConsole.ScreenShot(savePath);

            // Start the Console
            if (freezeDuring)
                Unfreeze();

	        return true;
        }
        
        // Enum Declaration
        public enum RebootType { Cold, Title }
        
        // Memory IO
        public class XboxMemoryStream : IStream
        {
            public XboxMemoryStream(Xbdm xbdm)
            {
	            EOF = false;
	            _xbdm = xbdm;
                Position = 0;
            }

            // Private Modifiers
            private readonly Xbdm _xbdm;
	        private const long _length = 0xFFFFFFFF;

	        // IO Functions
            #region Read
            private byte[] ReadPureBytes(uint length)
            {
                if (!_xbdm.Connect())
                    return null;

                var flag = true;
                if (length > 20)
                    _xbdm._xboxDebugTarget.Stop(out flag);

	            uint bytesRead;
                var output = new byte[length];
                _xbdm._xboxDebugTarget.GetMemory((uint)Position, length, output, out bytesRead);
                SeekTo(Position + length);

                if (!flag)
                    _xbdm._xboxDebugTarget.Go(out flag);

                return output;
            }
            
            public byte[] ReadBlock(int size)
            {
                return ReadPureBytes((uint)size);
            }
            public int ReadBlock(byte[] output, int offset, int size)
            {
                throw new NotImplementedException();
            }

            public byte ReadByte()
            {
                return ReadPureBytes(1)[0];
            }
            public sbyte ReadSByte()
            {
                return (sbyte)ReadPureBytes(1)[0];
            }

            public float ReadFloat()
            {
                return BitConverter.ToSingle(ReadPureBytes(4), 0);
            }

            public short ReadInt16()
            {
                return BitConverter.ToInt16(ReadPureBytes(2), 0);
            }
            public int ReadInt32()
            {
                return BitConverter.ToInt32(ReadPureBytes(4), 0);
            }
            public long ReadInt64()
            {
                return BitConverter.ToInt64(ReadPureBytes(8), 0);
            }

            public ushort ReadUInt16()
            {
                return BitConverter.ToUInt16(ReadPureBytes(2), 0);
            }
            public uint ReadUInt32()
            {
                return BitConverter.ToUInt32(ReadPureBytes(4), 0);
            }
            public ulong ReadUInt64()
            {
                return BitConverter.ToUInt64(ReadPureBytes(8), 0);
            }

            public string ReadAscii(int size)
            {
                return Encoding.ASCII.GetString(ReadPureBytes((uint)size));
            }
            public string ReadAscii()
            {
                throw new NotImplementedException();
            }

            public string ReadUTF8(int size)
            {
                return Encoding.UTF8.GetString(ReadPureBytes((uint)size));
            }
            public string ReadUTF8()
            {
                throw new NotImplementedException();
            }

            public string ReadUTF16(int size)
            {
                return Encoding.Unicode.GetString(ReadPureBytes((uint)size));
            }
            public string ReadUTF16()
            {
                throw new NotImplementedException();
            }
            #endregion
            #region Write
            private void WritePureBytes(byte[] input)
            {
                if (!_xbdm.Connect())
                    return;

                var flag = true;
                if (input.Length > 20)
                    _xbdm._xboxDebugTarget.Stop(out flag);

	            uint bytesRead;
                _xbdm._xboxDebugTarget.SetMemory((uint)Position, (uint)input.Length, input, out bytesRead);
                SeekTo(Position + input.Length);

                if (!flag)
                    _xbdm._xboxDebugTarget.Go(out flag);
            }

            public void WriteBlock(byte[] data, int offset, int size)
            {
                var input = new List<byte>();
                for (var i = offset; i < offset + size; i++)
                {
                    if (i > data.Length)
                        break;

                    input.Add(data[i]);
                }

                WritePureBytes(input.ToArray<byte>());
            }
            public void WriteBlock(byte[] data)
            {
                WritePureBytes(data);
            }

            public void WriteByte(byte value)
            {
                WritePureBytes(new[] { value });
            }
            public void WriteSByte(sbyte value)
            {
                WritePureBytes(new[] { (byte)value });
            }

            public void WriteFloat(float value)
            {
                var input = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                WritePureBytes(input);
            }

            public void WriteInt16(short value)
            {
                var input = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                WritePureBytes(input);
            }
            public void WriteInt32(int value)
            {
				var input = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                WritePureBytes(input);
            }
            public void WriteInt64(long value)
            {
				var input = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                WritePureBytes(input);
            }

            public void WriteUInt16(ushort value)
            {
				var input = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                WritePureBytes(input);
            }
            public void WriteUInt32(uint value)
            {
				var input = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                WritePureBytes(input);
            }
            public void WriteUInt64(ulong value)
            {
				var input = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                WritePureBytes(input);
            }

            public void WriteAscii(string str)
            {
				var input = Encoding.ASCII.GetBytes(str + char.MinValue);

                WritePureBytes(input);
            }

            public void WriteUTF8(string str)
            {
                byte[] data = Encoding.UTF8.GetBytes(str);
                WritePureBytes(data);
            }

            public void WriteUTF16(string str)
            {
				var input = Encoding.Unicode.GetBytes(str + char.MinValue);

                // Make Big Endian
				for (var i = 0; i < input.Length; i += 2)
                {
					var temp = input[i];
                    input[i] = input[i + 1];
                    input[i + 1] = temp;
                }
                WritePureBytes(input);
            }
            #endregion

            // Private Functions
            /// <summary>
            /// Gets wether the address is within the Xbox 360's memory
            /// </summary>
            /// <param name="address">The XBox 360 Memory Address</param>
            private static bool IsValidXboxMemoryAddress(long address)
            {
                return (address >= 0 && address <= 0xFFFFFFFF);
            }


            // Public Functions
            public void Close() { }
            public void Dispose() { Close(); }
	        public bool EOF { get; private set; }
	        public long Length { get { return _length; } }
	        public long Position { get; private set; }

	        public bool SeekTo(long address)
            {
	            // Check if seek is valid
	            if (!IsValidXboxMemoryAddress(address))
					return false;

	            Position = address;
	            return true;
            }

	        public void Skip(long count) { Position += count; }
        }
    }
}