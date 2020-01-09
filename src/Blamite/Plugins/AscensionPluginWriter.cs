using System.Globalization;
using System.Xml;
using Blamite.Blam.Shaders;

namespace Blamite.Plugins
{
	public class AscensionPluginWriter : IPluginVisitor
	{
		private readonly string _className;
		private readonly XmlWriter _output;

		public AscensionPluginWriter(XmlWriter output, string className)
		{
			_output = output;
			_className = className;
		}

		public bool EnterPlugin(int baseSize)
		{
			_output.WriteStartDocument();
			_output.WriteStartElement("plugin");
			_output.WriteAttributeString("class", _className);
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
			WriteBasicValue("float", name, offset, visible);
		}

		public void VisitUndefined(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("undefined", name, offset, visible);
		}

		public void VisitPoint2(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("float", name + " x", offset, visible);
			WriteBasicValue("float", name + " y", offset + 4, visible);
		}

		public void VisitPoint3(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("float", name + " x", offset, visible);
			WriteBasicValue("float", name + " y", offset + 4, visible);
			WriteBasicValue("float", name + " z", offset + 8, visible);
		}

		public void VisitVector2(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("float", name + " i", offset, visible);
			WriteBasicValue("float", name + " j", offset + 4, visible);
		}

		public void VisitVector3(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("float", name + " i", offset, visible);
			WriteBasicValue("float", name + " j", offset + 4, visible);
			WriteBasicValue("float", name + " k", offset + 8, visible);
		}

		public void VisitVector4(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("float", name + " i", offset, visible);
			WriteBasicValue("float", name + " j", offset + 4, visible);
			WriteBasicValue("float", name + " k", offset + 8, visible);
			WriteBasicValue("float", name + " w", offset + 12, visible);
		}

		public void VisitDegree(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("float", name, offset, visible);
		}

		public void VisitDegree2(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("float", name + " y", offset, visible);
			WriteBasicValue("float", name + " p", offset + 4, visible);
		}

		public void VisitDegree3(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("float", name + " y", offset, visible);
			WriteBasicValue("float", name + " p", offset + 4, visible);
			WriteBasicValue("float", name + " r", offset + 8, visible);
		}

		public void VisitPlane2(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("float", name + " i", offset, visible);
			WriteBasicValue("float", name + " j", offset + 4, visible);
			WriteBasicValue("float", name + " d", offset + 8, visible);
		}

		public void VisitPlane3(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("float", name + " i", offset, visible);
			WriteBasicValue("float", name + " j", offset + 4, visible);
			WriteBasicValue("float", name + " k", offset + 8, visible);
			WriteBasicValue("float", name + " d", offset + 12, visible);
		}

		public void VisitRect16(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("int16", name + " t", offset, visible);
			WriteBasicValue("int16", name + " l", offset + 2, visible);
			WriteBasicValue("int16", name + " b", offset + 4, visible);
			WriteBasicValue("int16", name + " r", offset + 6, visible);
		}

		public void VisitStringID(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("stringid", name, offset, visible);
		}

		public void VisitTagReference(string name, uint offset, bool visible, bool withClass, bool showJumpTo, uint pluginLine)
		{
			if (!withClass)
				WriteBasicValue("uint32", name + " Tag ID", offset, visible);
			else
				WriteBasicValue("tagref", name, offset, visible);
		}

		public void VisitDataReference(string name, uint offset, string format, bool visible, int align, uint pluginLine)
		{
			WriteValueStart("tagdata", name, offset, visible);
			_output.WriteAttributeString("format", format);
			_output.WriteEndElement();
		}

		public void VisitRawData(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			WriteValueStart("bytearray", name, offset, visible);
			_output.WriteAttributeString("length", size.ToString(CultureInfo.InvariantCulture));
			_output.WriteEndElement();
		}

		public void VisitAscii(string name, uint offset, bool visible, int length, uint pluginLine)
		{
			WriteValueStart("string", name, offset, visible);
			_output.WriteAttributeString("length", length.ToString(CultureInfo.InvariantCulture));
			_output.WriteEndElement();
		}

		public void VisitUtf16(string name, uint offset, bool visible, int length, uint pluginLine)
		{
			// TODO: does Ascension support this?
		}

		public void VisitColorInt(string name, uint offset, bool visible, bool alpha, uint pluginLine)
		{
			WriteValueStart("color8", name, offset, visible);
			_output.WriteAttributeString("order", alpha ? "ARGB" : "RGB");
			_output.WriteAttributeString("real", "True"); // What does this do?
			_output.WriteEndElement();
		}

		public void VisitColorF(string name, uint offset, bool visible, bool alpha, uint pluginLine)
		{
			WriteValueStart("colorf", name, offset, visible);
			_output.WriteAttributeString("order", alpha ? "ARGB" : "RGB");
			_output.WriteAttributeString("real", "True"); // What does this do?
			_output.WriteEndElement();
		}

		public bool EnterBitfield8(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteValueStart("bitmask8", name, offset, visible);
			return true;
		}

		public bool EnterBitfield16(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteValueStart("bitmask16", name, offset, visible);
			return true;
		}

		public bool EnterBitfield32(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteValueStart("bitmask32", name, offset, visible);
			return true;
		}

		public bool EnterBitfield64(string name, uint offset, bool visible, uint pluginLine)
		{
			//WriteValueStart("bitmask64", name, offset, visible);
			return false;
		}

		public void VisitBit(string name, int index)
		{
			_output.WriteStartElement("option");
			_output.WriteAttributeString("name", name);
			_output.WriteAttributeString("value", index.ToString(CultureInfo.InvariantCulture));
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
			_output.WriteAttributeString("value", value.ToString(CultureInfo.InvariantCulture));
			_output.WriteEndElement();
		}

		public void LeaveEnum()
		{
			_output.WriteEndElement();
		}

		public bool EnterReflexive(string name, uint offset, bool visible, uint entrySize, int align, bool sort, uint pluginLine)
		{
			WriteValueStart("struct", name, offset, visible);
			_output.WriteAttributeString("size", entrySize.ToString(CultureInfo.InvariantCulture));
			return true;
		}

		public void LeaveReflexive()
		{
			_output.WriteEndElement();
		}

		public void VisitShader(string name, uint offset, bool visible, ShaderType type, uint pluginLine)
		{
			WriteBasicValue("uint32", name, offset, visible);
		}

		public void VisitRangeUInt16(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("uint16", name + " min", offset, visible);
			WriteBasicValue("uint16", name + " max", offset + 4, visible);
		}

		public void VisitRangeFloat32(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("float", name + " min", offset, visible);
			WriteBasicValue("float", name + " max", offset + 4, visible);
		}

		public void VisitRangeDegree(string name, uint offset, bool visible, uint pluginLine)
		{
			WriteBasicValue("float", name + " min", offset, visible);
			WriteBasicValue("float", name + " max", offset + 4, visible);
		}

		public void VisitUnicList(string name, uint offset, bool visible, int languages, uint pluginLine)
		{
			for (var i = 0; i < languages; i++)
			{
				WriteBasicValue("uint16", "Language " + i + " " + name + " Index", (uint)(offset + i * 4), visible);
				WriteBasicValue("uint16", "Language " + i + " " + name + " Count", (uint)(offset + i * 4 + 2), visible);
			}
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