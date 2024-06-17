using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Blamite.RTE.Eldorado
{
	public class EldoradoRTEProvider : IRTEProvider
	{
		private readonly EngineDescription _buildInfo;

		/// <summary>
		///     Constructs a new EldoradoRTEProvider.
		/// </summary>
		public EldoradoRTEProvider(EngineDescription engine)
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
		public IStream GetMetaStream(ICacheFile cacheFile, ITag tag)
		{
			if (string.IsNullOrEmpty(_buildInfo.PokingExecutable))
				throw new InvalidOperationException("No gameExecutable value found in Engines.xml for engine " + _buildInfo.Name + ".");
			if (_buildInfo.Poking == null)
				throw new InvalidOperationException("No poking definitions found in Engines.xml for engine " + _buildInfo.Name + ".");

			Process gameProcess = FindGameProcess();
			if (gameProcess == null)
				return null;

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
				throw new ArgumentNullException("No poking definition found for build version " + _buildInfo.BuildVersion + ". You may need to suppy extra version info.");

			if (!info.LastTagIndexAddress.HasValue)
				throw new ArgumentNullException("Halo Online poking requires a LastTagIndex value.");
			if (!info.IndexArrayPointer.HasValue)
				throw new ArgumentNullException("Halo Online poking requires a IndexArrayPointer value.");
			if (!info.AddressArrayPointer.HasValue)
				throw new ArgumentNullException("Halo Online poking requires a AddressArrayPointer value.");

			var tagAddress = GetTagAddress(gameMemory, tag.Index.Index, info);
			if (tagAddress == 0)
			{
				gameMemory.Close();
				return null;
			}
			var offsetStream = new OffsetStream(gameMemory, tagAddress - tag.MetaLocation.BaseGroup.BasePointer);//gross lol
			return new EndianStream(offsetStream, Endian.LittleEndian);
		}

		protected Process FindGameProcess()
		{
			Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_buildInfo.PokingExecutable));

			if (processes.Length > 0)
				return processes[0];

			if (!string.IsNullOrEmpty(_buildInfo.PokingExecutableAlt))
			{
				processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_buildInfo.PokingExecutableAlt));
				return processes.Length > 0 ? processes[0] : null;
			}
			else
				return null;
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

		public long GetTagAddress(ProcessMemoryStream processStream, int index, PokingInformation info)
		{
			long baseAddress = (long)processStream.BaseProcess.MainModule.BaseAddress;
			long lastTag = baseAddress + info.LastTagIndexAddress.Value;
			long indexArray = baseAddress + info.IndexArrayPointer.Value;
			long addressArray = baseAddress + info.AddressArrayPointer.Value;

			using (EndianReader reader = new EndianReader(processStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian))
			{
				reader.SeekTo(lastTag);
				var maxIndex = reader.ReadUInt16();
				if (index >= maxIndex)
					return 0;

				reader.SeekTo(indexArray);
				var indexTableAddress = reader.ReadUInt32();
				if (indexTableAddress == 0)
					return 0;

				reader.SeekTo(indexTableAddress + index * 4);
				var addressIndex = reader.ReadInt32();
				if (addressIndex < 0)
					return 0;

				reader.SeekTo(addressArray);
				var addressTableAddress = reader.ReadUInt32();
				if (addressTableAddress == 0)
					return 0;
				reader.SeekTo(addressTableAddress + addressIndex * 4);
				return reader.ReadUInt32();
			}
		}
	}
}
