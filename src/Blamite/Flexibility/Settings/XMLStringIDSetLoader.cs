using System;
using System.Xml.Linq;
using Blamite.Blam;
using Blamite.Util;

namespace Blamite.Flexibility.Settings
{
	/// <summary>
	///     Loads stringID set definitions from XML data.
	/// </summary>
	public class XMLStringIDSetLoader : IComplexSettingLoader
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
			return LoadStringIDSets(path);
		}

		/// <summary>
		///     Loads stringID set definitions from an XML document.
		/// </summary>
		/// <param name="document">The XML document to load set definitions from.</param>
		/// <returns>The StringIDSetResolver that was created.</returns>
		public static StringIDSetResolver LoadStringIDSets(XDocument document)
		{
			// Make sure there is a root <stringIDs> tag
			XElement container = document.Element("stringIDs");
			if (container == null)
				throw new ArgumentException("Invalid stringID definition document");

			StringIDLayout idLayout = ProcessIDLayoutInfo(container);

			// Process <set> elements
			var resolver = new StringIDSetResolver(idLayout);
			foreach (XElement element in container.Elements("set"))
				ProcessSetElement(element, resolver);

			return resolver;
		}

		/// <summary>
		///     Loads stringID set definitions from an XML document.
		/// </summary>
		/// <param name="documentPath">The path to the XML document to load.</param>
		/// <returns>The StringIDSetResolver that was created.</returns>
		public static StringIDSetResolver LoadStringIDSets(string documentPath)
		{
			return LoadStringIDSets(XDocument.Load(documentPath));
		}

		private static StringIDLayout ProcessIDLayoutInfo(XElement element)
		{
			int indexBits = XMLUtil.GetNumericAttribute(element, "indexBits", 16);
			int setBits = XMLUtil.GetNumericAttribute(element, "setBits", 8);
			int lengthBits = XMLUtil.GetNumericAttribute(element, "lengthBits", 0);
			return new StringIDLayout(indexBits, setBits, lengthBits);
		}

		private static void ProcessSetElement(XElement element, StringIDSetResolver resolver)
		{
			int id = XMLUtil.GetNumericAttribute(element, "id");
			int min = XMLUtil.GetNumericAttribute(element, "min", 0);
			int globalIndex = XMLUtil.GetNumericAttribute(element, "startIndex");

			resolver.RegisterSet(id, min, globalIndex);
		}
	}
}