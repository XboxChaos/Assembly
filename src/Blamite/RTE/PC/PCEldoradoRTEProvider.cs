using Blamite.Blam;
using Blamite.IO;
using Blamite.RTE.PC.Native;
using Blamite.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Blamite.RTE.PC
{
	public class PCEldoradoRTEProvider : PCRTEProvider
	{
		protected long _lastTag;
		protected long _indexArray;
		protected long _addressArray;

		/// <summary>
		///     Constructs a new PCEldoradoRTEProvider.
		/// </summary>
		public PCEldoradoRTEProvider(EngineDescription engine) : base(engine)
		{
		}

		/// <summary>
		///     The type of connection that the provider will establish.
		///     Always RTEConnectionType.LocalProcess.
		/// </summary>
		public override RTEConnectionType ConnectionType
		{
			get { return _buildInfo.PokingPlatform; }
		}

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise.</returns>
		public override IStream GetCacheStream(ICacheFile cacheFile, ITag tag)
		{
			if (!CheckBuildInfo())
				return null; //ErrorMessage was handled by above.

			Process gameProcess = FindGameProcess();
			if (gameProcess == null)
				return null; //ErrorMessage was handled by above.

			var gameMemory = new ProcessMemoryStream(gameProcess);

			PokingInformation info;
			if (gameProcess.MainModule.FileVersionInfo?.ProductVersion != null)
			{
				string version = gameProcess.MainModule.FileVersionInfo.ProductVersion;
				info = _buildInfo.Poking.RetrieveInformation(version);
			}
			else
				info = FindBuild(gameMemory, _buildInfo.Poking.GetVersions());

			if (info == null)
			{
				ErrorMessage = "Game version " + _buildInfo.BuildVersion + " does not have poking information defined in the Formats folder.";
				return null;
			}

			if ((!info.LastTagIndexAddress.HasValue || !info.IndexArrayPointer.HasValue || !info.AddressArrayPointer.HasValue))
			{
				ErrorMessage = "Halo Online poking requires a LastTagIndex, IndexArrayPointer, and AddressArrayPointer value.";
				return null;
			}

			_baseAddress = (long)gameMemory.BaseProcess.MainModule.BaseAddress;
			_lastTag = _baseAddress + info.LastTagIndexAddress.Value;
			_indexArray = _baseAddress + info.IndexArrayPointer.Value;
			_addressArray = _baseAddress + info.AddressArrayPointer.Value;

			var reader = new EndianReader(gameMemory, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);

			var tagAddress = ReadTagPointer(reader, tag.Index.Index);
			if (tagAddress == 0)
			{
				gameMemory.Close();
				ErrorMessage = "Could not get the memory address for the given tag.";
				return null;
			}

			OffsetStream gameStream = new OffsetStream(gameMemory, tagAddress - tag.MetaLocation.BaseGroup.BasePointer);//gross lol
			return new EndianStream(gameStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
		}

		public PokingInformation FindBuild(ProcessMemoryStream processStream, List<PokingInformation> collection)
		{
			long baseAddress = (long)processStream.BaseProcess.MainModule.BaseAddress;

			using (EndianReader reader = new EndianReader(processStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian))
			{
				foreach (PokingInformation info in collection)
				{
					if (info.VersionString == null || !info.VersionAddress.HasValue)
						continue;

					long buildAddress = baseAddress + info.VersionAddress.Value;

					reader.SeekTo(buildAddress);
					string build = reader.ReadAscii(info.VersionString.Length);

					if (build == info.VersionString)
						return info;
				}
			}

			return null;
		}

		public long ReadTagPointer(IReader reader, int index)
		{
			reader.SeekTo(_lastTag);
			var maxIndex = reader.ReadUInt16();
			if (index >= maxIndex)
				return 0;

			reader.SeekTo(_indexArray);
			var indexTableAddress = reader.ReadUInt32();
			if (indexTableAddress == 0)
				return 0;

			reader.SeekTo(indexTableAddress + index * 4);
			var addressIndex = reader.ReadInt32();
			if (addressIndex < 0)
				return 0;

			reader.SeekTo(_addressArray);
			var addressTableAddress = reader.ReadUInt32();
			if (addressTableAddress == 0)
				return 0;
			reader.SeekTo(addressTableAddress + addressIndex * 4);
			return reader.ReadUInt32();
		}

		protected override void ReadMapPointers32(IReader reader)
		{ }

		protected override void ReadMapPointers64(IReader reader)
		{ }
	}
}
