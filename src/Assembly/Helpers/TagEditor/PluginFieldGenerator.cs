using Assembly.Helpers.TagEditor.Buffering;
using Assembly.Helpers.TagEditor.Fields;
using Blamite.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.TagEditor
{
	/// <summary>
	/// Generates tag editor fields based off of data in a plugin.
	/// </summary>
	public class PluginFieldGenerator : IPluginVisitor
	{
		private Func<int, TagBufferSource> _rootSourceGenerator;
		private TagBufferSource _rootSource;
		private TagBufferSource _currentSource;
		private ObservableCollection<TagField> _fields = new ObservableCollection<TagField>();

		/// <summary>
		/// Initializes a new instance of the <see cref="PluginFieldGenerator"/> class.
		/// </summary>
		/// <param name="rootSourceGenerator">Function that returns a <see cref="TagBufferSource"/> for the root tag data given a base size.</param>
		public PluginFieldGenerator(Func<int, TagBufferSource> rootSourceGenerator)
		{
			_rootSourceGenerator = rootSourceGenerator;
		}

		/// <summary>
		/// Gets the size of the tag's data.
		/// </summary>
		public int BaseSize { get; private set; }

		/// <summary>
		/// Gets the fields that are generated.
		/// </summary>
		public ObservableCollection<TagField> Fields
		{
			get { return _fields; }
		}

		bool IPluginVisitor.EnterPlugin(int baseSize)
		{
			_rootSource = _rootSourceGenerator(baseSize);
			_currentSource = _rootSource;
			BaseSize = baseSize;
			return true;
		}

		void IPluginVisitor.LeavePlugin()
		{
			
		}

		bool IPluginVisitor.EnterRevisions()
		{
			return false;
		}

		void IPluginVisitor.VisitRevision(PluginRevision revision)
		{
			
		}

		void IPluginVisitor.LeaveRevisions()
		{
			
		}

		void IPluginVisitor.VisitComment(string title, string text, uint pluginLine)
		{
			
		}

		void IPluginVisitor.VisitUInt8(string name, uint offset, bool visible, uint pluginLine)
		{
			AddField(new UInt8Field(name, offset, 0, "uint8", pluginLine, _currentSource));
		}

		void IPluginVisitor.VisitInt8(string name, uint offset, bool visible, uint pluginLine)
		{
			AddField(new Int8Field(name, offset, 0, "int8", pluginLine, _currentSource));
		}

		void IPluginVisitor.VisitUInt16(string name, uint offset, bool visible, uint pluginLine)
		{
			AddField(new UInt16Field(name, offset, 0, "uint16", pluginLine, _currentSource));
		}

		void IPluginVisitor.VisitInt16(string name, uint offset, bool visible, uint pluginLine)
		{
			AddField(new Int16Field(name, offset, 0, "int16", pluginLine, _currentSource));
		}

		void IPluginVisitor.VisitUInt32(string name, uint offset, bool visible, uint pluginLine)
		{
			AddField(new UInt32Field(name, offset, 0, "uint32", pluginLine, _currentSource));
		}

		void IPluginVisitor.VisitInt32(string name, uint offset, bool visible, uint pluginLine)
		{
			AddField(new Int32Field(name, offset, 0, "int32", pluginLine, _currentSource));
		}

		void IPluginVisitor.VisitFloat32(string name, uint offset, bool visible, uint pluginLine)
		{
			AddField(new Float32Field(name, offset, 0, "float32", pluginLine, _currentSource));
		}

		void IPluginVisitor.VisitUndefined(string name, uint offset, bool visible, uint pluginLine)
		{
			AddField(new Float32Field(name, offset, 0, "undefined", pluginLine, _currentSource));
		}

		void IPluginVisitor.VisitVector3(string name, uint offset, bool visible, uint pluginLine)
		{
			
		}

		void IPluginVisitor.VisitDegree(string name, uint offset, bool visible, uint pluginLine)
		{
			
		}

		void IPluginVisitor.VisitStringID(string name, uint offset, bool visible, uint pluginLine)
		{
			
		}

		void IPluginVisitor.VisitTagReference(string name, uint offset, bool visible, bool withClass, bool showJumpTo, uint pluginLine)
		{
			
		}

		void IPluginVisitor.VisitDataReference(string name, uint offset, string format, bool visible, int align, uint pluginLine)
		{
			
		}

		void IPluginVisitor.VisitRawData(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			
		}

		void IPluginVisitor.VisitRange(string name, uint offset, bool visible, string type, double min, double max, double smallChange, double largeChange, uint pluginLine)
		{
			
		}

		void IPluginVisitor.VisitAscii(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			
		}

		void IPluginVisitor.VisitUtf16(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			
		}

		void IPluginVisitor.VisitColorInt(string name, uint offset, bool visible, string format, uint pluginLine)
		{
			
		}

		void IPluginVisitor.VisitColorF(string name, uint offset, bool visible, string format, uint pluginLine)
		{
			
		}

		bool IPluginVisitor.EnterBitfield8(string name, uint offset, bool visible, uint pluginLine)
		{
			return false;
		}

		bool IPluginVisitor.EnterBitfield16(string name, uint offset, bool visible, uint pluginLine)
		{
			return false;
		}

		bool IPluginVisitor.EnterBitfield32(string name, uint offset, bool visible, uint pluginLine)
		{
			return false;
		}

		void IPluginVisitor.VisitBit(string name, int index)
		{
			
		}

		void IPluginVisitor.LeaveBitfield()
		{
			
		}

		bool IPluginVisitor.EnterEnum8(string name, uint offset, bool visible, uint pluginLine)
		{
			return false;
		}

		bool IPluginVisitor.EnterEnum16(string name, uint offset, bool visible, uint pluginLine)
		{
			return false;
		}

		bool IPluginVisitor.EnterEnum32(string name, uint offset, bool visible, uint pluginLine)
		{
			return false;
		}

		void IPluginVisitor.VisitOption(string name, int value)
		{
			
		}

		void IPluginVisitor.LeaveEnum()
		{
			
		}

		bool IPluginVisitor.EnterReflexive(string name, uint offset, bool visible, uint entrySize, int align, uint pluginLine)
		{
			return false;
		}

		void IPluginVisitor.LeaveReflexive()
		{
			
		}

		void IPluginVisitor.VisitShader(string name, uint offset, bool visible, Blamite.Blam.Shaders.ShaderType type, uint pluginLine)
		{
			
		}

		void IPluginVisitor.VisitUnicList(string name, uint offset, bool visible, int languages, uint pluginLine)
		{
			
		}

		private void AddField(TagField field)
		{
			_fields.Add(field);
		}
	}
}
