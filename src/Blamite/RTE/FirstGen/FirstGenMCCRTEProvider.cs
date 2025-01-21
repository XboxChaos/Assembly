using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Serialization;
using System;
using System.Diagnostics;

namespace Blamite.RTE.FirstGen
{
	public class FirstGenMCCRTEProvider : MCCRTEProvider
	{
		/// <summary>
		///     Constructs a new FirstGenMCCRTEProvider.
		/// </summary>
		public FirstGenMCCRTEProvider(EngineDescription engine) : base(engine)
		{ }

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise.</returns>
		public override IStream GetMetaStream(ICacheFile cacheFile, ITag tag = null)
		{
			if (!CheckBuildInfo())
				return null;

			Process gameProcess = FindGameProcess();
			if (gameProcess == null)
				return null;

			PokingInformation info = RetrieveInformation(gameProcess);

			if (!info.HeaderAddress.HasValue)
				throw new NotImplementedException("First Generation poking requires a HeaderAddress value.");
			if (!info.MagicAddress.HasValue)
				throw new NotImplementedException("First Generation poking requires a MagicAddress value.");

			var gameMemory = new ProcessModuleMemoryStream(gameProcess, _buildInfo.PokingModule);
			var mapInfo = new FirstGenMapPointerReader(gameMemory, _buildInfo, info);

			long metaAddress = mapInfo.CurrentCacheAddress;
			if (mapInfo.MapName != cacheFile.InternalName)
			{
				gameMemory.Close();
				return null;
			}

			var metaStream = new OffsetStream(gameMemory, metaAddress - cacheFile.MetaArea.BasePointer);
			return new EndianStream(metaStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
		}
	}
}
