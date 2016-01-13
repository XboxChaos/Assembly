using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;

namespace Blamite.RTE.Eldorado
{
	/// <summary>
	/// A real-time editing provider which connects to Halo Online.
	/// </summary>
	public class EldoradoRTEProvider : IRTEProvider
	{
		// TODO: Maybe find a way to dynamically get these addresses so we can support other versions of the game
		public static uint MaxTagCountAddress;
		public static uint TagIndexArrayPointerAddress;
		public static uint TagAddressArrayPointerAddress;

		/// <summary>
		/// Initializes a new instance of the <see cref="EldoradoRTEProvider"/> class.
		/// </summary>
		/// <param name="exeName">The name of the executable to connect to.</param>
		public EldoradoRTEProvider(string exeName)
		{
			EXEName = exeName;

			MaxTagCountAddress = 0x22AB008;
			TagIndexArrayPointerAddress = 0x22AAFFC;
			TagAddressArrayPointerAddress = 0x22AAFF8;
		}

		/// <summary>
		/// The type of connection that the provider will establish.
		/// </summary>
		public RTEConnectionType ConnectionType
		{
			get { return RTEConnectionType.LocalProcess; }
		}

		/// <summary>
		/// Gets or sets the name of the executable to connect to.
		/// </summary>
		public string EXEName { get; set; }

		/// <summary>
		/// Obtains a stream which can be used to read and write a cache file's meta in realtime.
		/// The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <param name="tag">The tag to get a stream for.</param>
		/// <returns>
		/// The stream if it was opened successfully, or null otherwise.
		/// </returns>
		public IStream GetMetaStream(ICacheFile cacheFile, ITag tag)
		{
			// Open a handle to the game process
			var gameProcess = FindGameProcess();
			if (gameProcess == null)
				return null;
			var memoryStream = new ProcessMemoryStream(gameProcess);

			var exeBase = (uint)memoryStream.BaseProcess.MainModule.BaseAddress - 0x400000;

			var tagAddress = GetTagAddress(new EndianReader(memoryStream, Endian.LittleEndian), tag.Index.Index, exeBase);
			if (tagAddress == 0)
			{
				memoryStream.Close();
				return null;
			}
			var offsetStream = new OffsetStream(memoryStream, tagAddress - tag.HeaderLocation.AsOffset());
			return new EndianStream(offsetStream, Endian.LittleEndian);
		}

		private Process FindGameProcess()
		{
			var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(EXEName));
			return processes.Length > 0 ? processes[0] : null;
		}

		public static uint GetTagAddress(IReader memoryReader, ushort index, uint exeBase)
		{
			//uint newcountaddr = MaxTagCountAddress;
			// Read the tag count and validate the tag index
			memoryReader.SeekTo(MaxTagCountAddress + exeBase);
			var maxIndex = memoryReader.ReadUInt16();
			if (index >= maxIndex)
				return 0;

			// Read the tag index table to get the index of the tag in the address table
			memoryReader.SeekTo(TagIndexArrayPointerAddress + exeBase);
			var tagIndexTableAddress = memoryReader.ReadUInt32();
			if (tagIndexTableAddress == 0)
				return 0;
			memoryReader.SeekTo(tagIndexTableAddress + index * 4);
			var addressIndex = memoryReader.ReadInt32();
			if (addressIndex < 0)
				return 0;

			// Read the tag's address in the address table
			memoryReader.SeekTo(TagAddressArrayPointerAddress + exeBase);
			var tagAddressTableAddress = memoryReader.ReadUInt32();
			if (tagAddressTableAddress == 0)
				return 0;
			memoryReader.SeekTo(tagAddressTableAddress + addressIndex * 4);
			return memoryReader.ReadUInt32();
		}
	}

	public class ZBTRTEProvider : EldoradoRTEProvider
	{
		public ZBTRTEProvider(string exeName) : base(exeName)
		{
			MaxTagCountAddress = 0x42D68E8;
			TagIndexArrayPointerAddress = 0x42D68DC;
			TagAddressArrayPointerAddress = 0x42D68D8;
		}
	}

	public class ZBT70RTEProvider : EldoradoRTEProvider
	{
		public ZBT70RTEProvider(string exeName) : base(exeName)
		{
			MaxTagCountAddress = 0x3010B90;
			TagIndexArrayPointerAddress = 0x4503F6;
			TagAddressArrayPointerAddress = 0x450406;
		}
	}
}
