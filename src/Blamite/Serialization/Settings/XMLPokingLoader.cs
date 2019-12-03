using System;
using System.Text;
using System.Xml.Linq;
using Blamite.Util;

namespace Blamite.Serialization.Settings
{
	public class XMLPokingLoader : IComplexSettingLoader
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
			return LoadPointers(path);
		}

		/// <summary>
		///     Loads all of the class names defined in an XML document.
		/// </summary>
		/// <param name="layoutDocument">The XML document to load class names from.</param>
		/// <returns>The names that were loaded.</returns>
		public static PokingCollection LoadPointers(XDocument namesDocument)
		{
			// Make sure there is a root <symbols> tag
			XContainer container = namesDocument.Element("poking");
			if (container == null)
				throw new ArgumentException("Invalid poking document");

			// Class tags have the format:
			// <class magic="(the magic as a string)" name="(class name)" />
			var result = new PokingCollection();
			foreach (XElement cl in container.Elements("version"))
			{
				string name = XMLUtil.GetStringAttribute(cl, "name");
				long pointer = XMLUtil.GetNumericAttribute(cl, "address");

				result.AddName(name, pointer);
			}
			return result;
		}

		/// <summary>
		///     Loads all of the class names defined in an XML document.
		/// </summary>
		/// <param name="documentPath">The path to the XML document to load.</param>
		/// <returns>The symbols that were loaded.</returns>
		public static PokingCollection LoadPointers(string documentPath)
		{
			return LoadPointers(XDocument.Load(documentPath));
		}
	}
}
