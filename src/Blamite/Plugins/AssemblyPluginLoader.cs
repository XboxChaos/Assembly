using System;
using System.Globalization;
using System.Linq;
using System.Xml;
using Blamite.Blam.Shaders;

namespace Blamite.Plugins
{
	public static class AssemblyPluginLoader
	{
		/// <summary>
		///     Parses an XML plugin, calling the corresponding method in
		///     IPluginVisitor for each XML tag it encounters.
		/// </summary>
		/// <param name="reader">The XmlReader to read the plugin XML from.</param>
		/// <param name="visitor">The IPluginVisitor to call for each XML tag.</param>
		public static void LoadPlugin(XmlReader reader, IPluginVisitor visitor)
		{
			if (!reader.ReadToNextSibling("plugin"))
				throw new ArgumentException("The XML file is missing a <plugin> tag.");

			int baseSize = 0;
			if (reader.MoveToAttribute("baseSize"))
				baseSize = ParseInt(reader.Value);

			if (!visitor.EnterPlugin(baseSize)) return;
			ReadElements(reader, true, visitor);
			visitor.LeavePlugin();
		}

		private static void ReadElements(XmlReader reader, bool topLevel, IPluginVisitor visitor)
		{
			while (reader.Read())
			{
				if (reader.NodeType != XmlNodeType.Element) continue;
				if (topLevel)
					HandleTopLevelElement(reader, visitor);
				else
					HandleElement(reader, visitor);
			}
		}

		private static void HandleTopLevelElement(XmlReader reader, IPluginVisitor visitor)
		{
			if (reader.Name == "revisions")
			{
				if (visitor.EnterRevisions())
				{
					ReadRevisions(reader.ReadSubtree(), visitor);
					visitor.LeaveRevisions();
				}
				else
				{
					reader.Skip();
				}
			}
			else
			{
				HandleElement(reader, visitor);
			}
		}

		private static void HandleElement(XmlReader reader, IPluginVisitor visitor)
		{
			switch (reader.Name)
			{
				case "comment":
					ReadComment(reader, visitor);
					break;
				default:
					HandleValueElement(reader, reader.Name, visitor);
					break;
			}
		}

		private static void ReadComment(XmlReader reader, IPluginVisitor visitor)
		{
			string title = "Comment";

			if (reader.MoveToAttribute("title"))
				title = reader.Value;

			reader.MoveToElement();
			var xmlLineInfo = reader as IXmlLineInfo;
			if (xmlLineInfo == null) return;
			var pluginLine = (uint) xmlLineInfo.LineNumber;
			string text = reader.ReadElementContentAsString();
			visitor.VisitComment(title, text, pluginLine);
		}

