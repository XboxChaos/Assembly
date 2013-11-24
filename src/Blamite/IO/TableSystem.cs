using System;
using System.Collections.Generic;
using System.IO;

namespace Blamite.IO
{
	public class TableSystem
	{
		public enum OpenType
		{
			LoadTables,
			CreateTables
		}

		/// <summary>
		///     Stream that contains the TableSystem
		/// </summary>
		private readonly EndianStream _stream;

		/// <summary>
		///     The description used in the Global File Header
		/// </summary>
		private string _desc;

		/// <summary>
		///     Intelligent List that contains all the headers
		/// </summary>
		private IList<TableHeader> _tables = new List<TableHeader>();

		/// <summary>
		///     Creates a new Instance of the TableSystem.
		/// </summary>
		/// <param name="input">Input stream to write/save the TableSystem to.</param>
		/// <param name="loadTables">Load the tables after loading into stream (defaults to false).</param>
		public TableSystem(Stream input, bool loadTables = false)
		{
			_stream = new EndianStream(input, Endian.BigEndian);

			if (loadTables)
				LoadTables();
		}

		/// <summary>
		///     Creates a new Instance of the TableSystem.
		/// </summary>
		/// <param name="input">Input stream to write/save the TableSystem to.</param>
		/// <param name="loadTables">Load the tables after loading into stream (defaults to false).</param>
		public TableSystem(string filePath, bool loadTables = false)
		{
			_stream = new EndianStream(new FileStream(filePath, FileMode.OpenOrCreate), Endian.BigEndian);

			if (loadTables)
				LoadTables();
		}

		/// <summary>
		///     Intelligent List that contains all the tables
		/// </summary>
		public IList<TableHeader> Tables
		{
			get { return _tables; }
			set { _tables = value; }
		}

		/// <summary>
		///     Stream that contains the TableSystem
		/// </summary>
		public EndianStream Stream
		{
			get { return _stream; }
		}

		/// <summary>
		///     The description used in the Global File Header
		/// </summary>
		public string Description
		{
			get { return _desc; }
			set { _desc = value; }
		}

		/// <summary>
		///     Create a new table, and add the entires.
		/// </summary>
		/// <param name="name">Table Name.</param>
		/// <param name="entrylength">Length of each entry in the table.</param>
		/// <param name="count">Number of entries in the table.</param>
		/// <param name="maxcount">Max number of entries in the table.</param>
		/// <param name="entries">IList of entries</param>
		public void CreateTable(string name, UInt32 entrylength, UInt32 count, UInt32 maxcount, IList<byte[]> entries)
		{
			_tables.Add(new TableHeader
			{
				Name = name,
				EntryLength = entrylength,
				Count = count,
				MaxCount = maxcount,
				Entries = entries
			});

			ConstructTables();
		}

		/// <summary>
		///     Create a new table, and add the entires.
		/// </summary>
		/// <param name="name">Table Name.</param>
		/// <param name="entrylength">Length of each entry in the table.</param>
		/// <param name="count">Number of entries in the table.</param>
		/// <param name="maxcount">Max number of entries in the table.</param>
		/// <param name="entries">Single Entry</param>
		public void CreateTable(string name, UInt32 entrylength, UInt32 count, UInt32 maxcount, byte[] entry)
		{
			IList<byte[]> entryIList = new List<byte[]> {entry};

			CreateTable(name, entrylength, count, maxcount, entryIList);
		}

