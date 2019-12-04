using System;
using System.Diagnostics;
using System.IO;
using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Util;
using System.Collections.Generic;
using Blamite.Serialization;

namespace Blamite.RTE.MCC
{
	public class MCCRTEProvider : IRTEProvider
	{
		private readonly EngineDescription _buildInfo;

		/// <summary>
		///     Constructs a new MCCRTEProvider.
		/// </summary>
		/// <param name="exeName">The name of the executable to connect to.</param>
		/// <param name="moduleName">The name of the executable module which hosts the game engine.</param>
		public MCCRTEProvider(EngineDescription engine)
		{
			_buildInfo = engine;
		}

		/// <summary>
		///     The type of connection that the provider will establish.
		///     Always RTEConnectionType.LocalProcess.
		/// </summary>
		public RTEConnectionType ConnectionType
		{
			get { return RTEConnectionType.LocalProcess; }
		}

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise.</returns>
		public IStream GetMetaStream(ICacheFile cacheFile = null)
		{
			if (string.IsNullOrEmpty(_buildInfo.GameExecutable))
				throw new InvalidOperationException("No gameExecutable value found in Engines.xml for engine " + _buildInfo.Name + ".");
			if (string.IsNullOrEmpty(_buildInfo.GameModule))
				throw new InvalidOperationException("No gameModule value found in Engines.xml for engine " + _buildInfo.Name + ".");
			if (_buildInfo.Poking == null)
				throw new InvalidOperationException("No poking definitions found in Engines.xml for engine " + _buildInfo.Name + ".");

			Process gameProcess = FindGameProcess();
			if (gameProcess == null)
				return null;

#if X86
			throw new InvalidOperationException("Cannot access a 64bit process with a 32bit program.");
#else
			//verify version, and check for anticheat at the same time
			string version = "";
			long pointer = -1;
			try
			{
				version = gameProcess.MainModule.FileVersionInfo.FileVersion;

				//TODO: make winstore support not horrible
				if (version == null)
					pointer = _buildInfo.Poking.RetrieveLastPointer();
				else
					pointer = _buildInfo.Poking.RetrievePointer(version);

				if (pointer == -1)
					throw new InvalidOperationException("Game version " + version + " does not have a pointer defined in the Formats folder.");
			}
			catch(System.ComponentModel.Win32Exception)
			{
				throw new InvalidOperationException("Cannot access game process due to Anti-Cheat.");
			}

			var gameMemory = new ProcessModuleMemoryStream(gameProcess, _buildInfo.GameModule);

			if (gameMemory.BaseModule == null)
				return null;

			long metaMagic = 0;

			using (EndianReader er = new EndianReader(gameMemory, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian))
			{
				er.SeekTo(gameMemory.BaseModuleAddress + pointer);

				long point = er.ReadInt64();

				if (point == 0)
					return null;

				er.SeekTo(point + 0x10);

				if (er.ReadUInt32() != MapHeaderMagic)
					return null;

				er.SeekTo(point + MemMagicOffset);

				metaMagic = er.ReadInt64();
			}

			if (metaMagic == 0)
				return null;

			var metaStream = new OffsetStream(gameMemory, metaMagic);
			return new EndianStream(metaStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
#endif


		}

		private Process FindGameProcess()
		{
			Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_buildInfo.GameExecutable));

			if (processes.Length > 0)
				return processes[0];

			if (!string.IsNullOrEmpty(_buildInfo.GameExecutableAlt))
			{
				processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_buildInfo.GameExecutableAlt));
				return processes.Length > 0 ? processes[0] : null;
			}
			else
				return null;
		}

		private const int MemMagicOffset = 0xA0A0;//likely needs adjusting for future games

		private static readonly int MapHeaderMagic = CharConstant.FromString("head");
	}
}
