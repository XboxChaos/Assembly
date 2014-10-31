using System;
using System.Xml.Linq;
using Blamite.Serialization.Settings;
using Blamite.Util;

namespace Blamite.Serialization.MapInfo
{
	/// <summary>
	///     Loads max team set definitions from XML data.
	/// </summary>
	public class XMLMaxTeamSetLoader : IComplexSettingLoader
	{
		/// <summary>
		///     Loads setting data from a path.
		/// </summary>
		/// <param name="path">The path to load from.</param>
		/// <returns>The loaded setting data.</returns>
		public object LoadSetting(string path)
		{
			return LoadMaxTeamSet(path);
		}

		/// <summary>
		///     Loads max team set definitions from an XML document.
		/// </summary>
		/// <param name="document">The XML document to load set definitions from.</param>
		/// <returns>The NameIndexCollection that was created.</returns>
		public static NameIndexCollection LoadMaxTeamSet(XDocument document)
		{
			// Make sure there is a root <maxTeams> tag
			XElement container = document.Element("maxTeams");
			if (container == null)
				throw new ArgumentException("Invalid maxTeams definition document");

			var collection = new NameIndexCollection();

			// Process <maxTeam> elements
			foreach (XElement element in container.Elements("maxTeam"))
				ProcessSetElement(element, collection);

			return collection;
		}

		/// <summary>
		///     Loads max team set definitions from an XML document.
		/// </summary>
		/// <param name="documentPath">The path to the XML document to load.</param>
		/// <returns>The NameIndexCollection that was created.</returns>
		public static NameIndexCollection LoadMaxTeamSet(string documentPath)
		{
			return LoadMaxTeamSet(XDocument.Load(documentPath));
		}

		private static void ProcessSetElement(XElement element, NameIndexCollection collection)
		{
			int index = XMLUtil.GetNumericAttribute(element, "index", -1);
			string name = XMLUtil.GetStringAttribute(element, "name", "");
			collection.AddLayout(new NameIndexLayout(index, name));
		}
	}
}