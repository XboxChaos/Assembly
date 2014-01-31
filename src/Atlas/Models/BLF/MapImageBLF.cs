using System.ComponentModel;

namespace Atlas.Models.BLF
{
	public class MapImageBLF : Base
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

		[Category("MapImage Chunk")]
		[Description("The number of bytes in the Map Image (mapi) chunk.")]
		[DisplayName("Size")]
		public string MapiLength
		{
			get { return _mapiLength; }
			set { SetField(ref _mapiLength, value); }
		}
		private string _mapiLength;

		[Category("MapImage Chunk")]
		[Description("The version of the Map Image (mapi) chunk.")]
		[DisplayName("Version")]
		public string MapiVersion
		{
			get { return _mapiVersion; }
			set { SetField(ref _mapiVersion, value); }
		}
		private string _mapiVersion;
	}
}
