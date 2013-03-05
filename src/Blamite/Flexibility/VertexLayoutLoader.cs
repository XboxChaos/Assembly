using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Blamite.Flexibility
{
    public static class VertexLayoutLoader
    {
        /// <summary>
        /// Loads a VertexLayout based off of information in a vertex XML element.
        /// </summary>
        /// <param name="vertexElement">The vertex element to create the layout from.</param>
        /// <returns>The VertexLayout that was created.</returns>
        public static VertexLayout LoadLayout(XElement vertexElement)
        {
            // Vertex tags have the format:
            // <vertex type="(type index)" name="(vertex name)">(elements)</vertex>
            int type = GetNumericAttribute(vertexElement, "type");
            string name = GetStringAttribute(vertexElement, "name");
            VertexLayout result = new VertexLayout(type, name);

            result.AddElements(LoadLayoutElements(vertexElement));
            return result;
        }

        private static IEnumerable<VertexElementLayout> LoadLayoutElements(XContainer container)
        {
            // Element tags have the format:
            // <value stream="(stream)" offset="(offset)" type="(type)" usage="(usage)" usageIndex="(usage index)" />
            return (from element in container.Elements("value")
                    select new VertexElementLayout(
                        GetNumericAttribute(element, "stream"),
                        GetNumericAttribute(element, "offset"),
                        ParseType(GetStringAttribute(element, "type")),
                        ParseUsage(GetStringAttribute(element, "usage")),
                        GetNumericAttribute(element, "usageIndex")));
        }

        /// <summary>
        /// Searches an enum case-insensitively for a value by name and returns it.
        /// </summary>
        /// <typeparam name="T">The type of the enum to search through.</typeparam>
        /// <param name="name">The case-insensitive name of the value to search for.</param>
        /// <param name="result">The variable to store the result to if a match is found.</param>
        /// <returns>true if the value was found and stored in result.</returns>
        private static bool FindEnumValue<T>(string name, out T result)
        {
            string lowerName = name.ToLower();

            // Use reflection to scan the enum and find the first case-insensitive match
            string[] names = typeof(T).GetEnumNames();
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].ToLower() == lowerName)
                {
                    T[] values = (T[])typeof(T).GetEnumValues();
                    result = values[i];
                    return true;
                }
            }

            result = default(T);
            return false;
        }

        /// <summary>
        /// Attempts to parse a vertex element type string by scanning the VertexElementType enum.
        /// </summary>
        /// <param name="typeStr">The case-insensitive type string to search for.</param>
        /// <returns>The VertexElementType value corresponding to the type string.</returns>
        /// <exception cref="FormatException">Thrown if no type is matched.</exception>
        private static VertexElementType ParseType(string typeStr)
        {
            VertexElementType type;
            if (!FindEnumValue<VertexElementType>(typeStr, out type))
                throw new FormatException("Unrecognized element type \"" + typeStr + "\"");
            return type;
        }

        /// <summary>
        /// Attempts to parse a vertex element usage string by scanning the VertexElementUsage enum.
        /// </summary>
        /// <param name="usageStr">The case-insensitive usage string to search for.</param>
        /// <returns>The VertexElementUsage value corresponding to the usage string.</returns>
        /// <exception cref="FormatException">Thrown if no usage is matched.</exception>
        private static VertexElementUsage ParseUsage(string usageStr)
        {
            VertexElementUsage usage;
            if (!FindEnumValue<VertexElementUsage>(usageStr, out usage))
                throw new FormatException("Unrecognized element usage \"" + usageStr + "\"");
            return usage;
        }

        /// <summary>
        /// Attempts to parse a string, yielding its corresponding integer value.
        /// The input string may hold a number represented in either decimal or in hexadecimal.
        /// </summary>
        /// <param name="numberStr">The string to parse. If it begins with "0x", it will be parsed as hexadecimal.</param>
        /// <param name="result">The variable to store the parsed value to if parsing succeeds.</param>
        /// <returns>true if parsing the string succeeded.</returns>
        private static bool ParseNumber(string numberStr, out int result)
        {
            if (numberStr.StartsWith("0x"))
                return int.TryParse(numberStr.Substring(2), NumberStyles.HexNumber, null, out result);
            return int.TryParse(numberStr, out result);
        }

        /// <summary>
        /// Retrieves the value of a integer attribute on an XML element.
        /// The attribute's value may be represented in either decimal or hexadecimal.
        /// An exception will be thrown if the attribute doesn't exist or is invalid.
        /// </summary>
        /// <param name="element">The XML element that holds the attribute.</param>
        /// <param name="name">The name of the attribute to get the value of.</param>
        /// <returns>The integer value that was parsed.</returns>
        /// <exception cref="ArgumentException">Thrown if the attribute is missing.</exception>
        /// <exception cref="FormatException">Thrown if the attribute does not represent an integer.</exception>
        /// <seealso cref="ParseNumber"/>
        private static int GetNumericAttribute(XElement element, string name)
        {
            XAttribute attribute = element.Attribute(name);
            if (attribute == null)
                throw new ArgumentException("An element is missing the required " + name + " attribute.");
            int result;
            if (!ParseNumber(attribute.Value, out result))
                throw new FormatException("An element has an invalid " + name + " attribute: " + attribute.Value);
            return result;
        }

        /// <summary>
        /// Retrieves the value of an attribute on an XML element.
        /// An exception will be thrown if the attribute doesn't exist.
        /// </summary>
        /// <param name="element">The XML element that holds the attribute.</param>
        /// <param name="name">The name of the attribute to get the value of.</param>
        /// <returns>The attribute's value.</returns>
        /// <exception cref="ArgumentException">Thrown if the attribute is missing.</exception>
        private static string GetStringAttribute(XElement element, string name)
        {
            XAttribute attribute = element.Attribute(name);
            if (attribute == null)
                throw new ArgumentException("An element is missing the required " + name + " attribute.");
            return attribute.Value;
        }
    }
}
