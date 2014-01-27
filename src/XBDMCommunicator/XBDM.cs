﻿using System;
using System.IO;
using XDevkit;

namespace XBDMCommunicator
{
	public class Xbdm
	{
		public enum RebootType
		{
			Cold,
			Title,
			ActiveTitle
		}

		private readonly XboxMemoryStream _xboxMemoryStream;
		private uint _xboxConnectionCode;
		private XboxConsole _xboxConsole;
		private IXboxDebugTarget _xboxDebugTarget;
		private IXboxManager _xboxManager;

		/// <summary>
		///     Create a new instance of the XBDM Communicator
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
		///     Update the Xbox Device Ident (XDK Name or IP)
		/// </summary>
		/// <param name="deviceIdent">The new XBox XDK Name or IP</param>
		public void UpdateDeviceIdent(string deviceIdent)
		{
			if (DeviceIdent != deviceIdent)
				Disconnect();
			DeviceIdent = deviceIdent;
		}

		/// <summary>
		///     Open a connection to the XBox Console
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

				try
				{
					XboxType = _xboxConsole.ConsoleType.ToString();
				}
				catch
				{
					XboxType = "Unable to get.";
				}

				IsConnected = true;
			}
			return true;
		}

		/// <summary>
		///     Close the connection to the XBox Console
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
		///     Send a string-based command, such as "bye", "reboot", "go", "stop"
		/// </summary>
		/// <param name="command">The command to send.</param>
		/// <returns>The responce from the console, or null if sending the command failed.</returns>
		public string SendStringCommand(string command)
		{
			if (!Connect())
				return null;
			try
			{
				string response;
				_xboxConsole.SendTextCommand(_xboxConnectionCode, command, out response);

				if (response == null || (!response.StartsWith("202") && !response.StartsWith("203"))) return response;

				while (true)
				{
					string tempResponseHolder;
					_xboxConsole.ReceiveSocketLine(_xboxConnectionCode, out tempResponseHolder);

					if (String.IsNullOrEmpty(tempResponseHolder)) continue;

					if (tempResponseHolder[0] == '.') break;

					response += Environment.NewLine + tempResponseHolder;
				}

				return response;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		///     Freeze the XBox Console
		/// </summary>
		public void Freeze()
		{
			if (!Connect())
				return;

			SendStringCommand("stop");
		}

		/// <summary>
		///     UnFreeze the XBox Console
		/// </summary>
		public void Unfreeze()
		{
			if (!Connect())
				return;

			SendStringCommand("go");
		}

		/// <summary>
		///     Reboot the XBox Console
		/// </summary>
		/// <param name="rebootType">The type of Reboot to do (Cold or Title)</param>
		public void Reboot(RebootType rebootType)
		{
			if (!Connect())
				return;

			switch (rebootType)
			{
				case RebootType.Cold:
					SendStringCommand("magicboot COLD");
					break;
				case RebootType.Title:
					SendStringCommand("magicboot");
					break;
				case RebootType.ActiveTitle:
					var xbePath = _xboxConsole.RunningProcessInfo.ProgramName;
					SendStringCommand(string.Format("magicboot title=\"{0}\" directory=\"{1}\"", xbePath, GetDirectoryOfXbe(xbePath)));
					break;
			}
		}

		/// <summary>
		///     Shutdown the XBox Console
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
		///     Save a screenshot from the XBox Console
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

		// Helpers
		#region Helpers

		private static string GetDataFromMultiLine(string data, string fieldNeeded)
		{
			string extractedData = null;
			var splitData = data.Split(new [] {"\r\n"}, StringSplitOptions.None);
			foreach (var line in splitData)
			{
				var index = line.IndexOf(fieldNeeded, StringComparison.Ordinal);
				if (index == -1)
					continue;

				// get got data, read it.
				extractedData = line.Remove(0, index);
				extractedData = extractedData.Remove(0, string.Format("{0}=", fieldNeeded).Length);
				if (extractedData.StartsWith("\"")) // Is a string
				{
					extractedData = extractedData.Remove(0, 1);
					var closingIndex = extractedData.IndexOf('"');
					extractedData = extractedData.Remove(closingIndex);
				}
				else // isn't
				{
					var closingIndex = extractedData.IndexOf(' ');
					extractedData = extractedData.Remove(closingIndex);
				}
			}
			return extractedData;
		}

		private static string GetDirectoryOfXbe(string xbePath)
		{
			var indexOfDirectory = xbePath.LastIndexOf("\\", StringComparison.Ordinal);

			return indexOfDirectory == -1 ? xbePath : xbePath.Remove(indexOfDirectory);
		}

		#endregion

		// Enum Declaration

		// Memory IO
		public class XboxMemoryStream : Stream
		{
			// Private Modifiers
			private readonly Xbdm _xbdm;

			public XboxMemoryStream(Xbdm xbdm)
			{
				_xbdm = xbdm;
				Position = 0;
			}

			// IO Functions
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

			public override sealed long Position { get; set; }

			public override void Flush()
			{
				// TODO: Make a more efficient flush
				_xbdm.Disconnect();
				_xbdm.Connect();
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (!_xbdm.Connect())
					return 0;

				bool alreadyStopped = true;
				if (count > 20)
					_xbdm._xboxDebugTarget.Stop(out alreadyStopped);

				uint bytesRead;
				if (offset == 0)
				{
					_xbdm._xboxDebugTarget.GetMemory((uint) Position, (uint) count, buffer, out bytesRead);
				}
				else
				{
					// Offset isn't 0, so read into a temp buffer and then copy it into the output
					var tempBuffer = new byte[count];
					_xbdm._xboxDebugTarget.GetMemory((uint) Position, (uint) count, tempBuffer, out bytesRead);
					Buffer.BlockCopy(tempBuffer, 0, buffer, offset, count);
				}
				Position += bytesRead;

				if (!alreadyStopped)
					_xbdm._xboxDebugTarget.Go(out alreadyStopped);

				return (int) bytesRead;
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
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				if (!_xbdm.Connect())
					return;

				bool alreadyStopped = true;
				if (count > 20)
					_xbdm._xboxDebugTarget.Stop(out alreadyStopped);

				byte[] pokeArray = buffer;
				if (offset != 0)
				{
					// Offset isn't 0, so copy into a second buffer before poking
					pokeArray = new byte[count];
					Buffer.BlockCopy(buffer, offset, pokeArray, 0, count);
				}

				uint bytesWritten;
				_xbdm._xboxDebugTarget.SetMemory((uint) Position, (uint) count, pokeArray, out bytesWritten);
				Position += bytesWritten;

				if (!alreadyStopped)
					_xbdm._xboxDebugTarget.Go(out alreadyStopped);
			}
		}
	}
}