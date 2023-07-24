using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using Blamite.Blam.Scripting;

namespace Blamite.Util
{
	/// <summary>
	///     Provides utility functions for working with XML files.
	/// </summary>
	public static class XMLUtil
	{
		/// <summary>
		///     Attempts to parse a string representing an integer.
		///     The input string may hold a number represented in either decimal or in hexadecimal.
		/// </summary>
		/// <param name="numberStr">The string to parse. If it begins with "0x", it will be parsed as hexadecimal.</param>
		/// <param name="result">The variable to store the parsed value to if parsing succeeds.</param>
		/// <returns>true if parsing the string succeeded.</returns>
		public static bool ParseNumber(string numberStr, out long result)
		{
			if (numberStr.StartsWith("0x"))
				return long.TryParse(numberStr.Substring(2), NumberStyles.HexNumber, null, out result);
			return long.TryParse(numberStr, out result);
		}

		/// <summary>
		///     Retrieves the value of a integer attribute on an XML element.
		///     The attribute's value may be represented in either decimal or hexadecimal.
		///     An exception will be thrown if the attribute doesn't exist or is invalid.
		/// </summary>
		/// <param name="element">The XML element that holds the attribute.</param>
		/// <param name="name">The name of the attribute to get the value of.</param>
		/// <returns>The integer value that was parsed.</returns>
		/// <exception cref="ArgumentException">Thrown if the attribute is missing.</exception>
		/// <exception cref="FormatException">Thrown if the attribute does not represent an integer.</exception>
		/// <seealso cref="ParseNumber" />
		public static long GetNumericAttribute(XElement element, string name)
		{
			XAttribute attribute = element.Attribute(name);
			if (attribute == null)
				throw new ArgumentException("A(n) \"" + element.Name + "\" element is missing the required \"" + name +
											"\" attribute.");

			long result;
			if (ParseNumber(attribute.Value, out result))
				return result;

			throw new FormatException("A(n) \"" + element.Name + "\" element has an invalid \"" + name + "\" attribute: " +
									  attribute.Value);
		}

		/// <summary>
		///     Retrieves the value of a integer attribute on an XML element, returning a default value if it is not found.
		///     The attribute's value may be represented in either decimal or hexadecimal.
		/// </summary>
		/// <param name="element">The XML element that holds the attribute.</param>
		/// <param name="name">The name of the attribute to get the value of.</param>
		/// <param name="defaultValue">The value to return if the attribute is not found.</param>
		/// <returns>The attribute's value, or the default value if the attribute was not found.</returns>
		/// <exception cref="FormatException">Thrown if the attribute does not represent an integer.</exception>
		/// <seealso cref="ParseNumber" />
		public static long GetNumericAttribute(XElement element, string name, long defaultValue)
		{
			XAttribute attribute = element.Attribute(name);
			if (attribute == null)
				return defaultValue;

			long result;
			if (ParseNumber(attribute.Value, out result))
				return result;

			throw new FormatException("A(n) \"" + element.Name + "\" element has an invalid \"" + name + "\" attribute: " +
									  attribute.Value);
		}

		/// <summary>
		///     Retrieves the value of an attribute on an XML element.
		///     An exception will be thrown if the attribute doesn't exist.
		/// </summary>
		/// <param name="element">The XML element that holds the attribute.</param>
		/// <param name="name">The name of the attribute to get the value of.</param>
		/// <returns>The attribute's value.</returns>
		/// <exception cref="ArgumentException">Thrown if the attribute is missing.</exception>
		public static string GetStringAttribute(XElement element, string name)
		{
			XAttribute attribute = element.Attribute(name);
			if (attribute == null)
				throw new ArgumentException("A(n) \"" + element.Name + "\" element is missing the required \"" + name +
											"\" attribute.");
			return attribute.Value;
		}

		/// <summary>
		///     Retrieves the value of an attribute on an XML element, returning a default value if it is not found.
		/// </summary>
		/// <param name="element">The XML element that holds the attribute.</param>
		/// <param name="name">The name of the attribute to get the value of.</param>
		/// <param name="defaultValue">The value to return if the attribute is not found.</param>
		/// <returns>The attribute's value, or the default value if the attribute was not found.</returns>
		public static string GetStringAttribute(XElement element, string name, string defaultValue)
		{
			XAttribute attribute = element.Attribute(name);
			if (attribute != null)
				return attribute.Value;
			return defaultValue;
		}

