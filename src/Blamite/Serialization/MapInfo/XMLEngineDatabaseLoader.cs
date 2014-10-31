using System.Xml.Linq;
using Blamite.Serialization.Settings;
using Blamite.Util;

namespace Blamite.Serialization.MapInfo
{
	/// <summary>
	///     Loads engine databases from XML data.
	/// </summary>
	public static class XMLEngineDatabaseLoader
	{
		/// <summary>
		///     Loads an engine database from an XML file.
		/// </summary>
		/// <param name="path">The path to the XML file to parse.</param>
		/// <returns>The built engine database.</returns>
		public static EngineDatabase LoadDatabase(string path)
		{
			XDocument document = XDocument.Load(path);
			return LoadDatabase(document);
		}

		/// <summary>
		///     Loads an engine database from an XML document.
		/// </summary>
		/// <param name="document">The XML document to process.</param>
		/// <returns>The built engine database.</returns>
		public static EngineDatabase LoadDatabase(XDocument document)
		{
			XElement enginesElem = document.Element("engines");
			return LoadDatabase(enginesElem);
		}

		/// <summary>
		///     Loads an engine database from an XML container.
		/// </summary>
		/// <param name="container">The container to read engine elements from.</param>
		/// <returns>The built engine database.</returns>
		public static EngineDatabase LoadDatabase(XContainer container)
		{
			XMLSettingsGroupLoader loader = CreateSettingsGroupLoader();
			var result = new EngineDatabase();
			foreach (XElement elem in container.Elements("engine"))
			{
				string name = XMLUtil.GetStringAttribute(elem, "name");
				int levlSize = XMLUtil.GetNumericAttribute(elem, "levlSize");
				int version = XMLUtil.GetNumericAttribute(elem, "version");
				SettingsGroup settings = loader.LoadSettingsGroup(elem);
				var desc = new EngineDescription(name, levlSize, version, settings);
				result.RegisterEngine(desc);
			}
			return result;
		}

		private static XMLSettingsGroupLoader CreateSettingsGroupLoader()
		{
			var loader = new XMLSettingsGroupLoader();
			loader.RegisterComplexSettingLoader("maxTeams", new XMLMaxTeamSetLoader());
			loader.RegisterComplexSettingLoader("multiplayerObjects", new XMLMultiplayerObjectSetLoader());
			return loader;
		}
	}
}