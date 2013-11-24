using System.Collections.Generic;

namespace Blamite.Patching
{
	public class BlfContent
	{
		public BlfContent(string mapInfoFileName, byte[] mapInfo, TargetGame targetGame,
			IList<BlfContainerEntry> blfContainerEntries)
		{
			MapInfoFileName = mapInfoFileName;
			MapInfo = mapInfo;
			TargetGame = targetGame;
			BlfContainerEntries = blfContainerEntries;
		}

		public BlfContent(string mapInfoFileName, byte[] mapInfo, TargetGame targetGame)
		{
			MapInfoFileName = mapInfoFileName;
			MapInfo = mapInfo;
			TargetGame = targetGame;
			BlfContainerEntries = new List<BlfContainerEntry>();
		}

		/// <summary>
		///     The filename of the mapinfo
		/// </summary>
		public string MapInfoFileName { get; private set; }

		/// <summary>
		///     The custom Mapinfo file.
		/// </summary>
		public byte[] MapInfo { get; private set; }

		/// <summary>
		///     The game the patch is targeted at.
		/// </summary>
		public TargetGame TargetGame { get; private set; }

		/// <summary>
		///     A collection of Blf Conainer Entries
		/// </summary>
		public IList<BlfContainerEntry> BlfContainerEntries { get; private set; }
	}

	public class BlfContainerEntry
	{
		public BlfContainerEntry(string fileName, byte[] blfContainer)
		{
			FileName = fileName;
			BlfContainer = blfContainer;
		}

		/// <summary>
		///     Blf Container File Name
		/// </summary>
		public string FileName { get; private set; }

		/// <summary>
		///     The Blf Container
		/// </summary>
		public byte[] BlfContainer { get; private set; }
	}
}