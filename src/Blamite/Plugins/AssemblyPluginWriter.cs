using System.Globalization;
using System.Xml;
using Blamite.Blam.Shaders;

namespace Blamite.Plugins
{
	public class AssemblyPluginWriter : IPluginVisitor
	{
		private readonly string _game;
		private readonly XmlWriter _output;

		public AssemblyPluginWriter(XmlWriter output, string game)
		{
			_output = output;
			_game = game;
		}

		public bool EnterPlugin(int baseSize)
		{
			_output.WriteStartDocument();
			_output.WriteStartElement("plugin");
			_output.WriteAttributeString("game", _game);
			_output.WriteAttributeString("baseSize", ToHexString(baseSize));
			_output.WriteComment(" Automatically generated plugin ");
			return true;
		}

		public void LeavePlugin()
		{
			_output.WriteEndElement();
			_output.WriteEndDocument();
		}

		public bool EnterRevisions()
		{
			_output.WriteStartElement("revisions");
			return true;
		}

		public void VisitRevision(PluginRevision revision)
		{
			_output.WriteStartElement("revision");
			_output.WriteAttributeString("author", revision.Researcher);
			_output.WriteAttributeString("version", revision.Version.ToString(CultureInfo.InvariantCulture));
			_output.WriteString(revision.Description);
			_output.WriteEndElement();
		}

		public void LeaveRevisions()
		{
			_output.WriteEndElement();
		}

		public void VisitComment(string title, string text, uint pluginLine)
		{
			_output.WriteStartElement("comment");
			_output.WriteAttributeString("title", title);
			_output.WriteString(text);
			_output.WriteEndElement();
		}

		public void VisitUInt8(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("uint8", name, offset, visible);
		}

		public void VisitInt8(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("int8", name, offset, visible);
		}

		public void VisitUInt16(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("uint16", name, offset, visible);
		}

		public void VisitInt16(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("int16", name, offset, visible);
		}

		public void VisitUInt32(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("uint32", name, offset, visible);
		}

		public void VisitInt32(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("int32", name, offset, visible);
		}

		public void VisitFloat32(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("float32", name, offset, visible);
		}

		public void VisitUndefined(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("undefined", name, offset, visible);
		}

		public void VisitPoint2(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteValueStart("point2", name, offset, visible);
		}

		public void VisitPoint3(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteValueStart("point3", name, offset, visible);
		}

		public void VisitVector2(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteValueStart("vector2", name, offset, visible);
		}

		public void VisitVector3(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteValueStart("vector3", name, offset, visible);
		}

		public void VisitVector4(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("vector4", name, offset, visible);
		}

		public void VisitDegree(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("degree", name, offset, visible);
		}

		public void VisitDegree2(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("degree2", name, offset, visible);
		}

		public void VisitDegree3(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("degree3", name, offset, visible);
		}

		public void VisitPlane2(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteValueStart("plane2", name, offset, visible);
		}

		public void VisitPlane3(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("plane3", name, offset, visible);
		}

		public void VisitRect16(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("rect16", name, offset, visible);
		}

		public void VisitStringID(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("stringId", name, offset, visible);
		}

		public void VisitTagReference(string name, uint offset, bool visible, bool withClass, bool showJumpTo, uint pluginLine)
		{
			WriteValueStart("tagRef", name, offset, visible);
			if (!withClass)
				_output.WriteAttributeString("withClass", "false");
			if (!showJumpTo)
				_output.WriteAttributeString("showJumpTo", "false");
			_output.WriteEndElement();
		}

		public void VisitAscii(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			WriteValueStart("ascii", name, offset, visible);
			_output.WriteAttributeString("size", ToHexString(size));
			_output.WriteEndElement();
		}

		public void VisitUtf16(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			WriteValueStart("utf16", name, offset, visible);
			_output.WriteAttributeString("size", ToHexString(size));
			_output.WriteEndElement();
		}

		public void VisitDataReference(string name, uint offset, string format, bool visible, int align, uint pluginLine)
		{
			WriteValueStart("dataRef", name, offset, visible);
			//_output.WriteAttributeString("format", format);
			if (align != 4)
				_output.WriteAttributeString("align", ToHexString(align));
			_output.WriteEndElement();
		}

