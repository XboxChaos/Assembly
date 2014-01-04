using System.IO;
using Atlas.Models;
using Newtonsoft.Json;

namespace Atlas.Helpers
{
	public class Storage : Base
	{
		public Storage()
		{
			// Get File Path
			string jsonString = null;
			if (File.Exists("Settings.json"))
				jsonString = File.ReadAllText("Settings.json");

			try
			{
				if (jsonString == null)
					_settings = new Settings();
				else
					_settings = JsonConvert.DeserializeObject<Settings>(jsonString) ?? new Settings();
			}
			catch (JsonSerializationException)
			{
				_settings = new Settings();
			}
			_settings.Loaded = true;
		}

		/// <summary>
		/// </summary>
		public Settings Settings
		{
			get { return _settings; }
			set
			{
				// Set Data
				SetField(ref _settings, value);

				// Write Changes
				var jsonData = JsonConvert.SerializeObject(value);

				// Get File Path
				File.WriteAllText("Settings.json", jsonData);
			}
		}
		private Settings _settings = new Settings();
	}
}
