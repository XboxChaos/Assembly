using System.Diagnostics;
using System.Text;
using Blamite.Blam.Shaders;

namespace Blamite.Plugins
{
	public class TestPluginVisitor : IPluginVisitor
	{
		private int _level = 0;
		private int _indentSize = 4;
		private string Indent()
		{
			var sb = new StringBuilder();
			for (var i = 0; i < (_level * _indentSize); i++)
				sb.Append(' ');

			return sb.ToString();
		}

		public bool EnterPlugin(int baseSize)
		{
			Debug.WriteLine(Indent() + "Plugin, baseSize = 0x{0:X}", baseSize);
			_level++;
			return true;
		}

		public void LeavePlugin()
		{
			_level--;
		}

		public bool EnterRevisions()
		{
			Debug.WriteLine(Indent() + "Version history:");
			_level++;
			return true;
		}

		public void VisitRevision(PluginRevision revision)
		{
			Debug.WriteLine(Indent() + "Plugin version {0} by {1}: {2}", revision.Version, revision.Researcher, revision.Description);
		}

		public void LeaveRevisions()
		{
			_level--;
		}

		public void VisitComment(string title, string text, uint pluginLine)
		{
			Debug.WriteLine(Indent() + "Comment \"{0}\": {1}", title, text);
		}

		public void VisitUInt8(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("UInt8", name, offset, visible);
		}

		public void VisitInt8(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Int8", name, offset, visible);
		}

		public void VisitUInt16(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("UInt16", name, offset, visible);
		}

		public void VisitInt16(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Int16", name, offset, visible);
		}

		public void VisitUInt32(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("UInt32", name, offset, visible);
		}

		public void VisitInt32(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Int32", name, offset, visible);
		}

		public void VisitFloat32(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Float32", name, offset, visible);
		}

		public void VisitUndefined(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Undefined", name, offset, visible);
		}

		public void VisitPoint2(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Point2", name, offset, visible);
		}

		public void VisitPoint3(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Point3", name, offset, visible);
		}

		public void VisitVector2(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Vector2", name, offset, visible);
		}

		public void VisitVector3(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Vector3", name, offset, visible);
		}

		public void VisitVector4(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Vector4", name, offset, visible);
		}

		public void VisitDegree(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Degree", name, offset, visible);
		}

		public void VisitDegree2(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Degree2", name, offset, visible);
		}

		public void VisitDegree3(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Degree3", name, offset, visible);
		}

		public void VisitPlane2(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Plane2", name, offset, visible);
		}

		public void VisitPlane3(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Plane3", name, offset, visible);
		}

		public void VisitRect16(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Rect16", name, offset, visible);
		}

		public void VisitStringID(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("StringID", name, offset, visible);
		}

		public void VisitTagReference(string name, uint offset, bool visible, bool withClass, bool showJumpTo, uint pluginLine)
		{
			PrintBasicValue("Tag reference", name, offset, visible);
		}

		public bool EnterBitfield8(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Bitfield8", name, offset, visible);
			_level++;
			return true;
		}

		public bool EnterBitfield16(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Bitfield16", name, offset, visible);
			_level++;
			return true;
		}

		public bool EnterBitfield32(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Bitfield32", name, offset, visible);
			_level++;
			return true;
		}

		public bool EnterBitfield64(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Bitfield64", name, offset, visible);
			_level++;
			return true;
		}

		public void VisitBit(string name, int index)
		{
			Debug.WriteLine(Indent() + "Bit \"{0}\" at position {1}", name, index);
		}

		public void LeaveBitfield()
		{
			_level--;
		}

		public bool EnterEnum8(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Enum8", name, offset, visible);
			_level++;
			return true;
		}

		public bool EnterEnum16(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Enum16", name, offset, visible);
			_level++;
			return true;
		}

		public bool EnterEnum32(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Enum32", name, offset, visible);
			_level++;
			return true;
		}

		public void VisitOption(string name, int value)
		{
			Debug.WriteLine(Indent() + "{0} = {1}", name, value);
		}

		public void LeaveEnum()
		{
			_level--;
		}

		public bool EnterTagBlock(string name, uint offset, bool visible, uint elementSize, int align, bool sort, uint pluginLine)
		{
			Debug.WriteLine(Indent() + "Tag block \"{0}\" at {1}, visible = {2}, elementSize = {3}, align = {4}, sort = {5}", name, offset, visible, elementSize, align, sort);
			_level++;
			return true;
		}

		public void LeaveTagBlock()
		{
			_level--;
		}

		public void VisitDataReference(string name, uint offset, string format, bool visible, int align, uint pluginLine)
		{
			Debug.WriteLine(Indent() + "Data reference \"{0}\" at {1}, format = {2}, visible = {3}, align = {4}", name, offset, format, visible, align);
		}

		public void VisitAscii(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			Debug.WriteLine(Indent() + "Ascii string \"{0}\" at {1}, visible = {2}, size = {3}", name, offset, visible, size);
		}

		public void VisitUtf16(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			Debug.WriteLine(Indent() + "Utf16 string \"{0}\" at {1}, visible = {2}, size = {3}", name, offset, visible, size);
		}

		public void VisitColorInt(string name, uint offset, bool visible, bool alpha, uint pluginLine)
		{
			Debug.WriteLine(Indent() + "Color32 \"{0}\" at {1}, visible = {2}, alpha = {3}", name, offset, visible, alpha);
		}

		public void VisitColorF(string name, uint offset, bool visible, bool alpha, uint pluginLine)
		{
			Debug.WriteLine(Indent() + "ColorF \"{0}\" at {1}, visible = {2}, alpha = {3}", name, offset, visible, alpha);
		}

		public void VisitRawData(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			Debug.WriteLine(Indent() + "Raw data block \"{0}\" at {1}, visible = {2}, size = {3}", name, offset, visible, size);
		}

		private static void PrintBasicValue(string type, string name, uint offset, bool visible)
		{
			Debug.WriteLine("{0} \"{1}\" at {2}, visible = {3}", type, name, offset, visible);
		}

		public void VisitShader(string name, uint offset, bool visible, ShaderType type, uint pluginLine)
		{
			Debug.WriteLine(Indent() + "Shader \"{0}\" at {1}, visible = {2}, type = {3}", name, offset, visible, type);
		}

		public void VisitRangeUInt16(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Range16", name, offset, visible);
		}

		public void VisitRangeFloat32(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("RangeF", name, offset, visible);
		}

		public void VisitRangeDegree(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("RangeD", name, offset, visible);
		}

		public void VisitUnicList(string name, uint offset, bool visible, int languages, uint pluginLine)
		{
			Debug.WriteLine(Indent() + "Unicode string list \"{0}\" at {1}, visible = {2}, languages = {3}", name, offset, visible, languages);
		}
	}
}
