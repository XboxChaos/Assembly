using System;
using System.Diagnostics;
using System.IO;
using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;

namespace Blamite.RTE.H2Vista
{
	/// <summary>
	///     A real-time editing provider which connects to Halo 2 Vista.
	/// </summary>
	public class H2VistaRteProvider : IRteProvider
	{
		/// <summary>
		///     Constructs a new H2VistaRTEProvider.
		/// </summary>
		/// <param name="exeName">The name of the executable to connect to.</param>
		public H2VistaRteProvider(string exeName)
		{
			ExeName = exeName;
		}

		/// <summary>
		///     Gets or sets the name of the executable to connect to.
		/// </summary>
		public string ExeName { get; set; }

		/// <summary>
		///     The type of connection that the provider will establish.
		///     Always RTEConnectionType.LocalProcess.
		/// </summary>
		public RteConnectionType ConnectionType
		{
			get { return RteConnectionType.LocalProcess; }
		}

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise.</returns>
		public IStream GetMetaStream(ICacheFile cacheFile)
		{
			var gameProcess = FindGameProcess();
			if (gameProcess == null)
				return null;

			var gameMemory = new ProcessMemoryStream(gameProcess);
			var mapInfo = new H2VistaMapPointerReader(gameMemory);

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
			var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ExeName));
			return processes.Length > 0 ? processes[0] : null;
		}
	}
}