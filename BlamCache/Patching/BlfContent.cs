namespace ExtryzeDLL.Patching
{
	public abstract class BlfContent
	{
		protected BlfContent(byte[] mapInfo, TargetGame targetGame)
		{
			MapInfo = mapInfo;
			TargetGame = targetGame;
		}

		public byte[] MapInfo { get; private set; }
		public TargetGame TargetGame { get; private set; }
	}

	public class Halo3BlfContent : BlfContent
	{
		public Halo3BlfContent(byte[] mapInfo, TargetGame targetGame, byte[] blf, byte[] blf_clip, byte[] blf_film, byte[] blf_sm)
			: base(mapInfo, targetGame)
		{
			Blf = blf;
			Blf_Clip = blf_clip;
			Blf_Film = blf_film;
			Blf_Sm = blf_sm;
		}

		public byte[] Blf { get; private set; }
		public byte[] Blf_Clip { get; private set; }
		public byte[] Blf_Film { get; private set; }
		public byte[] Blf_Sm { get; private set; }
	}
	public class HaloReachBlfContent : BlfContent
	{
		public HaloReachBlfContent(byte[] mapInfo, TargetGame targetGame, byte[] blf, byte[] blf_sm)
			: base(mapInfo, targetGame)
		{
			Blf = blf;
			Blf_Sm = blf_sm;
		}

		public byte[] Blf { get; private set; }
		public byte[] Blf_Sm { get; private set; }
	}
	public class Halo4BlfContent : BlfContent
	{
		public Halo4BlfContent(byte[] mapInfo, TargetGame targetGame, byte[] blf, byte[] blf_card, byte[] blf_lobby, byte[] blf_sm)
			: base(mapInfo, targetGame)
		{
			Blf = blf;
			Blf_Card = blf_card;
			Blf_Lobby = blf_lobby;
			Blf_Sm = blf_sm;
		}

		public byte[] Blf { get; private set; }
		public byte[] Blf_Card { get; private set; }
		public byte[] Blf_Lobby { get; private set; }
		public byte[] Blf_Sm { get; private set; }
	}
}