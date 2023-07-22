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

		public void VisitUInt8(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("uint8", name, offset, visible, tooltip);
		}

		public void VisitInt8(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("int8", name, offset, visible, tooltip);
		}

		public void VisitUInt16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("uint16", name, offset, visible, tooltip);
		}

		public void VisitInt16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("int16", name, offset, visible, tooltip);
		}

		public void VisitUInt32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("uint32", name, offset, visible, tooltip);
		}

		public void VisitInt32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("int32", name, offset, visible, tooltip);
		}

		public void VisitUInt64(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("uint64", name, offset, visible, tooltip);
		}

		public void VisitInt64(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("int64", name, offset, visible, tooltip);
		}

		public void VisitFloat32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("float32", name, offset, visible, tooltip);
		}

		public void VisitUndefined(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("undefined", name, offset, visible, tooltip);
		}

		public void VisitPoint2(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("point2", name, offset, visible, tooltip);
		}

		public void VisitPoint3(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("point3", name, offset, visible, tooltip);
		}

		public void VisitVector2(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("vector2", name, offset, visible, tooltip);
		}

		public void VisitVector3(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("vector3", name, offset, visible, tooltip);
		}

		public void VisitVector4(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("vector4", name, offset, visible, tooltip);
		}

		public void VisitDegree(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("degree", name, offset, visible, tooltip);
		}

		public void VisitDegree2(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("degree2", name, offset, visible, tooltip);
		}

		public void VisitDegree3(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("degree3", name, offset, visible, tooltip);
		}

		public void VisitPlane2(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("plane2", name, offset, visible, tooltip);
		}

		public void VisitPlane3(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("plane3", name, offset, visible, tooltip);
		}

		public void VisitRect16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("rect16", name, offset, visible, tooltip);
		}

		public void VisitQuat16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("quat16", name, offset, visible, tooltip);
		}

		public void VisitPoint16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("point16", name, offset, visible, tooltip);
		}

		public void VisitStringID(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("stringid", name, offset, visible, tooltip);
		}

		public void VisitTagReference(string name, uint offset, bool visible, bool withGroup, uint pluginLine, string tooltip)
		{
			WriteValueStart("tagRef", name, offset, visible, tooltip);
			if (!withGroup)
				_output.WriteAttributeString("withGroup", "false");
			_output.WriteEndElement();
		}

		public void VisitAscii(string name, uint offset, bool visible, int size, uint pluginLine, string tooltip)
		{
			WriteValueStart("ascii", name, offset, visible, tooltip);
			_output.WriteAttributeString("size", ToHexString(size));
			_output.WriteEndElement();
		}

		public void VisitUtf16(string name, uint offset, bool visible, int size, uint pluginLine, string tooltip)
		{
			WriteValueStart("utf16", name, offset, visible, tooltip);
			_output.WriteAttributeString("size", ToHexString(size));
			_output.WriteEndElement();
		}

		public void VisitHexString(string name, uint offset, bool visible, int size, uint pluginLine, string tooltip)
		{
			WriteValueStart("hexstring", name, offset, visible, tooltip);
			_output.WriteAttributeString("size", ToHexString(size));
			_output.WriteEndElement();
		}

		public void VisitDataReference(string name, uint offset, string format, bool visible, int align, uint pluginLine, string tooltip)
		{
			WriteValueStart("dataRef", name, offset, visible, tooltip);
			//_output.WriteAttributeString("format", format);
			if (align != 4)
				_output.WriteAttributeString("align", ToHexString(align));
			_output.WriteEndElement();
		}

		public void VisitRawData(string name, uint offset, bool visible, int size, uint pluginLine, string tooltip)
		{
			WriteValueStart("raw", name, offset, visible, tooltip);
			_output.WriteAttributeString("size", ToHexString(size));
			_output.WriteEndElement();
		}

		public void VisitColorInt(string name, uint offset, bool visible, bool alpha, uint pluginLine, string tooltip)
		{
			string element = "color32";

			WriteValueStart(element, name, offset, visible, tooltip);
			_output.WriteAttributeString("alpha", alpha.ToString().ToLowerInvariant());
			_output.WriteEndElement();
		}

		public void VisitColorF(string name, uint offset, bool visible, bool alpha, bool basic, uint pluginLine, string tooltip)
		{
			WriteValueStart("colorf", name, offset, visible, tooltip);
			_output.WriteAttributeString("alpha", alpha.ToString().ToLowerInvariant());
			_output.WriteAttributeString("basic", basic.ToString().ToLowerInvariant());
			_output.WriteEndElement();
		}

		public bool EnterFlags8(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("flags8", name, offset, visible, tooltip);
			return true;
		}

		public bool EnterFlags16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("flags16", name, offset, visible, tooltip);
			return true;
		}

		public bool EnterFlags32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("flags32", name, offset, visible, tooltip);
			return true;
		}

		public bool EnterFlags64(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("flags64", name, offset, visible, tooltip);
			return true;
		}

		public void VisitBit(string name, int index, string tooltip)
		{
			_output.WriteStartElement("bit");
			_output.WriteAttributeString("name", name);
			_output.WriteAttributeString("index", index.ToString(CultureInfo.InvariantCulture));
			if (!string.IsNullOrEmpty(tooltip))
				_output.WriteAttributeString("tooltip", tooltip);
			_output.WriteEndElement();
		}

		public void LeaveFlags()
		{
			_output.WriteEndElement();
		}

		public bool EnterEnum8(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("enum8", name, offset, visible, tooltip);
			return true;
		}

		public bool EnterEnum16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("enum16", name, offset, visible, tooltip);
			return true;
		}

		public bool EnterEnum32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("enum32", name, offset, visible, tooltip);
			return true;
		}

		public void VisitOption(string name, int value, string tooltip)
		{
			_output.WriteStartElement("option");
			_output.WriteAttributeString("name", name);
			_output.WriteAttributeString("value", ToHexString(value));
			if (!string.IsNullOrEmpty(tooltip))
				_output.WriteAttributeString("tooltip", tooltip);
			_output.WriteEndElement();
		}

		public void LeaveEnum()
		{
			_output.WriteEndElement();
		}

		public bool EnterTagBlock(string name, uint offset, bool visible, uint elementSize, int align, bool sort, uint pluginLine, string tooltip)
		{
			WriteValueStart("tagblock", name, offset, visible, tooltip);
			_output.WriteAttributeString("elementSize", ToHexString(elementSize));
			if (align != 4)
				_output.WriteAttributeString("align", ToHexString(align));
			if (sort == true)
				_output.WriteAttributeString("sort", sort.ToString().ToLowerInvariant());
			return true;
		}

		public void LeaveTagBlock()
		{
			_output.WriteEndElement();
		}

		public void VisitShader(string name, uint offset, bool visible, ShaderType type, uint pluginLine, string tooltip)
		{
			WriteValueStart("shader", name, offset, visible, tooltip);
			_output.WriteAttributeString("type", (type == ShaderType.Pixel) ? "pixel" : "vertex");
			_output.WriteEndElement();
		}

		public void VisitRangeInt16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("range16", name, offset, visible, tooltip);
		}

		public void VisitRangeFloat32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("rangeF", name, offset, visible, tooltip);
		}

		public void VisitRangeDegree(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("rangeD", name, offset, visible, tooltip);
		}

		public void VisitUnicList(string name, uint offset, bool visible, int languages, uint pluginLine, string tooltip)
		{
			WriteValueStart("unicList", name, offset, visible, tooltip);
			_output.WriteAttributeString("languages", languages.ToString());
			_output.WriteEndElement();
		}

		public void VisitDatum(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("datum", name, offset, visible, tooltip);
		}

		public void VisitOldStringID(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("oldstringid", name, offset, visible, tooltip);
		}

		private void WriteValueStart(string element, string name, uint offset, bool visible, string tooltip)
		{
			_output.WriteStartElement(element);
			_output.WriteAttributeString("name", name);
			_output.WriteAttributeString("offset", ToHexString(offset));
			_output.WriteAttributeString("visible", visible.ToString().ToLowerInvariant());
			if (!string.IsNullOrEmpty(tooltip))
				_output.WriteAttributeString("tooltip", tooltip);
		}

		private void WriteBasicValue(string element, string name, uint offset, bool visible, string tooltip)
		{
			WriteValueStart(element, name, offset, visible, tooltip);
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