using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Blamite.IO;
using Blamite.Serialization.MapInfo;

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
		private MaplevlInfo _mapInformation;
		private EndianStream _stream;
		private const int MapNamesOffset = 0x44;
		private int _mapDescriptionsOffset;
		private int _physicalNameOffset;
		private int _internalNameOffset;
		private int _mapIndexOffset;
		private int _maxTeamsOffset;
		private int _mpObjectsOffset;
		private int _insertionOffset;
		private int _defaultAuthorOffset;

		public EngineDescription Engine { get; private set; }

		public MapInfo(string blfLocation, EngineDatabase database)
		{
			Initalize(new FileStream(blfLocation, FileMode.OpenOrCreate), database);
		}

		public MapInfo(Stream blfStream, EngineDatabase database)
		{
			Initalize(blfStream, database);
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

		private void Initalize(Stream blfStream, EngineDatabase database)
		{
			_stream = new EndianStream(blfStream, Endian.BigEndian);

			// Load MapInfo data from file
			LoadMapInfo(database);
		}

		private void UpdateOffsets()
		{
			_mapDescriptionsOffset = (Engine.LanguageCount * 0x40) + MapNamesOffset;
			_physicalNameOffset = (Engine.LanguageCount * 0x100) + _mapDescriptionsOffset;
			_internalNameOffset = _physicalNameOffset + 0x100;
			_mapIndexOffset = _internalNameOffset + 0x100;
			_maxTeamsOffset = _mapIndexOffset + 0xA;
			_mpObjectsOffset = _mapIndexOffset + 0x14;
			_insertionOffset = _mapIndexOffset + 0xA;
			_defaultAuthorOffset = _insertionOffset + (Engine.InsertionCount * Engine.InsertionSize);
		}

		public void Close()
		{
			_stream.Close();
		}

		#region Loading Code

		public void LoadMapInfo(EngineDatabase database)
		{
			_mapInformation = new MaplevlInfo();
			
			// Find out which engine the file uses
			_stream.SeekTo(0x34);
			var size = _stream.ReadInt32();
			_stream.SeekTo(0x38);
			var version = _stream.ReadUInt16();
			Engine = database.FindEngine(size, version);

			if (Engine == null)
				throw new NotSupportedException("Engine version " + version + " of size 0x" + size.ToString("X") + " is not supported");

			// Update offsets based on engine info
			UpdateOffsets();

			// Load Map ID
			_stream.SeekTo(0x3C);
			_mapInformation.MapID = _stream.ReadInt32();

			// Load Flags
			_stream.SeekTo(0x42);
			_mapInformation.Flags = (LevelFlags)_stream.ReadInt16();

			// Load Map Names and Descriptions
			LoadMapNames(MapNamesOffset);
			LoadMapDescriptions(_mapDescriptionsOffset);

			// Load Map Physical Name
			_stream.SeekTo(_physicalNameOffset);
			_mapInformation.PhysicalName = _stream.ReadAscii();

			// Load Map Internal Name
			_stream.SeekTo(_internalNameOffset);
			_mapInformation.InternalName = _stream.ReadAscii();

			// Load Map Index
			_stream.SeekTo(_mapIndexOffset);
			_mapInformation.MapIndex = _stream.ReadInt32();

			// Load Max Teams
			if (Engine.MaxTeamCollection != null)
				LoadMapMaxTeams(_maxTeamsOffset);

			// Load Multiplayer Object Table
			if (Engine.MultiplayerObjectCollection != null)
				LoadMPObjectTable(_mpObjectsOffset);

			// Load Insertion Points
			LoadInsertionPoints(_insertionOffset);

			// Load Default Author Name
			if (Engine.UsesDefaultAuthor)
			{
				_stream.SeekTo(_defaultAuthorOffset);
				_mapInformation.DefaultAuthor = _stream.ReadAscii();
			}
		}

		private void LoadMapNames(int baseOffset)
		{
			_mapInformation.MapNames.Clear();

			for (int i = 0; i < Engine.LanguageCount; i++)
			{
				_stream.SeekTo(baseOffset + (i*0x40));
				_mapInformation.MapNames.Add(_stream.ReadUTF16());
			}
		}

		private void LoadMapDescriptions(int baseOffset)
		{
			_mapInformation.MapDescriptions.Clear();

			for (int i = 0; i < Engine.LanguageCount; i++)
			{
				_stream.SeekTo(baseOffset + (i*0x100));
				_mapInformation.MapDescriptions.Add(_stream.ReadUTF16());
			}
		}

		private void LoadMapMaxTeams(int baseOffset)
		{
			_stream.SeekTo(baseOffset);
			for (int i = 0; i < Engine.MaxTeamCollection.Count; i++)
				_mapInformation.MaxTeamCounts.Add(_stream.ReadByte());
		}

		private void LoadMPObjectTable(int baseOffset)
		{
			var ints = new int[64];
			for (int i = 0; i < 64; i++)
			{
				_stream.SeekTo(baseOffset + (i * 4));
				ints[i] = _stream.ReadInt32();
			}
			_mapInformation.ObjectTable = new BitArray(ints);
		}

		private void LoadInsertionPoints(int baseOffset)
		{
			_mapInformation.MapCheckpoints.Clear();
			
			for (int i = 0; i < Engine.InsertionCount; i++)
			{
				_mapInformation.MapCheckpoints.Add(new Checkpoint());

				_stream.SeekTo(baseOffset + (i * Engine.InsertionSize));
				int visible = _stream.ReadByte();
				_mapInformation.MapCheckpoints[i].IsVisible = visible == 1;

				switch (Engine.InsertionZoneType)
				{
					case ZoneType.Index:
						_stream.SeekTo(baseOffset + (i * Engine.InsertionSize) + 3);
						_mapInformation.MapCheckpoints[i].ZoneIndex = _stream.ReadByte();
						break;
					case ZoneType.Name:
						_stream.SeekTo(baseOffset + (i * Engine.InsertionSize) + 1);
						int used = _stream.ReadByte();
						_mapInformation.MapCheckpoints[i].IsUsed = used == 1;

						_stream.SeekTo(baseOffset + (i * Engine.InsertionSize) + 4);
						_mapInformation.MapCheckpoints[i].ZoneName = _stream.ReadAscii();
						break;
				}

				int namesBaseOffset = (baseOffset + (i * Engine.InsertionSize) + Engine.InsertionNameOffset);
				for (int n = 0; n < Engine.LanguageCount; n++)
				{
					_stream.SeekTo(namesBaseOffset + (n * 0x40));
					_mapInformation.MapCheckpoints[i].CheckpointName.Add(_stream.ReadUTF16());
				}

				int descriptionsBaseOffset = (baseOffset + (i * Engine.InsertionSize) + Engine.InsertionDescriptionOffset);
				for (int d = 0; d < Engine.LanguageCount; d++)
				{
					_stream.SeekTo(descriptionsBaseOffset + (d * 0x100));
					_mapInformation.MapCheckpoints[i].CheckpointDescription.Add(_stream.ReadUTF16());
				}
			}
		}

		#endregion

		#region Update Code

		public void UpdateMapInfo()
		{
			UpdateOffsets();

			// Update MapID
			_stream.SeekTo(0x3C);
			_stream.WriteInt32(_mapInformation.MapID);

			// Load Flags
			_stream.SeekTo(0x42);
			_stream.WriteInt16((short)_mapInformation.Flags);

			// Update Map Names and Descriptions
			UpdateMapNames(MapNamesOffset);
			UpdateMapDescriptions(_mapDescriptionsOffset);

			// Update Map Physical Name
			_stream.SeekTo(_physicalNameOffset);
			_stream.WriteAscii(_mapInformation.PhysicalName);

			// Update Map Internal Name
			_stream.SeekTo(_internalNameOffset);
			_stream.WriteAscii(_mapInformation.InternalName);

			// Update Map Index
			_stream.SeekTo(_mapIndexOffset);
			_stream.WriteInt32(_mapInformation.MapIndex);

			// Update Map Max Teams
			if (Engine.MaxTeamCollection != null)
				UpdateMapMaxTeams(_maxTeamsOffset);

			// Update Multiplayer Object Table
			if (Engine.MultiplayerObjectCollection != null)
				UpdateMPObjectTable(_mpObjectsOffset);

			// Update Insertion Points
			UpdateInsertionPoints(_insertionOffset);

			// Update Default Author
			if (Engine.UsesDefaultAuthor)
			{
				_stream.SeekTo(_defaultAuthorOffset);
				_stream.WriteAscii(_mapInformation.DefaultAuthor);
			}
		}

		private void UpdateMapNames(int baseOffset)
		{
			for (int i = 0; i < _mapInformation.MapNames.Count; i++)
			{
				int seekVal = baseOffset + (i * 0x40);

				_stream.SeekTo(seekVal);
				_stream.WriteUTF16(_mapInformation.MapNames[i]);
			}
		}

		private void UpdateMapDescriptions(int baseOffset)
		{
			for (int i = 0; i < _mapInformation.MapDescriptions.Count; i++)
			{
				int seekVal = baseOffset + (i * 0x100);

				_stream.SeekTo(seekVal);
				_stream.WriteUTF16(_mapInformation.MapDescriptions[i]);
			}
		}

		private void UpdateMapMaxTeams(int baseOffset)
		{
			_stream.SeekTo(baseOffset);
			for (int i = 0; i < Engine.MaxTeamCollection.Count; i++)
				_stream.WriteByte(_mapInformation.MaxTeamCounts[i]);
		}

		private void UpdateMPObjectTable(int baseOffset)
		{
			for (int i = 0; i < 64; i++)
			{
				int seekVal = baseOffset + (i * 4);
				_stream.SeekTo(seekVal);
				var buffer = new int[1];
				_mapInformation.ObjectTable.CopyTo(buffer, i * 32);
				_stream.WriteInt32(buffer[0]);
			}
		}

		private void UpdateInsertionPoints(int baseOffset)
		{
			for (int i = 0; i < Engine.InsertionCount; i++)
			{
				_stream.SeekTo(baseOffset + (i * Engine.InsertionSize));
				_stream.WriteByte((byte)(_mapInformation.MapCheckpoints[i].IsVisible ? 0x1 : 0x0));

				switch (Engine.InsertionZoneType)
				{
					case ZoneType.Index:
						_stream.SeekTo(baseOffset + (i * Engine.InsertionSize) + 3);
						_stream.WriteByte(_mapInformation.MapCheckpoints[i].ZoneIndex);
						break;
					case ZoneType.Name:
						_stream.SeekTo(baseOffset + (i * Engine.InsertionSize) + 1);
						_stream.WriteByte((byte)(_mapInformation.MapCheckpoints[i].IsUsed ? 0x1 : 0x0));

						_stream.SeekTo(baseOffset + (i * Engine.InsertionSize) + 4);
						_stream.WriteAscii(_mapInformation.MapCheckpoints[i].ZoneName);
						break;
				}

				int baseOffsetNames = (baseOffset + (i * Engine.InsertionSize) + Engine.InsertionNameOffset);
				for (int n = 0; n < Engine.LanguageCount; n++)
				{
					int nameSeek = baseOffsetNames + (n * 0x40);
					_stream.SeekTo(nameSeek);
					_stream.WriteUTF16(_mapInformation.MapCheckpoints[i].CheckpointName[n]);
				}

				int baseOffsetDescriptions = (baseOffset + (i * Engine.InsertionSize) + Engine.InsertionDescriptionOffset);
				for (int d = 0; d < Engine.LanguageCount; d++)
				{
					int descriptionSeek = baseOffsetDescriptions + (d * 0x100);
					_stream.SeekTo(descriptionSeek);
					_stream.WriteUTF16(_mapInformation.MapCheckpoints[i].CheckpointDescription[d]);
				}
			}
		}

		#endregion

		public class MaplevlInfo
		{
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