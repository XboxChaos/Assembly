using System.ComponentModel;

namespace Atlas.Models.BLF
{
	public class MapInfoBLF : Base
	{
		[Category("BLF")]
		[Description("The game the file is intended for.")]
		[DisplayName("Game")]
		public string Game
		{
			get { return _game; }
			set { SetField(ref _game, value); }
		}
		private string _game;

		[Category("BLF")]
		[Description("The number of BLF chunks in the file.")]
		[DisplayName("Total Chunk Count")]
		public string ChunkCount
		{
			get { return _chunkCount; }
			set { SetField(ref _chunkCount, value); }
		}
		private string _chunkCount;

		[Category("BLF")]
		[Description("The number of bytes in the entire file.")]
		[DisplayName("Size")]
		public string Length
		{
			get { return _length; }
			set { SetField(ref _length, value); }
		}
		private string _length;

		[Category("MapInfo Chunk")]
		[Description("The number of bytes in the mapinfo (levl) chunk.")]
		[DisplayName("Size")]
		public string LevlLength
		{
			get { return _levlLength; }
			set { SetField(ref _levlLength, value); }
		}
		private string _levlLength;

		[Category("MapInfo Chunk")]
		[Description("The version of the mapinfo (levl) chunk.")]
		[DisplayName("Version")]
		public string LevlVersion
		{
			get { return _levlVersion; }
			set { SetField(ref _levlVersion, value); }
		}
		private string _levlVersion;
	}
}
