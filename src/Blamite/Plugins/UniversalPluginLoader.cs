using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using Blamite.Blam.Shaders;

namespace Blamite.Plugins
{
	/// <summary>
	///     Modified version of AssemblyPluginLoader that can load plugins from Ascension and Alteration as well.
	/// </summary>
	public class UniversalPluginLoader
	{
		private readonly SortedList<float, PluginRevision> revisions = new SortedList<float, PluginRevision>();
		private int versionBase = 1;

		private UniversalPluginLoader()
		{
		}

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
			if (reader.MoveToAttribute("headersize") || reader.MoveToAttribute("baseSize"))
				baseSize = ParseInt(reader.Value);

			if (visitor.EnterPlugin(baseSize))
			{
				var loader = new UniversalPluginLoader();
				loader.ReadElements(reader, true, visitor);
				visitor.LeavePlugin();
			}
		}

		private void ReadElements(XmlReader reader, bool topLevel, IPluginVisitor visitor)
		{
			while (reader.Read())
			{
				if (reader.NodeType == XmlNodeType.Element)
				{
					string elementName = reader.Name.ToLower();
					if (topLevel)
						HandleTopLevelElement(reader, elementName, visitor);
					else
						HandleElement(reader, elementName, visitor);
				}
			}
		}

		private void HandleTopLevelElement(XmlReader reader, string elementName, IPluginVisitor visitor)
		{
			if (elementName == "revisions")
				ReadRevisions(reader.ReadSubtree());
			else if (elementName == "revision")
				ReadRevision(reader);
			else
				HandleElement(reader, elementName, visitor);
		}

		// Fixes up the version numbers on revisions
		private void DumpRevisions(IPluginVisitor visitor)
		{
			if (visitor.EnterRevisions())
			{
				// Dump ALL the revisions!
				int version = versionBase;
				foreach (var pair in revisions)
				{
					pair.Value.Version = version;
					version++;
					visitor.VisitRevision(pair.Value);
				}
				versionBase = version;

				visitor.LeaveRevisions();
			}
		}

		private void HandleElement(XmlReader reader, string elementName, IPluginVisitor visitor)
		{
			if (revisions.Count > 0)
			{
				DumpRevisions(visitor);
				revisions.Clear();
			}

			switch (elementName)
			{
				case "comment":
					ReadComment(reader, visitor);
					break;
				default:
					HandleValueElement(reader, elementName, visitor);
					break;
			}
		}

