using System;
using System.Diagnostics;
using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Util;
using Blamite.Serialization;

namespace Blamite.RTE.ThirdGen
{
	public class ThirdGenMCCRTEProvider : MCCRTEProvider
	{
		/// <summary>
		///     Constructs a new ThirdGenMCCRTEProvider.
		/// </summary>
		public ThirdGenMCCRTEProvider(EngineDescription engine) : base(engine)
		{ }

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise.</returns>
		public override IStream GetMetaStream(ICacheFile cacheFile = null, ITag tag = null)
		{
			if (!CheckBuildInfo())
				return null;

			Process gameProcess = FindGameProcess();
			if (gameProcess == null)
				return null;

			PokingInformation info = RetrieveInformation(gameProcess);

			if (!info.HeaderPointer.HasValue && (!info.HeaderAddress.HasValue || !info.MagicAddress.HasValue))
				throw new NotImplementedException("Poking information is missing required values.");

			var gameMemory = new ProcessModuleMemoryStream(gameProcess, _buildInfo.PokingModule);
			var mapInfo = new ThirdGenMapPointerReader(gameMemory, _buildInfo, info);

			long metaMagic = mapInfo.CurrentCacheAddress;

			if (gameMemory.BaseModule == null)
				return null;

			if (cacheFile != null && mapInfo.MapName != cacheFile.InternalName)
			{
				gameMemory.Close();
				return null;
			}
			
			if (metaMagic == 0)
				return null;

			var metaStream = new OffsetStream(gameMemory, metaMagic);
			return new EndianStream(metaStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
		}
	}
}
