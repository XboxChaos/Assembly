using System.Globalization;
using System.Xml;
using Blamite.Blam.Shaders;

namespace Blamite.Plugins
{
	public class AscensionPluginWriter : IPluginVisitor
	{
		private readonly string _groupName;
		private readonly XmlWriter _output;

		public AscensionPluginWriter(XmlWriter output, string groupName)
		{
			_output = output;
			_groupName = groupName;
		}

		public bool EnterPlugin(int baseSize)
		{
			_output.WriteStartDocument();
			_output.WriteStartElement("plugin");
			_output.WriteAttributeString("class", _groupName);
			_output.WriteAttributeString("headersize", baseSize.ToString(CultureInfo.InvariantCulture));
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
		}

		public void VisitComment(string title, string text, uint pluginLine)
		{
			_output.WriteStartElement("comment");
			_output.WriteAttributeString("title", title);
			_output.WriteAttributeString("visible", "True"); // Oops, Assembly doesn't store this yet...
			_output.WriteString(text);
			_output.WriteEndElement();
		}

		public void VisitUInt8(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("uint8", name, offset, visible);
		}

		public void VisitInt8(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("int8", name, offset, visible);
		}

		public void VisitUInt16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("uint16", name, offset, visible);
		}

		public void VisitInt16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("int16", name, offset, visible);
		}

		public void VisitUInt32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("uint32", name, offset, visible);
		}

		public void VisitInt32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("int32", name, offset, visible);
		}

		public void VisitUInt64(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("uint32", name + " a", offset, visible);
			WriteBasicValue("uint32", name + " b", offset, visible);
		}

		public void VisitInt64(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("int32", name + " a", offset, visible);
			WriteBasicValue("int32", name + " b", offset, visible);
		}

		public void VisitFloat32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("float", name, offset, visible);
		}

		public void VisitUndefined(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("undefined", name, offset, visible);
		}

		public void VisitPoint2(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("float", name + " x", offset, visible);
			WriteBasicValue("float", name + " y", offset + 4, visible);
		}

		public void VisitPoint3(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("float", name + " x", offset, visible);
			WriteBasicValue("float", name + " y", offset + 4, visible);
			WriteBasicValue("float", name + " z", offset + 8, visible);
		}

		public void VisitVector2(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("float", name + " i", offset, visible);
			WriteBasicValue("float", name + " j", offset + 4, visible);
		}

		public void VisitVector3(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("float", name + " i", offset, visible);
			WriteBasicValue("float", name + " j", offset + 4, visible);
			WriteBasicValue("float", name + " k", offset + 8, visible);
		}

		public void VisitVector4(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("float", name + " i", offset, visible);
			WriteBasicValue("float", name + " j", offset + 4, visible);
			WriteBasicValue("float", name + " k", offset + 8, visible);
			WriteBasicValue("float", name + " w", offset + 12, visible);
		}

		public void VisitDegree(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("float", name, offset, visible);
		}

		public void VisitDegree2(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("float", name + " y", offset, visible);
			WriteBasicValue("float", name + " p", offset + 4, visible);
		}

		public void VisitDegree3(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("float", name + " y", offset, visible);
			WriteBasicValue("float", name + " p", offset + 4, visible);
			WriteBasicValue("float", name + " r", offset + 8, visible);
		}

		public void VisitPlane2(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("float", name + " i", offset, visible);
			WriteBasicValue("float", name + " j", offset + 4, visible);
			WriteBasicValue("float", name + " d", offset + 8, visible);
		}

		public void VisitPlane3(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("float", name + " i", offset, visible);
			WriteBasicValue("float", name + " j", offset + 4, visible);
			WriteBasicValue("float", name + " k", offset + 8, visible);
			WriteBasicValue("float", name + " d", offset + 12, visible);
		}

		public void VisitRect16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("int16", name + " t", offset, visible);
			WriteBasicValue("int16", name + " l", offset + 2, visible);
			WriteBasicValue("int16", name + " b", offset + 4, visible);
			WriteBasicValue("int16", name + " r", offset + 6, visible);
		}

		public void VisitQuat16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("int16", name + " i", offset, visible);
			WriteBasicValue("int16", name + " j", offset + 2, visible);
			WriteBasicValue("int16", name + " k", offset + 4, visible);
			WriteBasicValue("int16", name + " d", offset + 6, visible);
		}

		public void VisitPoint16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("int16", name + " x", offset, visible);
			WriteBasicValue("int16", name + " y", offset + 2, visible);
		}

		public void VisitStringID(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("stringid", name, offset, visible);
		}

		public void VisitTagReference(string name, uint offset, bool visible, bool withGroup, uint pluginLine, string tooltip)
		{
			if (!withGroup)
				WriteBasicValue("uint32", name + " Tag ID", offset, visible);
			else
				WriteBasicValue("tagref", name, offset, visible);
		}

		public void VisitDataReference(string name, uint offset, string format, bool visible, int align, uint pluginLine, string tooltip)
		{
			WriteValueStart("tagdata", name, offset, visible);
			_output.WriteAttributeString("format", format);
			_output.WriteEndElement();
		}

		public void VisitRawData(string name, uint offset, bool visible, int size, uint pluginLine, string tooltip)
		{
			WriteValueStart("bytearray", name, offset, visible);
			_output.WriteAttributeString("length", size.ToString(CultureInfo.InvariantCulture));
			_output.WriteEndElement();
		}

		public void VisitAscii(string name, uint offset, bool visible, int length, uint pluginLine, string tooltip)
		{
			WriteValueStart("string", name, offset, visible);
			_output.WriteAttributeString("length", length.ToString(CultureInfo.InvariantCulture));
			_output.WriteEndElement();
		}

		public void VisitUtf16(string name, uint offset, bool visible, int length, uint pluginLine, string tooltip)
		{
			// TODO: does Ascension support this?
		}

		public void VisitHexString(string name, uint offset, bool visible, int length, uint pluginLine, string tooltip)
		{
			WriteValueStart("bytearray", name, offset, visible);
			_output.WriteAttributeString("length", length.ToString(CultureInfo.InvariantCulture));
			_output.WriteEndElement();
		}

		public void VisitColorInt(string name, uint offset, bool visible, bool alpha, uint pluginLine, string tooltip)
		{
			WriteValueStart("color8", name, offset, visible);
			_output.WriteAttributeString("order", alpha ? "ARGB" : "RGB");
			_output.WriteAttributeString("real", "True"); // What does this do?
			_output.WriteEndElement();
		}

		public void VisitColorF(string name, uint offset, bool visible, bool alpha, bool basic, uint pluginLine, string tooltip)
		{
			WriteValueStart("colorf", name, offset, visible);
			_output.WriteAttributeString("order", alpha ? "ARGB" : "RGB");
			_output.WriteAttributeString("real", "True"); // What does this do?
			_output.WriteEndElement();
		}

		public bool EnterFlags8(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("bitmask8", name, offset, visible);
			return true;
		}

		public bool EnterFlags16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("bitmask16", name, offset, visible);
			return true;
		}

		public bool EnterFlags32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("bitmask32", name, offset, visible);
			return true;
		}

		public bool EnterFlags64(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			//WriteValueStart("bitmask64", name, offset, visible);
			return false;
		}

		public void VisitBit(string name, int index, string tooltip)
		{
			_output.WriteStartElement("option");
			_output.WriteAttributeString("name", name);
			_output.WriteAttributeString("value", index.ToString(CultureInfo.InvariantCulture));
			_output.WriteEndElement();
		}

		public void LeaveFlags()
		{
			_output.WriteEndElement();
		}

		public bool EnterEnum8(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("enum8", name, offset, visible);
			return true;
		}

		public bool EnterEnum16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("enum16", name, offset, visible);
			return true;
		}

		public bool EnterEnum32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("enum32", name, offset, visible);
			return true;
		}

		public void VisitOption(string name, int value, string tooltip)
		{
			_output.WriteStartElement("option");
			_output.WriteAttributeString("name", name);
			_output.WriteAttributeString("value", value.ToString(CultureInfo.InvariantCulture));
			_output.WriteEndElement();
		}

		public void LeaveEnum()
		{
			_output.WriteEndElement();
		}

		public bool EnterTagBlock(string name, uint offset, bool visible, uint elementSize, int align, bool sort, uint pluginLine, string tooltip)
		{
			WriteValueStart("struct", name, offset, visible);
			_output.WriteAttributeString("size", elementSize.ToString(CultureInfo.InvariantCulture));
			return true;
		}

		public void LeaveTagBlock()
		{
			_output.WriteEndElement();
		}

		public void VisitShader(string name, uint offset, bool visible, ShaderType type, uint pluginLine, string tooltip)
		{
			WriteBasicValue("uint32", name, offset, visible);
		}

		public void VisitRangeInt16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("int16", name + " min", offset, visible);
			WriteBasicValue("int16", name + " max", offset + 2, visible);
		}

		public void VisitRangeFloat32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("float", name + " min", offset, visible);
			WriteBasicValue("float", name + " max", offset + 4, visible);
		}

		public void VisitRangeDegree(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("float", name + " min", offset, visible);
			WriteBasicValue("float", name + " max", offset + 4, visible);
		}

		public void VisitUnicList(string name, uint offset, bool visible, int languages, uint pluginLine, string tooltip)
		{
			for (var i = 0; i < languages; i++)
			{
				WriteBasicValue("uint16", "Language " + i + " " + name + " Index", (uint)(offset + i * 4), visible);
				WriteBasicValue("uint16", "Language " + i + " " + name + " Count", (uint)(offset + i * 4 + 2), visible);
			}
		}

		public void VisitDatum(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteBasicValue("uint32", name, offset, visible);
		}

		public void VisitOldStringID(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			WriteValueStart("string", name, offset, visible);
			_output.WriteAttributeString("length", "0x1C");
			_output.WriteEndElement();

			WriteBasicValue("stringid", name, offset + 0x1C, visible);
		}

		private void WriteValueStart(string element, string name, uint offset, bool visible)
		{
			_output.WriteStartElement(element);
			_output.WriteAttributeString("name", name);
			_output.WriteAttributeString("offset", offset.ToString(CultureInfo.InvariantCulture));
			_output.WriteAttributeString("visible", visible.ToString());
		}

		private void WriteBasicValue(string element, string name, uint offset, bool visible)
		{
			WriteValueStart(element, name, offset, visible);
			_output.WriteEndElement();
		}
	}
}