		/// <summary>
		///     Handles an element which describes how a value
		///     should be read from the cache file.
		/// </summary>
		/// <param name="reader">The XmlReader that read the element.</param>
		/// <param name="elementName">The element's name.</param>
		/// <param name="visitor">The IPluginVisitor to call to.</param>
		private static void HandleValueElement(XmlReader reader, string elementName, IPluginVisitor visitor)
		{
			string name = "Unknown";
			uint offset = 0;
			var xmlLineInfo = reader as IXmlLineInfo;
			if (xmlLineInfo == null) return;
			var pluginLine = (uint) xmlLineInfo.LineNumber;
			bool visible = true;
			string tooltip = "";

			if (reader.MoveToAttribute("name"))
				name = reader.Value;
			if (reader.MoveToAttribute("offset"))
				offset = ParseUInt(reader.Value);
			if (reader.MoveToAttribute("visible"))
				visible = ParseBool(reader.Value);
			if (reader.MoveToAttribute("tooltip"))
				tooltip = reader.Value;

			reader.MoveToElement();
			switch (elementName.ToLower()) // FIXME: Using ToLower() here violates XML standards
			{
				case "uint8":
					visitor.VisitUInt8(name, offset, visible, pluginLine, tooltip);
					break;
				case "int8":
					visitor.VisitInt8(name, offset, visible, pluginLine, tooltip);
					break;
				case "uint16":
					visitor.VisitUInt16(name, offset, visible, pluginLine, tooltip);
					break;
				case "int16":
					visitor.VisitInt16(name, offset, visible, pluginLine, tooltip);
					break;
				case "uint32":
					visitor.VisitUInt32(name, offset, visible, pluginLine, tooltip);
					break;
				case "int32":
					visitor.VisitInt32(name, offset, visible, pluginLine, tooltip);
					break;
				case "uint64":
					visitor.VisitUInt64(name, offset, visible, pluginLine, tooltip);
					break;
				case "int64":
					visitor.VisitInt64(name, offset, visible, pluginLine, tooltip);
					break;
				case "float32":
				case "float":
					visitor.VisitFloat32(name, offset, visible, pluginLine, tooltip);
					break;
				case "undefined":
					visitor.VisitUndefined(name, offset, visible, pluginLine, tooltip);
					break;
				case "point2":
					visitor.VisitPoint2(name, offset, visible, pluginLine, tooltip);
					break;
				case "point3":
					visitor.VisitPoint3(name, offset, visible, pluginLine, tooltip);
					break;
				case "vector2":
					visitor.VisitVector2(name, offset, visible, pluginLine, tooltip);
					break;
				case "vector3":
					visitor.VisitVector3(name, offset, visible, pluginLine, tooltip);
					break;
				case "vector4":
				case "quaternion":
					visitor.VisitVector4(name, offset, visible, pluginLine, tooltip);
					break;
				case "degree":
					visitor.VisitDegree(name, offset, visible, pluginLine, tooltip);
					break;
				case "degree2":
					visitor.VisitDegree2(name, offset, visible, pluginLine, tooltip);
					break;
				case "degree3":
					visitor.VisitDegree3(name, offset, visible, pluginLine, tooltip);
					break;
				case "plane2":
					visitor.VisitPlane2(name, offset, visible, pluginLine, tooltip);
					break;
				case "plane3":
					visitor.VisitPlane3(name, offset, visible, pluginLine, tooltip);
					break;
				case "rect16":
					visitor.VisitRect16(name, offset, visible, pluginLine, tooltip);
					break;
				case "quat16":
					visitor.VisitQuat16(name, offset, visible, pluginLine, tooltip);
					break;
				case "point16":
					visitor.VisitPoint16(name, offset, visible, pluginLine, tooltip);
					break;
				case "stringid":
					visitor.VisitStringID(name, offset, visible, pluginLine, tooltip);
					break;
				case "tagref":
					ReadTagRef(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "ascii":
					ReadAscii(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "utf16":
					ReadUtf16(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "flags8":
				case "bitfield8":
					if (visitor.EnterFlags8(name, offset, visible, pluginLine, tooltip))
						ReadBits(reader, visitor);
					else
						reader.Skip();
					break;
				case "flags16":
				case "bitfield16":
					if (visitor.EnterFlags16(name, offset, visible, pluginLine, tooltip))
						ReadBits(reader, visitor);
					else
						reader.Skip();
					break;
				case "flags32":
				case "bitfield32":
					if (visitor.EnterFlags32(name, offset, visible, pluginLine, tooltip))
						ReadBits(reader, visitor);
					else
						reader.Skip();
					break;
				case "flags64":
				case "bitfield64":
					if (visitor.EnterFlags64(name, offset, visible, pluginLine, tooltip))
						ReadBits(reader, visitor);
					else
						reader.Skip();
					break;

				case "enum8":
					if (visitor.EnterEnum8(name, offset, visible, pluginLine, tooltip))
						ReadOptions(reader, visitor);
					else
						reader.Skip();
					break;
				case "enum16":
					if (visitor.EnterEnum16(name, offset, visible, pluginLine, tooltip))
						ReadOptions(reader, visitor);
					else
						reader.Skip();
					break;
				case "enum32":
					if (visitor.EnterEnum32(name, offset, visible, pluginLine, tooltip))
						ReadOptions(reader, visitor);
					else
						reader.Skip();
					break;

				case "color":
				case "color32":
					visitor.VisitColorInt(name, offset, visible, ReadColorAlpha(reader), pluginLine, tooltip);
					break;
				case "colorf":
					ReadColorF(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "dataref":
					ReadDataRef(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "reflexive":
				case "tagblock":
					ReadTagBlock(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "raw":
					ReadRaw(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "shader":
					ReadShader(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "uniclist":
					ReadUnicList(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "range16":
					visitor.VisitRangeInt16(name, offset, visible, pluginLine, tooltip);
					break;
				case "rangef":
					visitor.VisitRangeFloat32(name, offset, visible, pluginLine, tooltip);
					break;
				case "ranged":
					visitor.VisitRangeDegree(name, offset, visible, pluginLine, tooltip);
					break;
				case "datum":
					visitor.VisitDatum(name, offset, visible, pluginLine, tooltip);
					break;

				default:
					throw new ArgumentException("Unknown element \"" + elementName + "\"." + PositionInfo(reader));
			}
		}

		private static void ReadRevisions(XmlReader reader, IPluginVisitor visitor)
		{
			reader.ReadStartElement();
			while (reader.ReadToFollowing("revision"))
				visitor.VisitRevision(ReadRevision(reader));
		}

		private static PluginRevision ReadRevision(XmlReader reader)
		{
			string author = "";
			int version = 1;

			if (reader.MoveToAttribute("author"))
				author = reader.Value;
			if (reader.MoveToAttribute("version"))
				version = ParseInt(reader.Value);

			reader.MoveToElement();
			string description = reader.ReadElementContentAsString();
			return new PluginRevision(author, version, description);
		}

		private static void ReadDataRef(XmlReader reader, string name, uint offset, bool visible, IPluginVisitor visitor,
			uint pluginLine, string tooltip)
		{
			string format = "bytes";

			if (reader.MoveToAttribute("format"))
				format = reader.Value;

			if (format != "bytes" &&
			    format != "utf16" &&
			    format != "asciiz")
				throw new ArgumentException("Invalid format. Must be either `bytes`, `utf16`, or `asciiz`.");

			int align = 4;
			if (reader.MoveToAttribute("align"))
				align = ParseInt(reader.Value);

			visitor.VisitDataReference(name, offset, format, visible, align, pluginLine, tooltip);
		}

		private static void ReadTagRef(XmlReader reader, string name, uint offset, bool visible, IPluginVisitor visitor,
			uint pluginLine, string tooltip)
		{
			bool withGroup = true;

			if (reader.MoveToAttribute("withClass") || reader.MoveToAttribute("withGroup"))
				withGroup = ParseBool(reader.Value);

			visitor.VisitTagReference(name, offset, visible, withGroup, pluginLine, tooltip);
		}

		private static void ReadAscii(XmlReader reader, string name, uint offset, bool visible, IPluginVisitor visitor,
			uint pluginLine, string tooltip)
		{
			// Both "size" and "length" are accepted here because they are the same
			// with ASCII strings, but "size" should be preferred because it's less ambiguous
			// and <utf16> only supports "size"
			int size = 0;
			if (reader.MoveToAttribute("size") || reader.MoveToAttribute("length"))
				size = ParseInt(reader.Value);

			visitor.VisitAscii(name, offset, visible, size, pluginLine, tooltip);
		}

		private static void ReadUtf16(XmlReader reader, string name, uint offset, bool visible, IPluginVisitor visitor,
			uint pluginLine, string tooltip)
		{
			int size = 0;
			if (reader.MoveToAttribute("size"))
				size = ParseInt(reader.Value);

			visitor.VisitUtf16(name, offset, visible, size, pluginLine, tooltip);
		}

		private static void ReadBits(XmlReader reader, IPluginVisitor visitor)
		{
			XmlReader subtree = reader.ReadSubtree();

			subtree.ReadStartElement();
			while (subtree.ReadToNextSibling("bit"))
				ReadBit(subtree, visitor);

			visitor.LeaveFlags();
		}

		private static void ReadBit(XmlReader reader, IPluginVisitor visitor)
		{
			string name = "Unknown";
			string tooltip = "";

			if (reader.MoveToAttribute("name"))
				name = reader.Value;
			if (!reader.MoveToAttribute("index"))
				throw new ArgumentException("Bit definitions must have an index." + PositionInfo(reader));
			int index = ParseInt(reader.Value);
			if (reader.MoveToAttribute("tooltip"))
				tooltip = reader.Value;

			visitor.VisitBit(name, index, tooltip);
		}

		private static void ReadOptions(XmlReader reader, IPluginVisitor visitor)
		{
			XmlReader subtree = reader.ReadSubtree();

			subtree.ReadStartElement();
			while (subtree.ReadToNextSibling("option"))
				ReadOption(subtree, visitor);

			visitor.LeaveEnum();
		}

		private static void ReadOption(XmlReader reader, IPluginVisitor visitor)
		{
			string name = "Unknown";
			int value = 0;
			string tooltip = "";

			if (reader.MoveToAttribute("name"))
				name = reader.Value;
			if (reader.MoveToAttribute("value"))
				value = ParseInt(reader.Value);
			if (reader.MoveToAttribute("tooltip"))
				tooltip = reader.Value;

			visitor.VisitOption(name, value, tooltip);
		}

		private static void ReadColorF(XmlReader reader, string name, uint offset, bool visible, IPluginVisitor visitor,
			uint pluginLine, string tooltip)
		{
			bool basic = false;

			if (reader.MoveToAttribute("basic"))
				basic = ParseBool(reader.Value);

			bool alpha = ReadColorAlpha(reader);

			visitor.VisitColorF(name, offset, visible, alpha, basic, pluginLine, tooltip);
		}

		private static bool ReadColorAlpha(XmlReader reader)
		{
			if (!reader.MoveToAttribute("alpha"))
				throw new ArgumentException("Color tags must have a alpha attribute." + PositionInfo(reader));

			return ParseBool(reader.Value);
		}

		private static void ReadTagBlock(XmlReader reader, string name, uint offset, bool visible, IPluginVisitor visitor,
			uint pluginLine, string tooltip)
		{
			if (!reader.MoveToAttribute("elementSize") && !reader.MoveToAttribute("entrySize"))
				throw new ArgumentException("Tag Blocks must have an elementSize attribute." + PositionInfo(reader));

			uint elementSize = ParseUInt(reader.Value);
			int align = 4;
			if (reader.MoveToAttribute("align"))
				align = ParseInt(reader.Value);
			bool sort = false;
			if (reader.MoveToAttribute("sort"))
				sort = ParseBool(reader.Value);

			if (visitor.EnterTagBlock(name, offset, visible, elementSize, align, sort, pluginLine, tooltip))
			{
				reader.MoveToElement();
				XmlReader subtree = reader.ReadSubtree();

				subtree.ReadStartElement();
				ReadElements(subtree, false, visitor);
				visitor.LeaveTagBlock();
			}
			else
			{
				reader.Skip();
			}
		}

		private static void ReadRaw(XmlReader reader, string name, uint offset, bool visible, IPluginVisitor visitor,
			uint pluginLine, string tooltip)
		{
			if (!reader.MoveToAttribute("size"))
				throw new ArgumentException("Raw data blocks must have a size attribute." + PositionInfo(reader));
			int size = ParseInt(reader.Value);

			visitor.VisitRawData(name, offset, visible, size, pluginLine, tooltip);
		}

		private static void ReadShader(XmlReader reader, string name, uint offset, bool visible, IPluginVisitor visitor,
			uint pluginLine, string tooltip)
		{
			if (!reader.MoveToAttribute("type"))
				throw new ArgumentException("Shaders must have a type attribute." + PositionInfo(reader));

			ShaderType type;
			if (reader.Value == "pixel")
				type = ShaderType.Pixel;
			else if (reader.Value == "vertex")
				type = ShaderType.Vertex;
			else
				throw new ArgumentException("Invalid shader type \"" + reader.Value + "\"");

			visitor.VisitShader(name, offset, visible, type, pluginLine, tooltip);
		}

		private static void ReadUnicList(XmlReader reader, string name, uint offset, bool visible, IPluginVisitor visitor,
			uint pluginLine, string tooltip)
		{
			if (!reader.MoveToAttribute("languages"))
				throw new ArgumentException("Unicode string lists must have a languages attribute." + PositionInfo(reader));
			int languages = ParseInt(reader.Value);

			visitor.VisitUnicList(name, offset, visible, languages, pluginLine, tooltip);
		}

		private static string PositionInfo(XmlReader reader)
		{
			var info = reader as IXmlLineInfo;
			return info != null ? string.Format(" Line {0}, position {1}.", info.LineNumber, info.LinePosition) : "";
		}

		private static int ParseInt(string str)
		{
			if (str.StartsWith("0x"))
				return int.Parse(str.Substring(2), NumberStyles.HexNumber);
			if (str.StartsWith("-0x"))
				return -int.Parse(str.Substring(3), NumberStyles.HexNumber);
			return int.Parse(str);
		}

		private static uint ParseUInt(string str)
		{
			return str.StartsWith("0x") ? uint.Parse(str.Substring(2), NumberStyles.HexNumber) : uint.Parse(str);
		}

		private static bool ParseBool(string str)
		{
			return (str == "1" || str.ToLower() == "true");
		}
	}
}