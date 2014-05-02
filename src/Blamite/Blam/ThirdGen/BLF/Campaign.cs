using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.BLF
{
	public class Campaign
	{
		// Private Modifiers
		public enum GameIdentifier : uint
		{
			Halo3 = 0x13180001,
			Halo4 = 0x1AD80003
		}

		private CmpnInfo _campaign;
		private EndianStream _stream;
		private int languageCount = 12;
		private int mapIDcount = 64;

		public Campaign(string blfLocation)
		{
			Initalize(new FileStream(blfLocation, FileMode.OpenOrCreate));
		}

		public Campaign(Stream blfStream)
		{
			Initalize(blfStream);
		}

		public Stream Stream
		{
			get { return _stream.BaseStream; }
		}

		public CmpnInfo HaloCampaign
		{
			get { return _campaign; }
			set { _campaign = value; }
		}

		private void Initalize(Stream blfStream)
		{
			_stream = new EndianStream(blfStream, Endian.BigEndian);

			// Load Campaign shit
			LoadCampaign();
		}

		private void UpdateLanguageCount(GameIdentifier gameIdent)
		{
			switch (gameIdent)
			{
				case GameIdentifier.Halo3:
					languageCount = 12;
					break;
				case GameIdentifier.Halo4:
					languageCount = 17;
					break;
				default:
					throw new InvalidOperationException("The Campaign BLF file is from an unknown Halo Version");
			}
		}

		public void Close()
		{
			_stream.Close();
		}

		#region Loading Code

		public void LoadCampaign()
		{
			_campaign = new CmpnInfo();

			// Load Game Identification
			_stream.SeekTo(0x36);
			_campaign.Game = (GameIdentifier)_stream.ReadUInt32();
			UpdateLanguageCount(_campaign.Game);

			// Load Campaign Names
			LoadCampaignNames();

			// Load Campaign Descriptions
			LoadCampaignDescriptions();

			// Load Map IDs
			LoadMapIDs();
		}

		public void LoadCampaignNames()
		{
			_campaign.MapNames = new ObservableCollection<string>();

			int baseOffset = _campaign.Game == GameIdentifier.Halo4 ? 0x44 : 0x44;
			for (int i = 0; i < languageCount; i++)
			{
				_stream.SeekTo(baseOffset + (i*0x40));
				_campaign.MapNames.Add(_stream.ReadUTF16());
			}
		}

		public void LoadCampaignDescriptions()
		{
			_campaign.MapDescriptions = new ObservableCollection<string>();

			int baseOffset = _campaign.Game == GameIdentifier.Halo4 ? 0x8C4 : 0x644;
			for (int i = 0; i < languageCount; i++)
			{
				_stream.SeekTo(baseOffset + (i*0x100));
				_campaign.MapDescriptions.Add(_stream.ReadUTF16());
			}
		}

		public void LoadMapIDs()
		{
			_campaign.MapIDs = new ObservableCollection<int>();

			int baseOffset = _campaign.Game == GameIdentifier.Halo4 ? 0x19C4 : 0x1244;
			for (int i = 0; i < mapIDcount; i++)
			{
				_stream.SeekTo(baseOffset + (i * 0x4));
				_campaign.MapIDs.Add(_stream.ReadInt32());
			}
		}

		#endregion

		#region Update Code

		public void UpdateCampaign()
		{
			// Update Map Names
			UpdateMapNames();

			// Update Map Descrptions
			UpdateMapDescriptions();

			// Update Map IDs
			UpdateMapIDs();
		}

		public void UpdateMapNames()
		{
			int baseOffset = _campaign.Game == GameIdentifier.Halo4 ? 0x44 : 0x44;
			for (int i = 0; i < _campaign.MapNames.Count; i++)
			{
				int seekVal = 0;
				//if (i == _campaign.MapNames.Count - 1)
				//	seekVal = baseOffset + ((i*0x40) + 0x40);
				//else
					seekVal = baseOffset + (i*0x40);

				_stream.SeekTo(seekVal);
				_stream.WriteUTF16(_campaign.MapNames[i]);
			}
		}

		public void UpdateMapDescriptions()
		{
			int baseOffset = _campaign.Game == GameIdentifier.Halo4 ? 0x8C4 : 0x644;
			for (int i = 0; i < _campaign.MapDescriptions.Count; i++)
			{
				int seekVal = 0;
				//if (i == _campaign.MapDescriptions.Count - 1)
				//	seekVal = baseOffset + ((i*0x100) + 0x100);
				//else
					seekVal = baseOffset + (i*0x100);

				_stream.SeekTo(seekVal);
				_stream.WriteUTF16(_campaign.MapDescriptions[i]);
			}
		}

		public void UpdateMapIDs()
		{
			int baseOffset = _campaign.Game == GameIdentifier.Halo4 ? 0x19C4 : 0x1244;
			for (int i = 0; i < _campaign.MapIDs.Count; i++)
			{
				int seekVal = 0;
				seekVal = baseOffset + (i * 0x4);
				_stream.SeekTo(seekVal);
				_stream.WriteInt32(_campaign.MapIDs[i]);
			}
		}

		#endregion

		public class CmpnInfo
		{
			public GameIdentifier Game { get; set; }
			public ObservableCollection<string> MapDescriptions { get; set; }
			public ObservableCollection<string> MapNames { get; set; }
			public ObservableCollection<int> MapIDs { get; set; }
		}
	}
}