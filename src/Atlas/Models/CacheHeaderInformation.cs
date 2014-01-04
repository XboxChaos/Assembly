using System.ComponentModel;

namespace Atlas.Models
{
	public class CacheHeaderInformation : Base
	{
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
	}
}
