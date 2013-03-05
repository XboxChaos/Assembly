/* Copyright 2012 Aaron Dierking, TJ Tunnell, Jordan Mueller, Alex Reed
 * 
 * This file is part of ExtryzeDLL.
 * 
 * Extryze is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Extryze is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ExtryzeDLL.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Blamite.IO;

namespace Blamite.Flexibility
{
    /// <summary>
    /// Provides a means of loading a structure layout from an XML file.
    /// </summary>
    public static class XMLLayoutLoader
    {
        /// <summary>
        /// Loads a structure layout based upon an XML container's children.
        /// </summary>
        /// <param name="layoutTag">The collection of structure field tags to parse.</param>
        /// <param name="size">The size of the structure in bytes.</param>
        /// <returns>The structure layout that was loaded.</returns>
        public static StructureLayout LoadLayout(XContainer layoutTag, int size)
        {
            StructureLayout layout = new StructureLayout(size);
            foreach (XElement element in layoutTag.Elements())
                HandleElement(layout, element);
            return layout;
        }

        /// <summary>
        /// Parses an XML element and adds the field that it represents to a
        /// structure layout.
        /// </summary>
        /// <param name="layout">The layout to add the parsed field to.</param>
        /// <param name="element">The element to parse.</param>
        private static void HandleElement(StructureLayout layout, XElement element)
        {
            // Every structure field at least has a name and an offset
            string name = GetStringAttribute(element, "name");
            int offset = GetNumericAttribute(element, "offset");

            if (IsArrayElement(element))
                HandleArrayElement(layout, element, name, offset);
            else if (IsRawElement(element))
                HandleRawElement(layout, element, name, offset);
            else
                HandleBasicElement(layout, element, name, offset);
        }

        /// <summary>
        /// Parses an XML element representing a basic structure field and adds
        /// the field information to a structure layout.
        /// </summary>
        /// <param name="layout">The structure layout to add the field's information to.</param>
        /// <param name="element">The XML element to parse.</param>
        /// <param name="name">The name of the field to add.</param>
        /// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
        private static void HandleBasicElement(StructureLayout layout, XElement element, string name, int offset)
        {
            StructureValueType type = IdentifyValueType(element.Name.LocalName);
            layout.AddBasicField(name, type, offset);
        }

        /// <summary>
        /// Parses an XML element representing an array field and adds the field
        /// information to a structure layout.
        /// </summary>
        /// <param name="layout">The structure layout to add the field's information to.</param>
        /// <param name="element">The XML element to parse.</param>
        /// <param name="name">The name of the field to add.</param>
        /// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
        private static void HandleArrayElement(StructureLayout layout, XElement element, string name, int offset)
        {
            int count = GetNumericAttribute(element, "count");
            int entrySize = GetNumericAttribute(element, "entrySize");
            layout.AddArrayField(name, offset, count, XMLLayoutLoader.LoadLayout(element, entrySize));
        }

        /// <summary>
        /// Parses an XML element representing an raw byte array and adds the
        /// field information to a structure layout.
        /// </summary>
        /// <param name="layout">The structure layout to add the field's information to.</param>
        /// <param name="element">The XML element to parse.</param>
        /// <param name="name">The name of the field to add.</param>
        /// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
        private static void HandleRawElement(StructureLayout layout, XElement element, string name, int offset)
        {
            int size = GetNumericAttribute(element, "size");
            layout.AddRawField(name, offset, size);
        }

        /// <summary>
        /// Identifies the type of a basic structure field given its tag name.
        /// </summary>
        /// <param name="name">The structure field's tag name.</param>
        /// <returns>The StructureValueType corresponding to the tag name.</returns>
        private static StructureValueType IdentifyValueType(string name)
        {
            switch (name)
            {
                case "byte":
                    return StructureValueType.Byte;
                case "sbyte":
                    return StructureValueType.SByte;
                case "uint16":
                    return StructureValueType.UInt16;
                case "int16":
                    return StructureValueType.Int16;
                case "uint32":
                    return StructureValueType.UInt32;
                case "int32":
                    return StructureValueType.Int32;
                case "asciiz":
                    return StructureValueType.Asciiz;
                case "float32":
                    return StructureValueType.Float32;
                default:
                    throw new ArgumentException("Unknown value type " + name);
            }
        }

        /// <summary>
        /// Determines whether or not an element represents an array of values.
        /// </summary>
        /// <param name="element">The XML element to parse.</param>
        /// <returns>true if the element represents an array field.</returns>
        private static bool IsArrayElement(XElement element)
        {
            return (element.Name == "array");
        }

        /// <summary>
        /// Determines whether or not an element represents a raw byte array.
        /// </summary>
        /// <param name="element">The XML element to parse.</param>
        /// <returns>true if the element represents a raw byte array.</returns>
        private static bool IsRawElement(XElement element)
        {
            return (element.Name == "raw");
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
