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
		///     Loads all of the group names defined in an XML document.
		/// </summary>
		/// <param name="layoutDocument">The XML document to load group names from.</param>
		/// <returns>The names that were loaded.</returns>
		public static PokingCollection LoadPointers(XDocument namesDocument)
		{
			// Make sure there is a root <poking> tag
			XContainer container = namesDocument.Element("poking");
			if (container == null)
				throw new ArgumentException("Invalid poking document");

			var result = new PokingCollection();
			foreach (XElement cl in container.Elements("version"))
			{
				string name = XMLUtil.GetStringAttribute(cl, "name");

				long headerPointer = XMLUtil.GetNumericAttribute(cl, "headerPointer", -1);

				long headerAddress = XMLUtil.GetNumericAttribute(cl, "headerAddress", -1);
				long magicAddress = XMLUtil.GetNumericAttribute(cl, "magicAddress", -1);
				long magicOffset = XMLUtil.GetNumericAttribute(cl, "magicOffset", -1);
				long sharedMagicAddress = XMLUtil.GetNumericAttribute(cl, "sharedMagicAddress", -1);

				string versionString = XMLUtil.GetStringAttribute(cl, "versionString", null);
				long versionAddress = XMLUtil.GetNumericAttribute(cl, "versionAddress", -1);
				long lastTagIndex = XMLUtil.GetNumericAttribute(cl, "lastTagIndex", -1);
				long indexArrayPointer = XMLUtil.GetNumericAttribute(cl, "indexArrayPointer", -1);
				long addressArrayPointer = XMLUtil.GetNumericAttribute(cl, "addressArrayPointer", -1);

				Version version = new Version(name);

				result.AddName(version, new PokingInformation(headerPointer, headerAddress, magicAddress, magicOffset, sharedMagicAddress,
					versionString, versionAddress, lastTagIndex, indexArrayPointer, addressArrayPointer));
			}
			return result;
		}

		/// <summary>
		///     Loads all of the poking versions defined in an XML document.
		/// </summary>
		/// <param name="documentPath">The path to the XML document to load.</param>
		/// <returns>The poking versions that were loaded.</returns>
		public static PokingCollection LoadPointers(string documentPath)
		{
			return LoadPointers(XDocument.Load(documentPath));
		}
	}
}
