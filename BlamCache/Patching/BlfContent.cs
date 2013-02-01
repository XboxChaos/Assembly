using System.Collections.Generic;

namespace ExtryzeDLL.Patching
{
	public class BlfContent
	{
		public BlfContent(byte[] mapInfo, TargetGame targetGame, IList<BlfContainerEntry> blfContainerEntries)
		{
			MapInfo = mapInfo;
			TargetGame = targetGame;
			BlfContainerEntries = blfContainerEntries;
		}
		public BlfContent(byte[] mapInfo, TargetGame targetGame)
		{
			MapInfo = mapInfo;
			TargetGame = targetGame;
			BlfContainerEntries = new List<BlfContainerEntry>();
		}

		/// <summary>
		/// The custom Mapinfo file.
		/// </summary>
		public byte[] MapInfo { get; private set; }

		/// <summary>
		/// The game the patch is targeted at.
		/// </summary>
		public TargetGame TargetGame { get; private set; }

		/// <summary>
		/// A collection of Blf Conainer Entries
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
		/// Blf Container File Name
		/// </summary>
		public string FileName { get; private set; }

		/// <summary>
		/// The Blf Container
		/// </summary>
		public byte[] BlfContainer { get; private set; }
	}
}