		/// <summary>
		///     Retrieves the value of a boolean ("true" or "false") attribute on an XML element.
		///     An exception will be thrown if the attribute doesn't exist or is invalid.
		/// </summary>
		/// <param name="element">The XML element that holds the attribute.</param>
		/// <param name="name">The name of the attribute to get the value of.</param>
		/// <returns>The attribute's corresponding bool value.</returns>
		/// <exception cref="FormatException">Thrown if the attribute does not represent a boolean.</exception>
		public static bool GetBoolAttribute(XElement element, string name)
		{
			XAttribute attribute = element.Attribute(name);
			if (attribute == null)
				throw new ArgumentException("A(n) \"" + element.Name + "\" element is missing the required \"" + name +
											"\" attribute.");

			if (attribute.Value == "true")
				return true;
			if (attribute.Value == "false")
				return false;

			throw new FormatException("A(n) \"" + element.Name + "\" element has an invalid \"" + name + "\" attribute: " +
									  attribute.Value);
		}

		/// <summary>
		///     Retrieves the value of a boolean ("true" or "false") attribute on an XML element, returning a default value if it
		///     is not found.
		/// </summary>
		/// <param name="element">The XML element that holds the attribute.</param>
		/// <param name="name">The name of the attribute to get the value of.</param>
		/// <param name="defaultValue">The value to return if the attribute is not found.</param>
		/// <returns>The attribute's corresponding bool value, or the default value if the attribute was not found.</returns>
		/// <exception cref="FormatException">Thrown if the attribute does not represent a boolean.</exception>
		public static bool GetBoolAttribute(XElement element, string name, bool defaultValue)
		{
			XAttribute attribute = element.Attribute(name);
			if (attribute == null)
				return defaultValue;

			if (attribute.Value == "true")
				return true;
			if (attribute.Value == "false")
				return false;

			throw new FormatException("A(n) \"" + element.Name + "\" element has an invalid \"" + name + "\" attribute: " +
									  attribute.Value);
		}

		/// <summary>
		///     Retrieves the value of an enum-based attribute on an XML element.
		///     The attribute value must be in lowercase.
		///     An exception will be thrown if the attribute doesn't exist or is invalid.
		/// </summary>
		/// <typeparam name="EnumType">The type of the enum to search through.</typeparam>
		/// <param name="element">The XML element that holds the attribute.</param>
		/// <param name="name">The name of the attribute to get the value of.</param>
		/// <returns>The enum value corresponding to the attribute's value.</returns>
		/// <exception cref="FormatException">Thrown if the attribute value does not correspond to a value in the enum.</exception>
		public static EnumType GetEnumAttribute<EnumType>(XElement element, string name)
		{
			XAttribute attribute = element.Attribute(name);
			if (attribute == null)
				throw new ArgumentException("A(n) \"" + element.Name + "\" element is missing the required \"" + name +
											"\" attribute.");

			EnumType result;
			if (FindEnumValueLower(attribute.Value, out result))
				return result;

			throw new FormatException("A(n) \"" + element.Name + "\" element has an invalid \"" + name + "\" attribute: " +
									  attribute.Value);
		}

		/// <summary>
		///     Retrieves the value of an enum-based attribute on an XML element, returning a default value if it is not found.
		///     The attribute value must be in lowercase.
		/// </summary>
		/// <typeparam name="EnumType">The type of the enum to search through.</typeparam>
		/// <param name="element">The XML element that holds the attribute.</param>
		/// <param name="name">The name of the attribute to get the value of.</param>
		/// <returns>The enum value corresponding to the attribute's value, or the default value if the attribute was not found.</returns>
		/// <exception cref="FormatException">Thrown if the attribute value does not correspond to a value in the enum.</exception>
		public static EnumType GetEnumAttribute<EnumType>(XElement element, string name, EnumType defaultValue)
		{
			XAttribute attribute = element.Attribute(name);
			if (attribute == null)
				return defaultValue;

			EnumType result;
			if (FindEnumValueLower(attribute.Value, out result))
				return result;

			throw new FormatException("A(n) \"" + element.Name + "\" element has an invalid \"" + name + "\" attribute: " +
									  attribute.Value);
		}

