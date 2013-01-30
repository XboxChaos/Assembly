using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ExtryzeDLL.Blam;

namespace ExtryzeDLL.Flexibility
{
    /// <summary>
    /// Loads stringID set definitions from XML data.
    /// </summary>
    public static class StringIDSetLoader
    {
        /// <summary>
        /// Loads stringID set definitions from an XML container.
        /// </summary>
        /// <param name="container">The XML container to load set definitions from.</param>
        /// <param name="resolver">The StringIDSetResolver to store sets to.</param>
        public static void LoadAllStringIDSets(XContainer container, StringIDSetResolver resolver)
        {
            // Make sure there is a root <layouts> tag
            XContainer stringIDContainer = container.Element("stringIDs");
            if (stringIDContainer == null)
                throw new ArgumentException("Invalid stringID definition document");

            // Process <set> elements
            foreach (XElement element in stringIDContainer.Elements("set"))
                ProcessSetElement(element, resolver);
        }

        private static void ProcessSetElement(XElement element, StringIDSetResolver resolver)
        {
            // Get the set's ID from the "id" attribute
            XAttribute idAttribute = element.Attribute("id");
            if (idAttribute == null)
                throw new ArgumentException("StringID set tags must have an \"id\" attribute");
            short id = (short)ParseNumber(idAttribute.Value);

            // Get the set's min index from the "min" attribute (optional, defaults to 0)
            ushort min = 0;
            XAttribute minAttribute = element.Attribute("min");
            if (minAttribute != null)
                min = (ushort)ParseNumber(minAttribute.Value);

            // Get the set's global index from the "startIndex" attribute
            XAttribute startIndexAttribute = element.Attribute("startIndex");
            if (startIndexAttribute == null)
                throw new ArgumentException("String id set tags must have a \"startIndex\" attribute");
            int globalIndex = ParseNumber(startIndexAttribute.Value);

            // Register the set
            resolver.RegisterSet(id, min, globalIndex);
        }

        /// <summary>
        /// Parses a number stored in a string. Numbers can be either hexadecimal (starting with "0x") or decimal.
        /// </summary>
        /// <param name="str">The number string to store.</param>
        /// <returns>The parsed number.</returns>
        private static int ParseNumber(string str)
        {
            if (str.StartsWith("0x"))
                return int.Parse(str.Substring(2), NumberStyles.HexNumber);
            return int.Parse(str);
        }
    }
}
