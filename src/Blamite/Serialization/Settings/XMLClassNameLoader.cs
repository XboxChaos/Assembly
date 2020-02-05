using System;
using System.Text;
using System.Xml.Linq;
using Blamite.Util;

namespace Blamite.Serialization.Settings
{
	public class XMLClassNameLoader : IComplexSettingLoader
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
			return LoadClassNames(path);
		}

		/// <summary>
		///     Loads all of the class names defined in an XML document.
		/// </summary>
		/// <param name="layoutDocument">The XML document to load class names from.</param>
		/// <returns>The names that were loaded.</returns>
		public static ClassNameCollection LoadClassNames(XDocument namesDocument)
		{
			// Make sure there is a root <symbols> tag
			XContainer container = namesDocument.Element("classes");
			if (container == null)
				throw new ArgumentException("Invalid class names document");

			// Class tags have the format:
			// <class magic="(the magic as a string)" name="(class name)" />
			var result = new ClassNameCollection();
			foreach (XElement cl in container.Elements("class"))
			{
				string magic = XMLUtil.GetStringAttribute(cl, "magic");
				string name = XMLUtil.GetStringAttribute(cl, "name");

				result.AddName(magic, name);
			}
			return result;
		}

		/// <summary>
		///     Loads all of the class names defined in an XML document.
		/// </summary>
		/// <param name="documentPath">The path to the XML document to load.</param>
		/// <returns>The symbols that were loaded.</returns>
		public static ClassNameCollection LoadClassNames(string documentPath)
		{
			return LoadClassNames(XDocument.Load(documentPath));
		}
	}
}
