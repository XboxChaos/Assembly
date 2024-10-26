using System;
using System.Text;
using System.Xml.Linq;
using Blamite.Util;

namespace Blamite.Serialization.Settings
{
	public class XMLGroupNameLoader : IComplexSettingLoader
	{
		/// <summary>
		///     Loads setting data from a path.
		/// </summary>
		/// <param name="path">The path to load from.</param>
		/// <returns>
		///     The loaded setting data.
		/// </returns>
		public object LoadSetting(string path)
		{
			return LoadGroupNames(path);
		}

		/// <summary>
		///     Loads all of the group names defined in an XML document.
		/// </summary>
		/// <param name="document">The XML document to load group names from.</param>
		/// <param name="path">The path to the original XML. For debugging purposes.</param>
		/// <returns>The names that were loaded.</returns>
		public static GroupNameCollection LoadGroupNames(XDocument document, string path)
		{
			// Make sure there is a root <tagGroups> tag
			XContainer container = document.Element("tagGroups");
			if (container == null)
				throw new ArgumentException($"Invalid group names document.\r\n{path}");

			// Group tags have the format:
			// <group magic="(the magic as a string)" name="(group name)" />
			var result = new GroupNameCollection();
			foreach (XElement gr in container.Elements("group"))
			{
				string magic = XMLUtil.GetStringAttribute(gr, "magic");
				string name = XMLUtil.GetStringAttribute(gr, "name");

				result.AddName(magic, name);
			}
			return result;
		}

		/// <summary>
		///     Loads all of the group names defined in an XML document.
		/// </summary>
		/// <param name="documentPath">The path to the XML document to load.</param>
		/// <returns>The symbols that were loaded.</returns>
		public static GroupNameCollection LoadGroupNames(string documentPath)
		{
			return LoadGroupNames(XDocument.Load(documentPath), documentPath);
		}
	}
}