		/// <summary>
		///     Load the container into memory
		/// </summary>
		public void LoadTables()
		{
			_tables.Clear();

			// Verify Header
			_stream.SeekTo(0);
			if (_stream.ReadByte() != 0xDE || _stream.ReadByte() != 0xAD ||
			    _stream.ReadByte() != 0xBE || _stream.ReadByte() != 0xEF)
				throw new Exception("Invalid Package\n\nThe Table is corrupt, the magic doesn't match. fool.");

			// Read Description
			_stream.SeekTo(0x20);
			_desc = _stream.ReadUTF16();

			// Read Allocation Tables
			_stream.SeekTo(0x250 + 0x36);
			IList<Int64> allocationEntries = new List<Int64>();
			UInt32 allocationEntryCount = _stream.ReadUInt32();
			_stream.SeekTo(0x250 + 0x42);
			for (int i = 0; i < allocationEntryCount; i++)
				allocationEntries.Add(_stream.ReadInt64());

			// Read all Tables
			foreach (Int64 tableAllocation in allocationEntries)
			{
				// Seek to table Start
				_stream.SeekTo(tableAllocation);

				// Construct new Table
				var header = new TableHeader();
				header.Entries = new List<byte[]>();

				// Read Table Data
				header.Name = _stream.ReadUTF16();
				_stream.SeekTo(tableAllocation + 0x32);

				header.EntryLength = _stream.ReadUInt32();
				header.Count = _stream.ReadUInt32();
				header.MaxCount = _stream.ReadUInt32();
				header.HeaderSize = _stream.ReadUInt32();

				// Start of Table Entries
				Int64 tableEntriesStart = tableAllocation + header.HeaderSize;

				// Loop Though Entries
				for (int i = 0; i < header.Count; i++)
				{
					// Seek to Table Entry Start
					_stream.SeekTo(tableEntriesStart + (header.EntryLength*i));

					// Read the entry
					var entry = new byte[header.EntryLength];
					_stream.ReadBlock(entry, 0, entry.Length);

					// Add the byte data to the table
					header.Entries.Add(entry);
				}

				// Add Table to IList
				_tables.Add(header);
			}
		}

		/// <summary>
		///     Write the container to the EndianStream
		/// </summary>
		public void ConstructTables()
		{
			// Calculate Length
			long strLength = 0x400;
			foreach (TableHeader table in _tables)
				strLength += (table.Count*table.EntryLength) + table.TablePadding + table.HeaderSize;

			// Change stream length
			_stream.BaseStream.SetLength(0);
			_stream.BaseStream.SetLength(strLength);

			// Construct Header

			#region header

			// Write Magic
			_stream.SeekTo(0);
			_stream.WriteBlock(new byte[] {0xDE, 0xAD, 0xBE, 0xEF});

			// Write Build Data
			_stream.SeekTo(0x20);
			_stream.WriteUTF16(_desc);

			#endregion

			// Write Tables

			#region tableIO

			_stream.SeekTo(0x400);
			long tableStart = 0x400;
			IList<long> TableStarts = new List<long>();

			foreach (TableHeader table in _tables)
			{
				TableStarts.Add(tableStart);
				_stream.SeekTo(tableStart);

				// Write Name
				_stream.WriteUTF16(table.Name);
				_stream.SeekTo(tableStart + 0x32);

				// Write EntryLength
				_stream.WriteUInt32(table.EntryLength);

				// Write EntryCount
				_stream.WriteUInt32(table.Count);

				// Write MaxCount
				_stream.WriteUInt32(table.MaxCount);

				// Write HeaderSize
				_stream.WriteUInt32(table.HeaderSize);

				foreach (var entry in table.Entries)
				{
					// Write Table Entry
					_stream.WriteBlock(entry);
				}

				tableStart = _stream.BaseStream.Position + table.TablePadding;
			}

			#endregion

			// Construct Allocation Table

			#region allocation_table

			_stream.SeekTo(0x250);
			tableStart = 0x250;
			// Write Table Header
			{
				// Write Name
				_stream.WriteUTF16("TableAllocation");
				_stream.SeekTo(tableStart + 0x32);

				// Write EntryLength
				_stream.WriteUInt32(0x04);

				// Write EntryCount
				_stream.WriteUInt32((UInt32) TableStarts.Count);

				// Write MaxCount
				_stream.WriteUInt32(UInt32.MaxValue);

				// Write HeaderSize
				_stream.WriteUInt32(0x42);
			}
			// Write Entries
			_stream.SeekTo(0x250 + 0x42);
			foreach (long tbs in TableStarts)
				_stream.WriteInt64(tbs);

			#endregion
		}

		/// <summary>
		///     Table Format
		/// </summary>
		public class TableHeader
		{
			public UInt32 HeaderSize = 0x42;
			public int TablePadding = 0x100;
			public string Name { get; set; }
			public UInt32 EntryLength { get; set; }
			public UInt32 Count { get; set; }
			public UInt32 MaxCount { get; set; }

			public IList<byte[]> Entries { get; set; }
		}
	}
}