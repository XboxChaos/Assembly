using System.ComponentModel;

namespace Atlas.Models.BLF
{
	public class CampaignBLF : Base
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

		[Category("Campaign Chunk")]
		[Description("The number of bytes in the Campaign (cmpn) chunk.")]
		[DisplayName("Size")]
		public string CmpnLength
		{
			get { return _cmpnLength; }
			set { SetField(ref _cmpnLength, value); }
		}
		private string _cmpnLength;

		[Category("Campaign Chunk")]
		[Description("The version of the Campaign (cmpn) chunk.")]
		[DisplayName("Version")]
		public string CmpnVersion
		{
			get { return _cmpnVersion; }
			set { SetField(ref _cmpnVersion, value); }
		}
		private string _cmpnVersion;
	}
}
