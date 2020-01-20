using System;
using System.Xml.Linq;
using Blamite.Blam;
using Blamite.Util;

namespace Blamite.Serialization.Settings
{
	/// <summary>
	///     Loads stringID set definitions from XML data.
	/// </summary>
	public class XMLStringIDNamespaceLoader : IComplexSettingLoader
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
		///     Loads stringID set definitions from an XML document.
		/// </summary>
		/// <param name="document">The XML document to load set definitions from.</param>
		/// <returns>The StringIDSetResolver that was created.</returns>
		public static StringIDNamespaceResolver LoadStringIDNamespaces(XDocument document)
		{
			// Make sure there is a root <stringIDs> tag
			XElement container = document.Element("stringIDs");
			if (container == null)
				throw new ArgumentException("Invalid stringID definition document");

			StringIDLayout idLayout = ProcessIDLayoutInfo(container);

			// Process <set> elements
			var resolver = new StringIDNamespaceResolver(idLayout);
			foreach (XElement element in container.Elements("namespace"))
				ProcessSetElement(element, resolver);

			return resolver;
		}

		/// <summary>
		///     Loads stringID set definitions from an XML document.
		/// </summary>
		/// <param name="documentPath">The path to the XML document to load.</param>
		/// <returns>The StringIDSetResolver that was created.</returns>
		public static StringIDNamespaceResolver LoadStringIDNamespaces(string documentPath)
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

		private static void ProcessSetElement(XElement element, StringIDNamespaceResolver resolver)
		{
			int id = (int)XMLUtil.GetNumericAttribute(element, "id");
			int min = (int)XMLUtil.GetNumericAttribute(element, "min", 0);
			int globalIndex = (int)XMLUtil.GetNumericAttribute(element, "startIndex");

			resolver.RegisterSet(id, min, globalIndex);
		}
	}
}