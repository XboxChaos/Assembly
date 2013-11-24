using System;
using System.IO;
using System.Xml.Linq;
using Blamite.Util;

namespace Blamite.Flexibility.Settings
{
	/// <summary>
	///     Provides a means of loading a structure layout from an XML file.
	/// </summary>
	public class XMLLayoutLoader : IComplexSettingLoader
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
			if (Directory.Exists(path))
				return LoadLayoutsFromDirectory(path);
			return LoadLayouts(path);
		}

		/// <summary>
		///     Loads all of the structure layouts defined in an XML document.
		/// </summary>
		/// <param name="layoutDocument">The XML document to load structure layouts from.</param>
		/// <returns>The layouts that were loaded.</returns>
		public static StructureLayoutCollection LoadLayouts(XDocument layoutDocument)
		{
			// Make sure there is a root <layouts> tag
			XContainer layoutContainer = layoutDocument.Element("layouts");
			if (layoutContainer == null)
				throw new ArgumentException("Invalid layout document");

			// Layout tags have the format:
			// <layout for="(layout's purpose)">(structure fields)</layout>
			var result = new StructureLayoutCollection();
			foreach (XElement layout in layoutContainer.Elements("layout"))
			{
				string name = XMLUtil.GetStringAttribute(layout, "for");
				int size = XMLUtil.GetNumericAttribute(layout, "size", 0);
				result.AddLayout(name, LoadLayout(layout, size));
			}
			return result;
		}

		/// <summary>
		///     Loads all of the structure layouts defined in an XML document.
		/// </summary>
		/// <param name="documentPath">The path to the XML document to load.</param>
		/// <returns>The layouts that were loaded.</returns>
		public static StructureLayoutCollection LoadLayouts(string documentPath)
		{
			return LoadLayouts(XDocument.Load(documentPath));
		}

		/// <summary>
		///     Loads all of the layout XML files in a directory.
		/// </summary>
		/// <param name="dirPath">The path to the directory to load XML files from.</param>
		/// <returns>The layouts that were loaded.</returns>
		public static StructureLayoutCollection LoadLayoutsFromDirectory(string dirPath)
		{
			var result = new StructureLayoutCollection();
			foreach (string file in Directory.EnumerateFiles(dirPath, "*.xml"))
			{
				StructureLayoutCollection layouts = LoadLayouts(file);
				result.Import(layouts);
			}
			return result;
		}

		/// <summary>
		///     Loads a structure layout based upon an XML container's children.
		/// </summary>
		/// <param name="parentElement">The element containing the value elements to parse.</param>
		/// <param name="size">The size of the structure in bytes.</param>
		/// <returns>The structure layout that was loaded.</returns>
		public static StructureLayout LoadLayout(XElement parentElement, int size)
		{
			var layout = new StructureLayout(size);
			foreach (XElement element in parentElement.Elements())
				HandleElement(layout, element);
			return layout;
		}

		/// <summary>
		///     Parses an XML element and adds the field that it represents to a
		///     structure layout.
		/// </summary>
		/// <param name="layout">The layout to add the parsed field to.</param>
		/// <param name="element">The element to parse.</param>
		private static void HandleElement(StructureLayout layout, XElement element)
		{
			// Every structure field at least has a name and an offset
			string name = XMLUtil.GetStringAttribute(element, "name");
			int offset = XMLUtil.GetNumericAttribute(element, "offset");

			if (IsArrayElement(element))
				HandleArrayElement(layout, element, name, offset);
			else if (IsRawElement(element))
				HandleRawElement(layout, element, name, offset);
			else
				HandleBasicElement(layout, element, name, offset);
		}

		/// <summary>
		///     Parses an XML element representing a basic structure field and adds
		///     the field information to a structure layout.
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
		///     Parses an XML element representing an array field and adds the field
		///     information to a structure layout.
		/// </summary>
		/// <param name="layout">The structure layout to add the field's information to.</param>
		/// <param name="element">The XML element to parse.</param>
		/// <param name="name">The name of the field to add.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		private static void HandleArrayElement(StructureLayout layout, XElement element, string name, int offset)
		{
			int count = XMLUtil.GetNumericAttribute(element, "count");
			int entrySize = XMLUtil.GetNumericAttribute(element, "entrySize");
			layout.AddArrayField(name, offset, count, LoadLayout(element, entrySize));
		}

		/// <summary>
		///     Parses an XML element representing an raw byte array and adds the
		///     field information to a structure layout.
		/// </summary>
		/// <param name="layout">The structure layout to add the field's information to.</param>
		/// <param name="element">The XML element to parse.</param>
		/// <param name="name">The name of the field to add.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		private static void HandleRawElement(StructureLayout layout, XElement element, string name, int offset)
		{
			int size = XMLUtil.GetNumericAttribute(element, "size");
			layout.AddRawField(name, offset, size);
		}

		/// <summary>
		///     Identifies the type of a basic structure field given its tag name.
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
		///     Determines whether or not an element represents an array of values.
		/// </summary>
		/// <param name="element">The XML element to parse.</param>
		/// <returns>true if the element represents an array field.</returns>
		private static bool IsArrayElement(XElement element)
		{
			return (element.Name == "array");
		}

		/// <summary>
		///     Determines whether or not an element represents a raw byte array.
		/// </summary>
		/// <param name="element">The XML element to parse.</param>
		/// <returns>true if the element represents a raw byte array.</returns>
		private static bool IsRawElement(XElement element)
		{
			return (element.Name == "raw");
		}
	}
}