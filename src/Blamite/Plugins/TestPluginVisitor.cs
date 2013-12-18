using System.Diagnostics;
using Blamite.Blam.Shaders;

namespace Blamite.Plugins
{
	public class TestPluginVisitor : IPluginVisitor
	{
		public bool EnterPlugin(int baseSize)
		{
			Debug.WriteLine("Plugin, baseSize = 0x{0:X}", baseSize);
			Debug.Indent();
			return true;
		}

		public void LeavePlugin()
		{
			Debug.Unindent();
		}

		public bool EnterRevisions()
		{
			Debug.WriteLine("Version history:");
			Debug.Indent();
			return true;
		}

		public void VisitRevision(PluginRevision revision)
		{
			Debug.WriteLine("Plugin version {0} by {1}: {2}", revision.Version, revision.Researcher, revision.Description);
		}

		public void LeaveRevisions()
		{
			Debug.Unindent();
		}

		public void VisitComment(string title, string text, uint pluginLine)
		{
			Debug.WriteLine("Comment \"{0}\": {1}", title, text);
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

		public void VisitVector3(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Vector3", name, offset, visible);
		}

		public void VisitDegree(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Degree", name, offset, visible);
		}

		public void VisitRange(string name, uint offset, bool visible, string type, double minval, double maxval,
			double smallchange, double largechange, uint pluginLine)
		{
			var yolo = new object[]
			{
				#region objectarray
				name,
				offset,
				visible,
				minval,
				maxval,
				largechange,
				smallchange
				#endregion
			};

			Debug.WriteLine(
				"Range \"{0}\" at position {1}, visible = {2} Minimium Value = {3}, Maxamium Value = {4}. Large Change = {5}, Small Change = {6}.",
				yolo);
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
			Debug.Indent();
			return true;
		}

		public bool EnterBitfield16(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Bitfield16", name, offset, visible);
			Debug.Indent();
			return true;
		}

		public bool EnterBitfield32(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Bitfield32", name, offset, visible);
			Debug.Indent();
			return true;
		}

		public void VisitBit(string name, int index)
		{
			Debug.WriteLine("Bit \"{0}\" at position {1}", name, index);
		}

		public void LeaveBitfield()
		{
			Debug.Unindent();
		}

		public bool EnterEnum8(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Enum8", name, offset, visible);
			Debug.Indent();
			return true;
		}

		public bool EnterEnum16(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Enum16", name, offset, visible);
			Debug.Indent();
			return true;
		}

		public bool EnterEnum32(string name, uint offset, bool visible, uint pluginLine)
		{
			PrintBasicValue("Enum32", name, offset, visible);
			Debug.Indent();
			return true;
		}

		public void VisitOption(string name, int value)
		{
			Debug.WriteLine("{0} = {1}", name, value);
		}

		public void LeaveEnum()
		{
			Debug.Unindent();
		}

		public bool EnterReflexive(string name, uint offset, bool visible, uint entrySize, int align, uint pluginLine)
		{
			Debug.WriteLine("Reflexive \"{0}\" at {1}, visible = {2}, entrySize = {3}, align = {4}", name, offset, visible, entrySize, align);
			Debug.Indent();
			return true;
		}

		public void LeaveReflexive()
		{
			Debug.Unindent();
		}

		public void VisitDataReference(string name, uint offset, string format, bool visible, int align, uint pluginLine)
		{
			Debug.WriteLine("Data reference \"{0}\" at {1}, format = {2}, visible = {3}, align = {4}", name, offset, format, visible, align);
		}

		public void VisitAscii(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			Debug.WriteLine("Ascii string \"{0}\" at {1}, visible = {2}, size = {3}", name, offset, visible, size);
		}

		public void VisitUtf16(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			Debug.WriteLine("Utf16 string \"{0}\" at {1}, visible = {2}, size = {3}", name, offset, visible, size);
		}

		public void VisitColorInt(string name, uint offset, bool visible, string format, uint pluginLine)
		{
			Debug.WriteLine("Color32 \"{0}\" at {1}, visible = {2}, format = {3}", name, offset, visible, format);
		}

		public void VisitColorF(string name, uint offset, bool visible, string format, uint pluginLine)
		{
			Debug.WriteLine("ColorF \"{0}\" at {1}, visible = {2}, format = {3}", name, offset, visible, format);
		}

		public void VisitRawData(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			Debug.WriteLine("Raw data block \"{0}\" at {1}, visible = {2}, size = {3}", name, offset, visible, size);
		}

		private static void PrintBasicValue(string type, string name, uint offset, bool visible)
		{
			Debug.WriteLine("{0} \"{1}\" at {2}, visible = {3}", type, name, offset, visible);
		}

		public void VisitShader(string name, uint offset, bool visible, ShaderType type, uint pluginLine)
		{
			Debug.WriteLine("Shader \"{0}\" at {1}, visible = {2}, type = {3}", name, offset, visible, type);
		}
	}
}