		private void ReadComment(XmlReader reader, IPluginVisitor visitor)
		{
			string title = "Comment";

			if (reader.MoveToAttribute("title"))
				title = reader.Value;

			reader.MoveToElement();
			var pluginLine = (uint) (reader as IXmlLineInfo).LineNumber;
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
		private void HandleValueElement(XmlReader reader, string elementName, IPluginVisitor visitor)
		{
			string name = "Unknown";
			uint offset = 0;
			var pluginLine = (uint) (reader as IXmlLineInfo).LineNumber;
			bool visible = true;
			string tooltip = "";

			if (reader.MoveToAttribute("name"))
				name = reader.Value;
			if (reader.MoveToAttribute("offset"))
				offset = ParseUInt(reader.Value);
			if (reader.MoveToAttribute("visible"))
				visible = ParseBool(reader.Value);
			if (reader.MoveToAttribute("tooltip"))
				visible = ParseBool(reader.Value);

			reader.MoveToElement();
			switch (elementName.ToLower())
			{
				case "uint8":
				case "byte":
					visitor.VisitUInt8(name, offset, visible, pluginLine, tooltip);
					break;
				case "sbyte":
				case "int8":
					visitor.VisitInt8(name, offset, visible, pluginLine, tooltip);
					break;
				case "ushort":
				case "uint16":
					visitor.VisitUInt16(name, offset, visible, pluginLine, tooltip);
					break;
				case "short":
				case "int16":
					visitor.VisitInt16(name, offset, visible, pluginLine, tooltip);
					break;
				case "dword":
				case "uint":
				case "uint32":
				case "long":
				case "true":
					visitor.VisitUInt32(name, offset, visible, pluginLine, tooltip);
					break;
				case "int":
				case "int32":
					visitor.VisitInt32(name, offset, visible, pluginLine, tooltip);
					break;
				case "int64":
					visitor.VisitInt64(name, offset, visible, pluginLine, tooltip);
					break;
				case "uint64":
					visitor.VisitUInt64(name, offset, visible, pluginLine, tooltip);
					break;
				case "undefined":
				case "unknown":
					visitor.VisitUndefined(name, offset, visible, pluginLine, tooltip);
					break;
				case "float":
				case "float32":
				case "degree":
					visitor.VisitFloat32(name, offset, visible, pluginLine, tooltip);
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
				case "tag":
				case "tagid":
				case "tagref":
				case "ident":
					ReadTagRef(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "string":
				case "ascii":
					ReadAscii(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;
				case "string32":
					visitor.VisitAscii(name, offset, visible, 32, pluginLine, tooltip);
					break;
				case "string64":
					visitor.VisitAscii(name, offset, visible, 64, pluginLine, tooltip);
					break;
				case "string256":
					visitor.VisitAscii(name, offset, visible, 256, pluginLine, tooltip);
					break;

				case "unicode":
				case "utf16":
					ReadUtf16(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;
				case "unicode32":
					visitor.VisitUtf16(name, offset, visible, 32, pluginLine, tooltip);
					break;
				case "unicode64":
					visitor.VisitUtf16(name, offset, visible, 64, pluginLine, tooltip);
					break;
				case "unicode256":
					visitor.VisitUtf16(name, offset, visible, 256, pluginLine, tooltip);
					break;

				case "flags8":
				case "bitfield8":
				case "bitmask8":
					if (visitor.EnterFlags8(name, offset, visible, pluginLine, tooltip))
						ReadBits(reader, visitor);
					break;
				case "flags16":
				case "bitmask16":
				case "bitfield16":
					if (visitor.EnterFlags16(name, offset, visible, pluginLine, tooltip))
						ReadBits(reader, visitor);
					break;
				case "flags32":
				case "bitmask32":
				case "bitfield32":
					if (visitor.EnterFlags32(name, offset, visible, pluginLine, tooltip))
						ReadBits(reader, visitor);
					break;
				case "flags64":
				case "bitmask64":
				case "bitfield64":
					if (visitor.EnterFlags64(name, offset, visible, pluginLine, tooltip))
						ReadBits(reader, visitor);
					break;

				case "enum8":
					if (visitor.EnterEnum8(name, offset, visible, pluginLine, tooltip))
						ReadOptions(reader, visitor);
					break;
				case "enum16":
					if (visitor.EnterEnum16(name, offset, visible, pluginLine, tooltip))
						ReadOptions(reader, visitor);
					break;
				case "enum32":
					if (visitor.EnterEnum32(name, offset, visible, pluginLine, tooltip))
						ReadOptions(reader, visitor);
					break;

				case "tagdata":
				case "dataref":
					ReadDataRef(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "struct":
				case "reflexive":
				case "reflexives":
				case "tagblock":
					ReadTagBlock(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "bytearray":
				case "raw":
					ReadRaw(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "shader":
					ReadShader(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "uniclist":
					ReadUnicList(reader, name, offset, visible, visitor, pluginLine, tooltip);
					break;

				case "color8":
				case "color16":
					// these should not have been used in Alteration/Ascension
					break;

				case "color24":
					visitor.VisitColorInt(name, offset - 1, visible, false, pluginLine, tooltip);
					break;

				case "color32":
					visitor.VisitColorInt(name, offset, visible, ReadColorAlpha(reader), pluginLine, tooltip);
					break;

				case "colorf":
					visitor.VisitColorF(name, offset, visible, ReadColorAlpha(reader), ReadColorBasicValue(reader), pluginLine, tooltip);
					break;

				case "id":
					// Group ID, part of a tag reference
					break;

				case "unused":
				case "unusued":
				case "ununused":
					// Do nothing, I really don't understand the point of this
					break;

				case "datum":
					visitor.VisitDatum(name, offset, visible, pluginLine, tooltip);
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


				default:
					throw new ArgumentException("Unknown element \"" + elementName + "\"." + PositionInfo(reader));
			}
		}

		private void ReadRevisions(XmlReader reader)
		{
			reader.ReadStartElement();
			while (reader.ReadToFollowing("revision"))
				ReadRevision(reader);
		}

		private static void ReadDataRef(XmlReader reader, string name, uint offset, bool visible, IPluginVisitor visitor,
			uint pluginLine, string tooltip)
		{
			string format = "bytes";

			if (reader.MoveToAttribute("format"))
				format = reader.Value;

			if (format != "bytes" &&
				format != "asciiz")
				throw new ArgumentException("Invalid format. Must be either `bytes` or `asciiz`.");

			int align = 4;
			if (reader.MoveToAttribute("align"))
				align = ParseInt(reader.Value);

			visitor.VisitDataReference(name, offset, format, visible, align, pluginLine, tooltip);
		}

		private void ReadRevision(XmlReader reader)
		{
			string author = "", description;
			float version = 1;

			if (reader.MoveToAttribute("author"))
				author = reader.Value;

			if (reader.MoveToAttribute("version"))
			{
				version = float.Parse(reader.Value.TrimEnd('a', 'b', 'c'));

				// Handle a letter suffix (some Entity plugins use this)
				char suffix = reader.Value[reader.Value.Length - 1];
				if (suffix >= 'a' && suffix <= 'c')
					version += (suffix - 'a' + 1)*0.001f;
			}

			reader.MoveToElement();
			description = reader.ReadElementContentAsString();

			var revision = new PluginRevision(author, (int) version, description);
			revisions[version] = revision;
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
			int size = 0;

			if (reader.MoveToAttribute("size") || reader.MoveToAttribute("length"))
				size = ParseInt(reader.Value);

			visitor.VisitAscii(name, offset, visible, size, pluginLine, tooltip);
		}

		private static void ReadUtf16(XmlReader reader, string name, uint offset, bool visible, IPluginVisitor visitor,
			uint pluginLine, string tooltip)
		{
			int size = 0;

			if (reader.MoveToAttribute("size") || reader.MoveToAttribute("length"))
				size = ParseInt(reader.Value);

			visitor.VisitUtf16(name, offset, visible, size, pluginLine, tooltip);
		}

		private static void ReadBits(XmlReader reader, IPluginVisitor visitor)
		{
			XmlReader subtree = reader.ReadSubtree();

			subtree.ReadStartElement();
			while (subtree.Read())
			{
				if (subtree.NodeType == XmlNodeType.Element)
				{
					if (subtree.Name == "bit" || subtree.Name == "option")
						ReadBit(subtree, visitor);
					else
						throw new ArgumentException("Unknown bit definition tag: " + subtree.Name + PositionInfo(reader));
				}
			}

			visitor.LeaveFlags();
		}

		private static void ReadBit(XmlReader reader, IPluginVisitor visitor)
		{
			string name = "Unknown";
			int index = 0;
			string tooltip = "";

			if (reader.MoveToAttribute("name"))
				name = reader.Value;
			if (reader.MoveToAttribute("tooltip"))
				tooltip = reader.Value;
			if (!reader.MoveToAttribute("index") && !reader.MoveToAttribute("value"))
				throw new ArgumentException("Bit definitions must have an index" + PositionInfo(reader));
			index = ParseInt(reader.Value);

			visitor.VisitBit(name, index, tooltip);
		}

		private static void ReadRaw(XmlReader reader, string name, uint offset, bool visible, IPluginVisitor visitor,
			uint pluginLine, string tooltip)
		{
			int size;

			if (!reader.MoveToAttribute("size") && !reader.MoveToAttribute("length"))
				throw new ArgumentException("Raw data blocks must have a size or length attribute." + PositionInfo(reader));
			size = ParseInt(reader.Value);

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
			if (reader.MoveToAttribute("tooltip"))
				tooltip = reader.Value;
			if (reader.MoveToAttribute("value"))
				value = ParseInt(reader.Value);

			visitor.VisitOption(name, value, tooltip);
		}

		private static bool ReadColorAlpha(XmlReader reader)
		{
			string format;

			if (!reader.MoveToAttribute("alpha") && !reader.MoveToAttribute("format") && !reader.MoveToAttribute("order"))
				throw new ArgumentException("Color tags must have a alpha, format, or order attribute." + PositionInfo(reader));
			format = reader.Value.ToLower();

			if (format.ToLower() == "true")
				return true;
			else if (format.ToLower() == "false")
				return false;
			else
				return format.ToLower().Contains("a");
		}

		private static bool ReadColorBasicValue(XmlReader reader)
		{
			if (reader.MoveToAttribute("basic"))
				return reader.Value.ToLower() == "true";

			return false;
		}

		private void ReadTagBlock(XmlReader reader, string name, uint offset, bool visible, IPluginVisitor visitor,
			uint pluginLine, string tooltip)
		{
			uint entrySize = 0;

			if (reader.MoveToAttribute("entrySize") || reader.MoveToAttribute("size") || reader.MoveToAttribute("elementSize"))
			{
				if (!string.IsNullOrWhiteSpace(reader.Value))
					entrySize = ParseUInt(reader.Value);
			}

			int align = 4;
			if (reader.MoveToAttribute("align"))
				align = ParseInt(reader.Value);
			bool sort = false;
			if (reader.MoveToAttribute("sort"))
				sort = ParseBool(reader.Value);

			if (visitor.EnterTagBlock(name, offset, visible, entrySize, align, sort, pluginLine, tooltip))
			{
				reader.MoveToElement();
				XmlReader subtree = reader.ReadSubtree();

				subtree.ReadStartElement();
				ReadElements(subtree, false, visitor);
				visitor.LeaveTagBlock();
			}
		}

		private static string PositionInfo(XmlReader reader)
		{
			var info = reader as IXmlLineInfo;
			if (info != null)
				return string.Format(" Line {0}, position {1}.", info.LineNumber, info.LinePosition);
			return "";
		}

		private static int ParseInt(string str)
		{
			if (str.StartsWith("0x"))
				return int.Parse(str.Substring(2), NumberStyles.HexNumber);
			return int.Parse(str);
		}

		private static uint ParseUInt(string str)
		{
			if (str.StartsWith("0x"))
				return uint.Parse(str.Substring(2), NumberStyles.HexNumber);
			return uint.Parse(str);
		}

		private static bool ParseBool(string str)
		{
			return (str == "1" || str.ToLower() == "true");
		}
	}
}