		/// <summary>
		///     Returns whether or not an element has an attribute set.
		/// </summary>
		/// <param name="element">The XML element to search for the attribute on.</param>
		/// <param name="name">The name of the attribute to find.</param>
		/// <returns>true if the element has the attribute set.</returns>
		public static bool HasAttribute(XElement element, string name)
		{
			return (element.Attribute(name) != null);
		}

		/// <summary>
		///     Searches an enum for a value by its lowercase name and returns it.
		/// </summary>
		/// <typeparam name="T">The type of the enum to search through.</typeparam>
		/// <param name="name">The lowercase name of the value to search for.</param>
		/// <param name="result">The variable to store the result to if a match is found.</param>
		/// <returns>true if the value was found and stored in result.</returns>
		private static bool FindEnumValueLower<T>(string name, out T result)
		{
			// Use reflection to scan the enum and find the first case-insensitive match
			string[] names = Enum.GetNames(typeof(T));
			for (int i = 0; i < names.Length; i++)
			{
				if (names[i].ToLowerInvariant() != name) continue;
				
				var values = (T[]) Enum.GetValues(typeof(T));
				result = values[i];
				return true;
			}

			result = default(T);
			return false;
		}

		public static void WriteScriptExpressionsToXml(IEnumerable<ScriptExpression> expressions, string outputPath)
		{
			if (expressions.Count() > 0)
			{
				var settings = new XmlWriterSettings
				{
					Indent = true
				};

				using (var writer = XmlWriter.Create(outputPath, settings))
				{
					writer.WriteStartDocument();
					writer.WriteStartElement("Expressions");

					foreach (var expr in expressions)
					{
						WriteExpression(writer, expr);
					}

					writer.WriteEndDocument();
					writer.Close();
				}
			}
		}

		private static void WriteExpression(XmlWriter writer, ScriptExpression expression)
		{
			writer.WriteStartElement("Expression");
			writer.WriteAttributeString("Index", expression.Index.Index.ToString("X4"));
			writer.WriteAttributeString("Salt", expression.Index.Salt.ToString("X4"));
			writer.WriteAttributeString("Opcode", expression.Opcode.ToString("X4"));
			writer.WriteAttributeString("ValueType", expression.ReturnType.ToString("X4"));
			switch (expression.Type)
			{
				case ScriptExpressionType.Group:
					writer.WriteAttributeString("ExpressionType", "Call");
					break;
				case ScriptExpressionType.Expression:
					writer.WriteAttributeString("ExpressionType", "Expression");
					break;
				case ScriptExpressionType.ScriptReference:
					writer.WriteAttributeString("ExpressionType", "Script");
					break;
				case ScriptExpressionType.GlobalsReference:
					writer.WriteAttributeString("ExpressionType", "Global");
					break;
				case ScriptExpressionType.ParameterReference:
					writer.WriteAttributeString("ExpressionType", "Parameter");
					break;
			}
			writer.WriteAttributeString("NextSalt", expression.Next.Salt.ToString("X4"));
			writer.WriteAttributeString("NextIndex", expression.Next.Index.ToString("X4"));
			writer.WriteAttributeString("StringOff", expression.StringOffset.ToString("X"));
			writer.WriteAttributeString("String", expression.StringValue);
			writer.WriteAttributeString("Value", expression.Value.ToString());
			writer.WriteAttributeString("LineNum", expression.LineNumber.ToString("X4"));
			writer.WriteEndElement();
		}

		public static void WritUnitSeatMappingsToXml(IEnumerable<UnitSeatMapping> mappings, string outputPath)
        {
			if (mappings.Count() > 0)
			{
				var settings = new XmlWriterSettings
				{
					Indent = true
				};

				using (var writer = XmlWriter.Create(outputPath, settings))
				{
					writer.WriteStartDocument();
					writer.WriteStartElement("UnitSeatMappings");

					foreach (var mapping in mappings.OrderBy(m => m.Index))
					{
						writer.WriteStartElement("Mapping");
						writer.WriteAttributeString("Index", mapping.Index.ToString());
						writer.WriteAttributeString("Name", mapping.Name);
						writer.WriteAttributeString("Count", mapping.Count.ToString());
						writer.WriteEndElement();
					}

					writer.WriteEndDocument();
					writer.Close();
				}
			}
		}
	}
}
