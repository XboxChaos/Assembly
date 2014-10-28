using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.BLF
{
	[Flags]
	public enum LevelFlags
	{
		None = 0,
		Unknown0 = 1 << 0,
		Unknown1 = 1 << 1,
		Visible = 1 << 2,
		GeneratesFilm = 1 << 3,
		IsMainMenu = 1 << 4,
		IsCampaign = 1 << 5,
		IsMultiplayer = 1 << 6,
		IsDLC = 1 << 7,
		Unknown8 = 1 << 8,
		Unknown9 = 1 << 9,
		IsFirefight = 1 << 10,
		IsCinematic = 1 << 11,
		IsForgeOnly = 1 << 12,
		Unknown13 = 1 << 13,
		Unknown14 = 1 << 14,
		Unknown15 = 1 << 15,
	}

	public class MapInfo
	{
		// Private Modifiers
		public enum GameIdentifier : uint
		{
			Halo3 = 0x4D500003,
			Halo3ODST = 0x98C00003,
			HaloReachBetas = 0xCC880005,
			HaloReach = 0xCC980007,
			Halo4NetworkTest = 0xCC980008,
			Halo4 = 0x1DD80009
		}

		private MaplevlInfo _mapInformation;
		private EndianStream _stream;
		private int languageCount;
		private int insertionCount;
		private int insertionBaseOffset;
		private int insertionPointSize;
		private int insertionNameOffset;
		private int insertionDescriptionOffset;
		
		public MapInfo(string blfLocation)
		{
			Initalize(new FileStream(blfLocation, FileMode.OpenOrCreate));
		}

		public MapInfo(Stream blfStream)
		{
			Initalize(blfStream);
		}

		public Stream Stream
		{
			get { return _stream.BaseStream; }
		}

		public MaplevlInfo MapInformation
		{
			get { return _mapInformation; }
			set { _mapInformation = value; }
		}

		private void Initalize(Stream blfStream)
		{
			_stream = new EndianStream(blfStream, Endian.BigEndian);

			// Load MapInfo shit
			LoadMapInfo();
		}

		private void UpdateCountsAndOffsets(GameIdentifier gameIdent)
		{
			switch (gameIdent)
			{
				case GameIdentifier.Halo3:
					insertionCount = 4;
					languageCount = 12;
					insertionBaseOffset = 0x1160;
					insertionPointSize = 0xF08;
					insertionNameOffset = 0x8;
					insertionDescriptionOffset = 0x308;
					break;
				case GameIdentifier.Halo3ODST:
					insertionCount = 9;
					languageCount = 12;
					insertionBaseOffset = 0x1160;
					insertionPointSize = 0xF10;
					insertionNameOffset = 0x10;
					insertionDescriptionOffset = 0x310;
					break;
				case GameIdentifier.HaloReach:
				case GameIdentifier.HaloReachBetas:
				case GameIdentifier.Halo4NetworkTest:
					insertionCount = 12;
					languageCount = 12;
					insertionBaseOffset = 0x1258;
					insertionPointSize = 0xF88;
					insertionNameOffset = 0x88;
					insertionDescriptionOffset = 0x388;
					break;
				case GameIdentifier.Halo4:
					insertionCount = 12;
					languageCount = 17;
					insertionBaseOffset = 0x1898;
					insertionPointSize = 0x15C8;
					insertionNameOffset = 0x88;
					insertionDescriptionOffset = 0x4C8;
					break;
				default:
					throw new InvalidOperationException("The MapInfo BLF file is from an unknown Halo Version");
			}
		}

		public void Close()
		{
			_stream.Close();
		}

		#region Loading Code

		public void LoadMapInfo()
		{
			_mapInformation = new MaplevlInfo();

			// Load Game Identification
			_stream.SeekTo(0x36);
			_mapInformation.Game = (GameIdentifier) _stream.ReadUInt32();
			UpdateCountsAndOffsets(_mapInformation.Game);

			// Load MapID
			_stream.SeekTo(0x3C);
			_mapInformation.MapID = _stream.ReadInt32();

			// Load Flags
			_stream.SeekTo(0x42);
			_mapInformation.Flags = (LevelFlags)_stream.ReadInt16();

			// Load Map Names
			LoadMapNames();

			// Load Map Descriptions
			LoadMapDescriptions();

			// Load Map Physical Name
			_stream.SeekTo(_mapInformation.Game == GameIdentifier.Halo4 ? 0x1584 : 0x0F44);
			_mapInformation.PhysicalName = _stream.ReadAscii();

			// Load Map Internal Name
			_stream.SeekTo(_mapInformation.Game == GameIdentifier.Halo4 ? 0x1684 : 0x1044);
			_mapInformation.InternalName = _stream.ReadAscii();

			// Load Map Index
			_stream.SeekTo(_mapInformation.Game == GameIdentifier.Halo4 ? 0x1784 : 0x1144);
			_mapInformation.MapIndex = _stream.ReadInt32();

			// Load Max Teams
			LoadMapMaxTeams();

			// Load Multiplayer Object Table
			LoadMPObjectTable();

			// Load Insertion Points
			LoadInsertionPoints();

			// Load Default Author Name
			LoadDefaultAuthor();
		}

		public void LoadMapNames()
		{
			_mapInformation.MapNames.Clear();

			const int baseOffset = 0x44;
			for (int i = 0; i < languageCount; i++)
			{
				_stream.SeekTo(baseOffset + (i*0x40));
				_mapInformation.MapNames.Add(_stream.ReadUTF16());
			}
		}

		public void LoadMapDescriptions()
		{
			_mapInformation.MapDescriptions.Clear();

			int baseOffset = _mapInformation.Game == GameIdentifier.Halo4 ? 0x0484 : 0x0344;
			for (int i = 0; i < languageCount; i++)
			{
				_stream.SeekTo(baseOffset + (i*0x100));
				_mapInformation.MapDescriptions.Add(_stream.ReadUTF16());
			}
		}

		private void LoadMapMaxTeams()
		{
			if (_mapInformation.Game == GameIdentifier.Halo3 || _mapInformation.Game == GameIdentifier.Halo3ODST)
			{
				_stream.SeekTo(0x114E);
				for (int i = 0; i < 12; i++)
					_mapInformation.MaxTeamCounts.Add(_stream.ReadByte());
			}
		}

		private void LoadMPObjectTable()
		{
			if (_mapInformation.Game != GameIdentifier.Halo3 && _mapInformation.Game != GameIdentifier.Halo3ODST)
			{
				int baseOffset = _mapInformation.Game == GameIdentifier.Halo4 ? 0x1798 : 0x1158;
				var intList = new List<int>();
				for (int i = 0; i < 64; i++)
				{
					_stream.SeekTo(baseOffset + (i * 4));
					intList.Add(_stream.ReadInt32());
				}
				_mapInformation.ObjectTable = new BitArray(intList.ToArray());
			}
		}

		private void LoadInsertionPoints()
		{
			_mapInformation.MapCheckpoints.Clear();
			
			for (int i = 0; i < insertionCount; i++)
			{
				_mapInformation.MapCheckpoints.Add(new Checkpoint());

				_stream.SeekTo(insertionBaseOffset + (i * insertionPointSize));
				int visible = _stream.ReadByte();
				_mapInformation.MapCheckpoints[i].IsVisible = visible == 1;

				if (_mapInformation.Game == GameIdentifier.Halo3 || _mapInformation.Game == GameIdentifier.Halo3ODST)
				{
					_stream.SeekTo(insertionBaseOffset + (i * insertionPointSize) + 3);
					_mapInformation.MapCheckpoints[i].ZoneIndex = _stream.ReadByte();
				}
				else
				{
					_stream.SeekTo(insertionBaseOffset + (i * insertionPointSize) + 1);
					int used = _stream.ReadByte();
					_mapInformation.MapCheckpoints[i].IsUsed = used == 1;

					_stream.SeekTo(insertionBaseOffset + (i * insertionPointSize) + 4);
					_mapInformation.MapCheckpoints[i].ZoneName = _stream.ReadAscii();
				}

				int namesBaseOffset = (insertionBaseOffset + (i * insertionPointSize) + insertionNameOffset);
				for (int n = 0; n < languageCount; n++)
				{
					_stream.SeekTo(namesBaseOffset + (n * 0x40));
					_mapInformation.MapCheckpoints[i].CheckpointName.Add(_stream.ReadUTF16());
				}

				int descriptionsBaseOffset = (insertionBaseOffset + (i * insertionPointSize) + insertionDescriptionOffset);
				for (int d = 0; d < languageCount; d++)
				{
					_stream.SeekTo(descriptionsBaseOffset + (d * 0x100));
					_mapInformation.MapCheckpoints[i].CheckpointDescription.Add(_stream.ReadUTF16());
				}
			}
		}

		private void LoadDefaultAuthor()
		{
			if (_mapInformation.Game == GameIdentifier.HaloReach)
			{
				_stream.SeekTo(0xCCB8);
				_mapInformation.DefaultAuthor = _stream.ReadAscii();
			}

			if (_mapInformation.Game == GameIdentifier.Halo4)
			{
				_stream.SeekTo(0x11DF8);
				_mapInformation.DefaultAuthor = _stream.ReadAscii();
			}
		}

		#endregion

		#region Update Code

		public void UpdateMapInfo()
		{
			// Update Game Identification
			_stream.SeekTo(0x36);
			_stream.WriteUInt32((UInt32) _mapInformation.Game);

			// Update MapID
			_stream.SeekTo(0x3C);
			_stream.WriteInt32(_mapInformation.MapID);

			// Load Flags
			_stream.SeekTo(0x42);
			_stream.WriteInt16((short)_mapInformation.Flags);

			// Update Map Names
			UpdateMapNames();

			// Update Map Descrptions
			UpdateMapDescriptions();

			// Update Map Physical Name
			_stream.SeekTo(_mapInformation.Game == GameIdentifier.Halo4 ? 0x1584 : 0x0F44);
			_stream.WriteAscii(_mapInformation.PhysicalName);

			// Update Map Internal Name
			_stream.SeekTo(_mapInformation.Game == GameIdentifier.Halo4 ? 0x1684 : 0x1044);
			_stream.WriteAscii(_mapInformation.InternalName);

			// Update Map Index
			_stream.SeekTo(_mapInformation.Game == GameIdentifier.Halo4 ? 0x1784 : 0x1144);
			_stream.WriteInt32(_mapInformation.MapIndex);

			// Update Map Max Teams
			UpdateMapMaxTeams();

			// Update Multiplayer Object Table
			UpdateMPObjectTable();

			// Update Insertion Points
			UpdateInsertionPoints();

			// Update Default Author
			UpdateDefaultAuthor();
		}

		public void UpdateMapNames()
		{
			const int baseOffset = 0x44;
			for (int i = 0; i < _mapInformation.MapNames.Count; i++)
			{
				int seekVal = baseOffset + (i*0x40);

				_stream.SeekTo(seekVal);
				_stream.WriteUTF16(_mapInformation.MapNames[i]);
			}
		}

		public void UpdateMapDescriptions()
		{
			int baseOffset = _mapInformation.Game == GameIdentifier.Halo4 ? 0x0484 : 0x0344;
			for (int i = 0; i < _mapInformation.MapDescriptions.Count; i++)
			{
				int seekVal = baseOffset + (i*0x100);

				_stream.SeekTo(seekVal);
				_stream.WriteUTF16(_mapInformation.MapDescriptions[i]);
			}
		}

		private void UpdateMapMaxTeams()
		{
			if (_mapInformation.Game == GameIdentifier.Halo3 || _mapInformation.Game == GameIdentifier.Halo3ODST)
			{
				_stream.SeekTo(0x114E);
				for (int i = 0; i < 12; i++)
					_stream.WriteByte(_mapInformation.MaxTeamCounts[i]);
			}
		}

		private void UpdateMPObjectTable()
		{
			if (_mapInformation.Game != GameIdentifier.Halo3 && _mapInformation.Game != GameIdentifier.Halo3ODST)
			{
				int baseOffset = _mapInformation.Game == GameIdentifier.Halo4 ? 0x1798 : 0x1158;
				for (int i = 0; i < 64; i++)
				{
					int seekVal = baseOffset + (i * 4);
					_stream.SeekTo(seekVal);
					var buffer = new int[1];
					_mapInformation.ObjectTable.CopyTo(buffer, i * 32);
					_stream.WriteInt32(buffer[0]);
				}
			}
		}

		private void UpdateInsertionPoints()
		{
			for (int i = 0; i < insertionCount; i++)
			{
				_stream.SeekTo(insertionBaseOffset + (i * insertionPointSize));
				_stream.WriteByte((byte)(_mapInformation.MapCheckpoints[i].IsVisible ? 0x1 : 0x0));

				if (_mapInformation.Game == GameIdentifier.Halo3 || _mapInformation.Game == GameIdentifier.Halo3ODST)
				{
					_stream.SeekTo(insertionBaseOffset + (i * insertionPointSize) + 3);
					_stream.WriteByte(_mapInformation.MapCheckpoints[i].ZoneIndex);
				}
				else
				{
					_stream.SeekTo(insertionBaseOffset + (i * insertionPointSize) + 1);
					_stream.WriteByte((byte)(_mapInformation.MapCheckpoints[i].IsUsed ? 0x1 : 0x0));

					_stream.SeekTo(insertionBaseOffset + (i * insertionPointSize) + 4);
					_stream.WriteAscii(_mapInformation.MapCheckpoints[i].ZoneName);
				}
				
				int baseOffsetNames = (insertionBaseOffset + (i * insertionPointSize) + insertionNameOffset);
				for (int n = 0; n < languageCount; n++)
				{
					int nameSeek = baseOffsetNames + (n * 0x40);
					_stream.SeekTo(nameSeek);
					_stream.WriteUTF16(_mapInformation.MapCheckpoints[i].CheckpointName[n]);
				}

				int baseOffsetDescriptions = (insertionBaseOffset + (i * insertionPointSize) + insertionDescriptionOffset);
				for (int d = 0; d < languageCount; d++)
				{
					int descriptionSeek = baseOffsetDescriptions + (d * 0x100);
					_stream.SeekTo(descriptionSeek);
					_stream.WriteUTF16(_mapInformation.MapCheckpoints[i].CheckpointDescription[d]);
				}
			}
		}

		private void UpdateDefaultAuthor()
		{
			if (_mapInformation.Game == GameIdentifier.HaloReach)
			{
				_stream.SeekTo(0xCCB8);
				_stream.WriteAscii(_mapInformation.DefaultAuthor);
			}

			if (_mapInformation.Game == GameIdentifier.Halo4)
			{
				_stream.SeekTo(0x11DF8);
				_stream.WriteAscii(_mapInformation.DefaultAuthor);
			}
		}

		#endregion

		public class MaplevlInfo
		{
			public GameIdentifier Game { get; set; }
			public int MapID { get; set; }
			public LevelFlags Flags { get; set; }
			public IList<string> MapNames = new List<string>();
			public IList<string> MapDescriptions = new List<string>();
			public string InternalName { get; set; }
			public string PhysicalName { get; set; }
			public int MapIndex { get; set; }
			public IList<byte> MaxTeamCounts = new List<byte>();
			public BitArray ObjectTable { get; set; }
			public IList<Checkpoint> MapCheckpoints = new List<Checkpoint>();
			public string DefaultAuthor { get; set; }
		}

		// Insertion Points
		public class Checkpoint
		{
			public bool IsVisible { get; set; }
			public bool IsUsed { get; set; }
			public byte ZoneIndex { get; set; }
			public string ZoneName { get; set; }
			public IList<string> CheckpointName = new List<string>();
			public IList<string> CheckpointDescription = new List<string>();
		}
	}
}