		public void VisitRawData(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			WriteValueStart("raw", name, offset, visible);
			_output.WriteAttributeString("size", ToHexString(size));
			_output.WriteEndElement();
		}

		public void VisitColorInt(string name, uint offset, bool visible, string format, uint pluginLine)
		{
			string element;
			switch (format.Length)
			{
				case 1:
					element = "color8";
					break;
				case 2:
					element = "color16";
					break;
				case 3:
					element = "color24";
					break;
				default:
					element = "color32";
					break;
			}

			WriteValueStart(element, name, offset, visible);
			_output.WriteAttributeString("format", format);
			_output.WriteEndElement();
		}

		public void VisitColorF(string name, uint offset, bool visible, string format, uint pluginLine)
		{
			WriteValueStart("colorf", name, offset, visible);
			_output.WriteAttributeString("format", format);
			_output.WriteEndElement();
		}

		public bool EnterBitfield8(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteValueStart("bitfield8", name, offset, visible);
			return true;
		}

		public bool EnterBitfield16(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteValueStart("bitfield16", name, offset, visible);
			return true;
		}

		public bool EnterBitfield32(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteValueStart("bitfield32", name, offset, visible);
			return true;
		}

		public void VisitBit(string name, int index)
		{
			_output.WriteStartElement("bit");
			_output.WriteAttributeString("name", name);
			_output.WriteAttributeString("index", index.ToString(CultureInfo.InvariantCulture));
			_output.WriteEndElement();
		}

		public void LeaveBitfield()
		{
			_output.WriteEndElement();
		}

		public bool EnterEnum8(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteValueStart("enum8", name, offset, visible);
			return true;
		}

		public bool EnterEnum16(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteValueStart("enum16", name, offset, visible);
			return true;
		}

		public bool EnterEnum32(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteValueStart("enum32", name, offset, visible);
			return true;
		}

		public void VisitOption(string name, int value)
		{
			_output.WriteStartElement("option");
			_output.WriteAttributeString("name", name);
			_output.WriteAttributeString("value", ToHexString(value));
			_output.WriteEndElement();
		}

		public void LeaveEnum()
		{
			_output.WriteEndElement();
		}

		public bool EnterReflexive(string name, uint offset, bool visible, uint entrySize, int align, bool sort, uint pluginLine)
		{
			WriteValueStart("reflexive", name, offset, visible);
			_output.WriteAttributeString("entrySize", ToHexString(entrySize));
			if (align != 4)
				_output.WriteAttributeString("align", ToHexString(align));
			if (sort == true)
				_output.WriteAttributeString("sort", sort.ToString().ToLower());
			return true;
		}

		public void LeaveReflexive()
		{
			_output.WriteEndElement();
		}

		public void VisitShader(string name, uint offset, bool visible, ShaderType type, uint pluginLine)
		{
			WriteValueStart("shader", name, offset, visible);
			_output.WriteAttributeString("type", (type == ShaderType.Pixel) ? "pixel" : "vertex");
			_output.WriteEndElement();
		}

		public void VisitRangeUInt16(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("range16", name, offset, visible);
		}

		public void VisitRangeFloat32(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("rangeF", name, offset, visible);
		}

		public void VisitRangeDegree(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("rangeD", name, offset, visible);
		}

		public void VisitUnicList(string name, uint offset, bool visible, int languages, uint pluginLine)
		{
			WriteValueStart("unicList", name, offset ,visible);
			_output.WriteAttributeString("languages", languages.ToString());
			_output.WriteEndElement();
		}

		private void WriteValueStart(string element, string name, uint offset, bool visible)
		{
			_output.WriteStartElement(element);
			_output.WriteAttributeString("name", name);
			_output.WriteAttributeString("offset", ToHexString(offset));
			_output.WriteAttributeString("visible", visible.ToString().ToLower());
		}

		private void WriteBasicValue(string element, string name, uint offset, bool visible)
		{
			WriteValueStart(element, name, offset, visible);
			_output.WriteEndElement();
		}

		private static string ToHexString(int num)
		{
			return "0x" + num.ToString("X");
		}

		private static string ToHexString(uint num)
		{
			return "0x" + num.ToString("X");
		}
	}
}