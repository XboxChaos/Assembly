using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Serialization;
using System;
using System.Diagnostics;
using System.IO;

namespace Blamite.RTE.SecondGen
{
	public class SecondGenRTEProvider : IRTEProvider
	{
		private readonly EngineDescription _buildInfo;

		/// <summary>
		///     Constructs a new SecondGenRTEProvider.
		/// </summary>
		/// <param name="exeName">The name of the executable to connect to.</param>
		public SecondGenRTEProvider(EngineDescription engine)
		{
			_buildInfo = engine;
		}

		/// <summary>
		///     The type of connection that the provider will establish.
		///     Always RTEConnectionType.LocalProcess.
		/// </summary>
		public RTEConnectionType ConnectionType
		{
			get { return RTEConnectionType.LocalProcess32; }
		}

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise.</returns>
		public IStream GetMetaStream(ICacheFile cacheFile)
		{
			if (string.IsNullOrEmpty(_buildInfo.PokingExecutable))
				throw new InvalidOperationException("No gameExecutable value found in Engines.xml for engine " + _buildInfo.Name + ".");
			if (_buildInfo.Poking == null)
				throw new InvalidOperationException("No poking definitions found in Engines.xml for engine " + _buildInfo.Name + ".");

			Process gameProcess = FindGameProcess();
			if (gameProcess == null)
				return null;

			string version = gameProcess.MainModule.FileVersionInfo.FileVersion;

			PokingInformation info = _buildInfo.Poking.RetrieveInformation(version);
			if (info == null)
				throw new InvalidOperationException("Game version " + version + " does not have poking information defined in the Formats folder.");

			if (!info.HeaderAddress.HasValue)
				throw new NotImplementedException("Second Generation poking requires a HeaderAddress value.");
			if (!info.MagicAddress.HasValue)
				throw new NotImplementedException("Second Generation poking requires a MagicAddress value.");
			if (!info.SharedMagicAddress.HasValue)
				throw new NotImplementedException("Second Generation poking requires a SharedMagicAddress value.");

			var gameMemory = new ProcessMemoryStream(gameProcess);
			var mapInfo = new SecondGenMapPointerReader(gameMemory, _buildInfo, info);

			long metaAddress;
			if (cacheFile.Type != CacheFileType.Shared)
			{
				metaAddress = mapInfo.CurrentCacheAddress;

				// The map isn't shared, so make sure the map names match
				if (mapInfo.MapName != cacheFile.InternalName)
				{
					gameMemory.Close();
					return null;
				}
			}
			else
			{
				metaAddress = mapInfo.SharedCacheAddress;

				// Make sure the shared and current map pointers are different,
				// or that the current map is the shared map
				if (mapInfo.MapType != CacheFileType.Shared && mapInfo.CurrentCacheAddress == mapInfo.SharedCacheAddress)
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
			Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_buildInfo.PokingExecutable));
			return processes.Length > 0 ? processes[0] : null;
		}
	}
}
