using System.Xml.Linq;
using Blamite.Util;

namespace Blamite.Flexibility.Settings
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
				string version = XMLUtil.GetStringAttribute(elem, "version");
				string inherits = XMLUtil.GetStringAttribute(elem, "inherits", null);
				SettingsGroup settings = loader.LoadSettingsGroup(elem);
				if (!string.IsNullOrWhiteSpace(inherits))
				{
					// Clone the base engine's settings and then import the new settings on top of it
					SettingsGroup baseSettings = result.FindEngineByName(inherits).Settings.DeepClone();
					baseSettings.Import(settings);
					settings = baseSettings;
				}
				var desc = new EngineDescription(name, version, settings);
				result.RegisterEngine(desc);
			}
			return result;
		}

		private static XMLSettingsGroupLoader CreateSettingsGroupLoader()
		{
			var loader = new XMLSettingsGroupLoader();
			loader.RegisterComplexSettingLoader("layouts", new XMLLayoutLoader());
			loader.RegisterComplexSettingLoader("localeSymbols", new XMLLocaleSymbolLoader());
			loader.RegisterComplexSettingLoader("stringIds", new XMLStringIDSetLoader());
			loader.RegisterComplexSettingLoader("scripting", new XMLOpcodeLookupLoader());
			loader.RegisterComplexSettingLoader("vertexLayouts", new XMLVertexLayoutLoader());
			return loader;
		}
	}
}