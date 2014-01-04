using System.ComponentModel;

namespace Atlas.Models
{
	public class CacheHeaderInformation : Base
	{
		#region Meta

		[Category("Meta")]
		[Description("The friendly name of the game the cache is compatible with.")]
		[DisplayName("Game")]
		public string Game
		{
			get { return _game; } 
			set { SetField(ref _game, value); }
		}
		private string _game;

		[Category("Meta")]
		[DisplayName("Build")]
		public string Build
		{
			get { return _build; }
			set { SetField(ref _build, value); }
		}
		private string _build;

		[Category("Meta")]
		[DisplayName("Type")]
		public string Type 
		{
			get { return _type; }
			set { SetField(ref _type, value); } 
		}
		private string _type;

		[Category("Meta")]
		[DisplayName("Internal Name")]
		public string InternalName
		{

			get { return _internalName; }
			set { SetField(ref _internalName, value); }
		}
		private string _internalName;

		[Category("Meta")]
		[DisplayName("Scenario Name")]
		public string ScenarioName
		{
			get { return _scenarioName; } 
			set { SetField(ref _scenarioName, value); }
		}
		private string _scenarioName;

		#endregion

		#region Offset

		[Category("Offsets")]
		[DisplayName("Meta Base")]
		public string MetaBase
		{
			get { return _metaBase; }
			set { SetField(ref _metaBase, value); }
		}
		private string _metaBase;

		[Category("Offsets")]
		[DisplayName("Meta Size")]
		public string MetaSize
		{
			get { return _metaSize; }
			set { SetField(ref _metaSize, value); }
		}
		private string _metaSize;

		[Category("Offsets")]
		[DisplayName("Map Magic")]
		public string MapMagic
		{
			get { return _mapMagic; }
			set { SetField(ref _mapMagic, value); }
		}
		private string _mapMagic;

		[Category("Offsets")]
		[DisplayName("Index Header Pointer")]
		public string IndexHeaderPointer
		{
			get { return _indexHeaderPointer; }
			set { SetField(ref _indexHeaderPointer, value); }
		}
		private string _indexHeaderPointer;

		[Category("Offsets")]
		[DisplayName("SDK Version")]
		public string SdkVersion
		{
			get { return _sdkVersion; }
			set { SetField(ref _sdkVersion, value); }
		}
		private string _sdkVersion;

		[Category("Offsets")]
		[DisplayName("Raw Table Offset")]
		public string RawTableOffset
		{
			get { return _rawTableOffset; }
			set { SetField(ref _rawTableOffset, value); }
		}
		private string _rawTableOffset;

		[Category("Offsets")]
		[DisplayName("Raw Table Size")]
		public string RawTableSize
		{
			get { return _rawTableSize; }
			set { SetField(ref _rawTableSize, value); }
		}
		private string _rawTableSize;

		[Category("Offsets")]
		[DisplayName("Index Offset Magic")]
		public string IndexOffsetMagic
		{
			get { return _indexOffsetMagic; }
			set { SetField(ref _indexOffsetMagic, value); }
		}
		private string _indexOffsetMagic;

		#endregion
	}
}
