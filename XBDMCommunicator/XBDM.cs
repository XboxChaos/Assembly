using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ExtryzeDLL.IO;
using XDevkit;

namespace XBDMCommunicator
{
    public class XBDM
    {
        /// <summary>
        /// Create a new instance of the XBDM Communicator
        /// </summary>
        /// <param name="deviceIdent">The Name of IP of the XBox Console running xbdm.</param>
        /// <param name="openConnection">Open a connection to the XBox Console</param>
        public XBDM(string deviceIdent, bool openConnection = false)
        {
            _deviceIdent = deviceIdent;
            _xboxMemoryStream = new XboxMemoryStream(this);

            if (openConnection)
                Connect();
        }


        // Private Modifiers
        private IXboxManager _xboxManager;
        private XboxMemoryStream _xboxMemoryStream;
        private XboxConsole _xboxConsole;
        private IXboxDebugTarget _xboxDebugTarget;
        private string _deviceIdent;
        private string _xboxType;
        private uint _xboxConnectionCode;
        private bool _isConnected;


        // Public Modifiers
        public String DeviceIdent { get { return _deviceIdent; } }
        public String XboxType { get { return _xboxType; } }
        public Boolean IsConnected { get { return _isConnected; } }
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
            if (_deviceIdent != deviceIdent)
                Disconnect();
            _deviceIdent = deviceIdent;
        }

        /// <summary>
        /// Open a connection to the XBox Console
        /// </summary>
        /// <returns>true if the connection was successful.</returns>
        public bool Connect()
        {
            if (!_isConnected)
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

                try { _xboxType = _xboxConsole.ConsoleType.ToString(); }
                catch { _xboxType = "Unable to get."; }

                _isConnected = true;
            }
            return true;
        }

        /// <summary>
        /// Close the connection to the XBox Console
        /// </summary>
        public void Disconnect()
        {
            if (_isConnected)
            {
                if (_xboxConsole != null)
                    _xboxConsole.CloseConnection(_xboxConnectionCode);

                _xboxManager = null;
                _xboxDebugTarget = null;
                _xboxConsole = null;
                _isConnected = false;
            }
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
            
            string response = "";
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
                    SendStringCommand("bye");
                    Disconnect();
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
        public void GetScreenshot(string savePath, bool freezeDuring = false)
        {
            if (!Connect())
                return;

            // Stop the Console
            if (freezeDuring)
                Freeze();

            // Screensnap that console
            _xboxConsole.ScreenShot(savePath);

            // Start the Console
            if (freezeDuring)
                Unfreeze();
        }
        
        // Enum Declaration
        public enum RebootType { Cold, Title }
        
        // Memory IO
        public class XboxMemoryStream : IWriter, IReader
        {
            public XboxMemoryStream(XBDM xbdm)
            {
                _xbdm = xbdm;
                _address = 0;
            }

            // Private Modifiers
            private XBDM _xbdm;
            private long _address;
            private bool _eof = false;
            private long _length = 0xFFFFFFFF;

            // IO Functions
            #region Read
            private byte[] ReadPureBytes(uint length)
            {
                if (!_xbdm.Connect())
                    return null;

                bool flag = true;
                if (length > 20)
                    _xbdm._xboxDebugTarget.Stop(out flag);

                uint bytesRead = 0;
                byte[] output = new byte[length];
                _xbdm._xboxDebugTarget.GetMemory((uint)_address, length, output, out bytesRead);
                SeekTo(_address + length);

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

                bool flag = true;
                if (input.Length > 20)
                    _xbdm._xboxDebugTarget.Stop(out flag);

                uint bytesRead = 0;
                _xbdm._xboxDebugTarget.SetMemory((uint)_address, (uint)input.Length, input, out bytesRead);
                SeekTo(_address + input.Length);

                if (!flag)
                    _xbdm._xboxDebugTarget.Go(out flag);
            }

            public void WriteBlock(byte[] data, int offset, int size)
            {
                List<byte> input = new List<byte>();
                for (int i = offset; i < offset + size; i++)
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
                WritePureBytes(new byte[] { value });
            }
            public void WriteSByte(sbyte value)
            {
                WritePureBytes(new byte[] { (byte)value });
            }

            public void WriteFloat(float value)
            {
                byte[] input = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                WritePureBytes(input);
            }

            public void WriteInt16(short value)
            {
                byte[] input = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                WritePureBytes(input);
            }
            public void WriteInt32(int value)
            {
                byte[] input = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                WritePureBytes(input);
            }
            public void WriteInt64(long value)
            {
                byte[] input = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                WritePureBytes(input);
            }

            public void WriteUInt16(ushort value)
            {
                byte[] input = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                WritePureBytes(input);
            }
            public void WriteUInt32(uint value)
            {
                byte[] input = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                WritePureBytes(input);
            }
            public void WriteUInt64(ulong value)
            {
                byte[] input = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                WritePureBytes(input);
            }

            public void WriteAscii(string str)
            {
                byte[] input = Encoding.ASCII.GetBytes(str + char.MinValue);

                WritePureBytes(input);
            }

            public void WriteUTF16(string str)
            {
                byte[] input = Encoding.Unicode.GetBytes(str + char.MinValue);

                // Make Big Endian
                for (int i = 0; i < input.Length; i += 2)
                {
                    byte temp = input[i];
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
            private bool IsValidXboxMemoryAddress(long address)
            {
                return (address >= 0 && address <= 0xFFFFFFFF);
            }


            // Public Functions
            public void Close() { }
            public bool EOF { get { return _eof; } }
            public long Length { get { return _length; } }
            public long Position { get { return _address; } }

            public bool SeekTo(long address)
            {
                // Check if seek is valid
                if (IsValidXboxMemoryAddress(address))
                {
                    _address = address;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            public void Skip(long count) { _address += count; }
        }
    }
}