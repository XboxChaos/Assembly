using System;
using System.Xml.Linq;
using Blamite.Blam;
using Blamite.Util;

namespace Blamite.Serialization.Settings
{
	/// <summary>
	///     Loads stringID namespace definitions from XML data.
	/// </summary>
	public class XMLStringIDLoader : IComplexSettingLoader
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
			return LoadStringIDNamespaces(path);
		}

		/// <summary>
		///     Loads stringID namespace definitions from an XML document.
		/// </summary>
		/// <param name="document">The XML document to load namespace definitions from.</param>
		/// <returns>The StringIDInformation that was created.</returns>
		public static StringIDInformation LoadStringIDNamespaces(XDocument document)
		{
			// Make sure there is a root <stringIDs> tag
			XElement container = document.Element("stringIDs");
			if (container == null)
				throw new ArgumentException("Invalid stringID definition document");

			StringIDLayout idLayout = ProcessIDLayoutInfo(container);

			// Process <namespace> elements
			var info = new StringIDInformation(idLayout);

			foreach (XElement element in container.Elements("namespace"))
				ProcessNamespaceElement(element, info);

			return info;
		}

		/// <summary>
		///     Loads stringID namespace definitions from an XML document.
		/// </summary>
		/// <param name="documentPath">The path to the XML document to load.</param>
		/// <returns>The StringIDInformation that was created.</returns>
		public static StringIDInformation LoadStringIDNamespaces(string documentPath)
		{
			return LoadStringIDNamespaces(XDocument.Load(documentPath));
		}

		private static StringIDLayout ProcessIDLayoutInfo(XElement element)
		{
			int indexBits = (int)XMLUtil.GetNumericAttribute(element, "indexBits", 16);
			int setBits = (int)XMLUtil.GetNumericAttribute(element, "namespaceBits", 8);
			int lengthBits = (int)XMLUtil.GetNumericAttribute(element, "lengthBits", 0);
			return new StringIDLayout(indexBits, setBits, lengthBits);
		}

		private static void ProcessNamespaceElement(XElement element, StringIDInformation resolver)
		{
			int id = (int)XMLUtil.GetNumericAttribute(element, "id");
			int min = (int)XMLUtil.GetNumericAttribute(element, "min", 0);
			int globalIndex = (int)XMLUtil.GetNumericAttribute(element, "startIndex");

			resolver.AddNamespace(id, min, globalIndex);
		}
	}
}