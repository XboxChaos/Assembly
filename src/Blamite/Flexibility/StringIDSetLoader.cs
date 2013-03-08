using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Blamite.Blam;
using Blamite.Util;

namespace Blamite.Flexibility
{
    /// <summary>
    /// Loads stringID set definitions from XML data.
    /// </summary>
    public static class StringIDSetLoader
    {
        /// <summary>
        /// Loads stringID set definitions from an XML document.
        /// </summary>
        /// <param name="container">The XML document to load set definitions from.</param>
        /// <param name="resolver">The StringIDSetResolver to store sets to.</param>
        public static void LoadStringIDSets(XDocument document, StringIDSetResolver resolver)
        {
            // Make sure there is a root <stringIDs> tag
            XContainer stringIDContainer = document.Element("stringIDs");
            if (stringIDContainer == null)
                throw new ArgumentException("Invalid stringID definition document");

            // Process <set> elements
            foreach (XElement element in stringIDContainer.Elements("set"))
                ProcessSetElement(element, resolver);
        }

        /// <summary>
        /// Loads stringID set definitions from an XML document.
        /// </summary>
        /// <param name="documentPath">The path to the XML document to load.</param>
        /// <param name="resolver">The StringIDSetResolver to store sets to.</param>
        /// <returns>The definitions that were loaded.</returns>
        public static void LoadStringIDSets(string documentPath, StringIDSetResolver resolver)
        {
            LoadStringIDSets(XDocument.Load(documentPath), resolver);
        }

        private static void ProcessSetElement(XElement element, StringIDSetResolver resolver)
        {
            byte id = (byte)XMLUtil.GetNumericAttribute(element, "id");
            ushort min = (ushort)XMLUtil.GetNumericAttribute(element, "min", 0);
            int globalIndex = XMLUtil.GetNumericAttribute(element, "startIndex");

            resolver.RegisterSet(id, min, globalIndex);
        }
    }
}
