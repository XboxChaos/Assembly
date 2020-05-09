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
		public override IStream GetMetaStream(ICacheFile cacheFile = null)
		{
			if (!CheckBuildInfo())
				return null;

			Process gameProcess = FindGameProcess();
			if (gameProcess == null)
				return null;

			long pointer = RetrievePointer(gameProcess);

			var gameMemory = new ProcessModuleMemoryStream(gameProcess, _buildInfo.GameModule);

			if (gameMemory.BaseModule == null)
				return null;

			long metaMagic = 0;

			using (EndianReader er = new EndianReader(gameMemory, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian))
			{
				er.SeekTo((long)gameMemory.BaseModule.BaseAddress + pointer);

				long point = er.ReadInt64();

				if (point == 0)
					return null;

				er.SeekTo(point + 0x10);

				if (er.ReadUInt32() != MapHeaderMagic)
					return null;

				er.SeekTo(point + _buildInfo.HeaderSize + _buildInfo.PokingOffset);

				metaMagic = er.ReadInt64();
			}

			if (metaMagic == 0)
				return null;

			var metaStream = new OffsetStream(gameMemory, metaMagic);
			return new EndianStream(metaStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
		}

		private static readonly int MapHeaderMagic = CharConstant.FromString("head");

	}
}
