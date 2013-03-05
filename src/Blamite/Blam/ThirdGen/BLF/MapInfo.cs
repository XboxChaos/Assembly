using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.BLF
{
    public class MapInfo
    {
        // Private Modifiers
        private EndianStream _stream;
        private MaplevlInfo _mapInformation;
        private int languageCount = 10;
        //private IList<Checkpoint> _mapCheckpoints;

        // Public Modifiers
        public Stream Stream
        {
            get { return _stream.BaseStream; }
        }
        public MaplevlInfo MapInformation
        {
            get { return _mapInformation; }
            set { _mapInformation = value; }
        }

        // Enum Declaration
        public enum GameIdentifier : uint
        {
            Halo3 = 0x4D500003,
            Halo3ODST = 0x98C00003,
            HaloReach = 0xCC980007,
            Halo4NetworkTest = 0xCC980008,
            Halo4 = 0x1DD80009
        }

        // Class Declaration
        public class MaplevlInfo
        {
            public GameIdentifier Game { get; set; }
            public int MapID { get; set; }

            public IList<string> MapNames = new List<string>();
            public IList<string> MapDescriptions = new List<string>();

            public string InternalName { get; set; }
            public string PhysicalName { get; set; }
        }
        //public IList<Checkpoint> MapCheckpoints
        //{
        //    get { return _mapCheckpoints; }
        //    set { _mapCheckpoints = value; }
        //}

        // Class Descriptions
        //public class Checkpoint
        //{
        //    public IList<string> CheckpointName = new List<string>();
        //    public IList<string> CheckpointDescription = new List<string>();
        //}

        public MapInfo(string blfLocation) { Initalize(new FileStream(blfLocation, FileMode.OpenOrCreate)); }
        public MapInfo(Stream blfStream) { Initalize(blfStream); }
        private void Initalize(Stream blfStream)
        {
            _stream = new EndianStream(blfStream, Endian.BigEndian);

            // Load MapInfo shit
            LoadMapInfo();
        }

        #region Loading Code
        public void LoadMapInfo()
        {
            _mapInformation = new MaplevlInfo();
            
            // Load Game Identification
            _stream.SeekTo(0x36);
            _mapInformation.Game = (GameIdentifier)_stream.ReadUInt32();
            UpdateLanguageCount(_mapInformation.Game);

            // Load MapID
            _stream.SeekTo(0x3C);
            _mapInformation.MapID = _stream.ReadInt32();

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
        }
        public void LoadMapNames()
        {
            _mapInformation.MapNames.Clear();

            int baseOffset = _mapInformation.Game == GameIdentifier.Halo4 ? 0x44 : 0x44;
            for (int i = 0; i < languageCount; i++)
            {
                if (i == languageCount - 1)
                    i++;
                _stream.SeekTo(baseOffset + (i * 0x40));
                _mapInformation.MapNames.Add(_stream.ReadUTF16());
            }
        }
        public void LoadMapDescriptions()
        {
            _mapInformation.MapDescriptions.Clear();

            int baseOffset = _mapInformation.Game == GameIdentifier.Halo4 ? 0x0484 : 0x0344;
            for (int i = 0; i < languageCount; i++)
            {
                if (i == languageCount - 1)
                    i++;

                _stream.SeekTo(baseOffset + (i * 0x100));
                _mapInformation.MapDescriptions.Add(_stream.ReadUTF16());
            }
        }
        #endregion

        #region Update Code
        public void UpdateMapInfo()
        {
            // Update Game Identification
            _stream.SeekTo(0x36);
            _stream.WriteUInt32((UInt32)_mapInformation.Game);

            // Update MapID
            _stream.SeekTo(0x3C);
            _stream.WriteInt32(_mapInformation.MapID);

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
        }
        public void UpdateMapNames()
        {
            int baseOffset = _mapInformation.Game == GameIdentifier.Halo4 ? 0x44 : 0x44;
            for (int i = 0; i < _mapInformation.MapNames.Count; i++)
            {
                int seekVal = 0;
                if (i == _mapInformation.MapNames.Count - 1)
                    seekVal = baseOffset + ((i * 0x40) + 0x40);
                else
                    seekVal = baseOffset + (i * 0x40);

                _stream.SeekTo(seekVal);
                _stream.WriteUTF16(_mapInformation.MapNames[i]);
            }
        }
        public void UpdateMapDescriptions()
        {
            int baseOffset = _mapInformation.Game == GameIdentifier.Halo4 ? 0x0484 : 0x0344;
            for (int i = 0; i < _mapInformation.MapDescriptions.Count; i++)
            {
                int seekVal = 0;
                if (i == _mapInformation.MapDescriptions.Count - 1)
                    seekVal = baseOffset + ((i * 0x100) + 0x100);
                else
                    seekVal = baseOffset + (i * 0x100);

                _stream.SeekTo(seekVal);
                _stream.WriteUTF16(_mapInformation.MapDescriptions[i]);
            }
        }
        #endregion

        private void UpdateLanguageCount(GameIdentifier gameIdent)
        {
            switch (gameIdent)
            {
                case GameIdentifier.Halo3: case GameIdentifier.Halo3ODST: case GameIdentifier.HaloReach:
                    languageCount = 10;
                    break;
                case GameIdentifier.Halo4: case GameIdentifier.Halo4NetworkTest:
                    languageCount = 11;
                    break;
                default:
                    throw new InvalidOperationException("The MapInfo BLF file is from an unknown Halo Version");
            }
        }

        public void Close()
        {
            _stream.Close();
        }
    }
}