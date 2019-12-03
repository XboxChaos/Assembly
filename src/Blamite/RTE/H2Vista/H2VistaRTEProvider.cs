#if NET45

using System;
using System.Diagnostics;
using System.IO;
using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Serialization;

namespace Blamite.RTE.H2Vista
{
	/// <summary>
	///     A real-time editing provider which connects to Halo 2 Vista.
	/// </summary>
	public class H2VistaRTEProvider : IRTEProvider
	{
		private readonly EngineDescription _buildInfo;

		/// <summary>
		///     Constructs a new H2VistaRTEProvider.
		/// </summary>
		/// <param name="exeName">The name of the executable to connect to.</param>
		public H2VistaRTEProvider(EngineDescription engine)
		{
			_buildInfo = engine;
			ExecutableName = _buildInfo.GameExecutable;
		}

		/// <summary>
		///     Gets or sets the name of the executable to connect to.
		/// </summary>
		public string ExecutableName { get; set; }

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
		public IStream GetMetaStream(ICacheFile cacheFile)
		{
			if (string.IsNullOrEmpty(ExecutableName))
				throw new InvalidOperationException("No gameExecutable value found in Engines.xml for engine " + _buildInfo.Name + ".");
			if (_buildInfo.Poking == null)
				throw new InvalidOperationException("No poking definitions found in Engines.xml for engine " + _buildInfo.Name + ".");

			Process gameProcess = FindGameProcess();
			if (gameProcess == null)
				return null;

			string version = gameProcess.MainModule.FileVersionInfo.FileVersion;
			long pointer = _buildInfo.Poking.RetrievePointer(version);
			if (pointer == -1)
				throw new InvalidOperationException("Game version " + version + " does not have a pointer defined in the Formats folder.");

			var gameMemory = new ProcessMemoryStream(gameProcess);
			var mapInfo = new H2VistaMapPointerReader(gameMemory, pointer);

			long metaAddress;
			if (cacheFile.Type != CacheFileType.Shared)
			{
				metaAddress = mapInfo.CurrentMetaAddress;

				// The map isn't shared, so make sure the map names match
				if (mapInfo.MapName != cacheFile.InternalName)
				{
					gameMemory.Close();
					return null;
				}
			}
			else
			{
				metaAddress = mapInfo.SharedMetaAddress;

				// Make sure the shared and current map pointers are different,
				// or that the current map is the shared map
				if (mapInfo.MapType != CacheFileType.Shared && mapInfo.CurrentMetaAddress == mapInfo.SharedMetaAddress)
				{
					gameMemory.Close();
					return null;
				}
			}

			var metaStream = new OffsetStream(gameMemory, metaAddress - cacheFile.MetaArea.BasePointer);
			return new EndianStream(metaStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
		}

		private Process FindGameProcess()
		{
			Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ExecutableName));
			return processes.Length > 0 ? processes[0] : null;
		}
	}
}

#endif
