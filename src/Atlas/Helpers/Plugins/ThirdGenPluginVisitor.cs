using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Atlas.Helpers.Tags;
using Atlas.Views.Cache.TagEditorComponents.Data;
using Blamite.Blam.Shaders;
using Blamite.IO;
using Blamite.Plugins;
using Blamite.Util;

namespace Atlas.Helpers.Plugins
{
	public class ThirdGenPluginVisitor : IPluginVisitor
	{
		// Private Members

		private readonly FileSegmentGroup _metaArea;
		private readonly IList<PluginRevision> _pluginRevisions = new List<PluginRevision>();
		private readonly List<TagBlockData> _tagBlocks = new List<TagBlockData>();
		private readonly bool _showInvisibles;
		private readonly Trie _stringIdTrie;
		private readonly TagHierarchy _tags;
		private BitfieldData _currentBitfield;
		private EnumData _currentEnum;
		private TagBlockData _currentTagBlock;

		public ThirdGenPluginVisitor(TagHierarchy tags, Trie stringIdTrie, FileSegmentGroup metaArea, bool showInvisibles)
		{
			_tags = tags;
			_stringIdTrie = stringIdTrie;
			_metaArea = metaArea;

			Values = new ObservableCollection<TagDataField>();
			TagBlocks = new ObservableCollection<TagBlockData>();
			_showInvisibles = showInvisibles;
		}

		// Public Members
		public IList<PluginRevision> PluginRevisions
		{
			get { return _pluginRevisions; }
		}

		public ObservableCollection<TagDataField> Values { get; private set; }
		public ObservableCollection<TagBlockData> TagBlocks { get; private set; }

		public bool EnterPlugin(int baseSize)
		{
			return true;
		}

		public void LeavePlugin()
		{
		}

		public void VisitComment(string title, string text, uint pluginLine)
		{
			AddValue(new CommentData(title, text, pluginLine));
		}

		public void VisitVector3(string name, uint offset, bool visible, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new VectorData(name, offset, 0, 0, 0, 0, pluginLine));
		}

		public void VisitDegree(string name, uint offset, bool visible, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new DegreeData(name, offset, 0, 0, pluginLine));
		}

		public void VisitRange(string name, uint offset, bool visible, string type, double minval, double maxval,
			double smallchange, double largechange, uint pluginLine)
		{
			/*TrackBar metaComponents = new TrackBar();
			metaComponents.LoadValues(name, type, minval, maxval, smallchange, largechange);

			AddUIElement(metaComponents, visible);*/
		}

		public void VisitColorInt(string name, uint offset, bool visible, string format, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new ColourData(name, offset, 0, format, "int", "", pluginLine));
		}

		public void VisitColorF(string name, uint offset, bool visible, string format, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new ColourData(name, offset, 0, format, "float", "", pluginLine));
		}

		public void VisitStringID(string name, uint offset, bool visible, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new StringIDData(name, offset, 0, "", _stringIdTrie, pluginLine));
		}

		public void VisitAscii(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new StringData(name, offset, 0, StringType.ASCII, "", size, pluginLine));
		}

		public void VisitUtf16(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new StringData(name, offset, 0, StringType.UTF16, "", size, pluginLine));
		}

		public void VisitRawData(string name, uint offset, bool visible, int size, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new RawData(name, offset, 0, "", size, pluginLine));
		}

		public void VisitTagReference(string name, uint offset, bool visible, bool withClass, bool showJumpTo, uint pluginLine)
		{
			if (!visible && !_showInvisibles) return;

			var jumpTo = showJumpTo
				? Visibility.Visible
				: Visibility.Hidden;

			AddValue(new TagRefData(name, offset, 0, _tags, jumpTo, withClass, pluginLine));
		}

		public void VisitDataReference(string name, uint offset, string format, bool visible, int align, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new DataRef(name, offset, format, 0, 0, "", 0, pluginLine));
		}

		public void VisitShader(string name, uint offset, bool visible, ShaderType type, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new ShaderRef(name, offset, 0, type, null, pluginLine));
		}

		#region Bitfield

		public bool EnterBitfield8(string name, uint offset, bool visible, uint pluginLine)
		{
			return EnterBitfield(BitfieldType.Bitfield8, name, offset, visible, pluginLine);
		}

		public bool EnterBitfield16(string name, uint offset, bool visible, uint pluginLine)
		{
			return EnterBitfield(BitfieldType.Bitfield16, name, offset, visible, pluginLine);
		}

		public bool EnterBitfield32(string name, uint offset, bool visible, uint pluginLine)
		{
			return EnterBitfield(BitfieldType.Bitfield32, name, offset, visible, pluginLine);
		}

		public void VisitBit(string name, int index)
		{
			if (_currentBitfield != null)
				_currentBitfield.DefineBit(index, name);
			else
				throw new InvalidOperationException("Cannot add a bit to a non-existant bitfield");
		}

		public void LeaveBitfield()
		{
			if (_currentBitfield == null)
				throw new InvalidOperationException("Cannot leave a bitfield if one isn't active");

			AddValue(_currentBitfield);
			_currentBitfield = null;
		}

		private bool EnterBitfield(BitfieldType type, string name, uint offset, bool visible, uint pluginLine)
		{
			if (visible || _showInvisibles)
			{
				_currentBitfield = new BitfieldData(name, offset, 0, type, pluginLine);
				return true;
			}
			return false;
		}

		#endregion

		#region Enum

		public bool EnterEnum8(string name, uint offset, bool visible, uint pluginLine)
		{
			return EnterEnum(EnumType.Enum8, name, offset, visible, pluginLine);
		}

		public bool EnterEnum16(string name, uint offset, bool visible, uint pluginLine)
		{
			return EnterEnum(EnumType.Enum16, name, offset, visible, pluginLine);
		}

		public bool EnterEnum32(string name, uint offset, bool visible, uint pluginLine)
		{
			return EnterEnum(EnumType.Enum32, name, offset, visible, pluginLine);
		}

		public void VisitOption(string name, int value)
		{
			if (_currentEnum != null)
				_currentEnum.Values.Add(new EnumValue(name, value));
			else
				throw new InvalidOperationException("Cannot add an option to a non-existant enum");
		}

		public void LeaveEnum()
		{
			if (_currentEnum == null)
				throw new InvalidOperationException("Cannot leave an enum if one isn't active");

			AddValue(_currentEnum);
			_currentEnum = null;
		}

		private bool EnterEnum(EnumType type, string name, uint offset, bool visible, uint pluginLine)
		{
			if (visible || _showInvisibles)
			{
				_currentEnum = new EnumData(name, offset, 0, type, 0, pluginLine);
				return true;
			}
			return false;
		}

		#endregion

		#region TagBlock

		public bool EnterReflexive(string name, uint offset, bool visible, uint entrySize, int align, uint pluginLine)
		{
			if (visible || _showInvisibles)
			{
				var data = new TagBlockData(name, offset, 0, entrySize, pluginLine, _metaArea);
				AddValue(data);

				_tagBlocks.Add(data);
				TagBlocks.Add(data);
				_currentTagBlock = data;
				return true;
			}
			return false;
		}

		public void LeaveReflexive()
		{
			if (_currentTagBlock == null)
				throw new InvalidOperationException("Not in a tag block");

			_tagBlocks.RemoveAt(_tagBlocks.Count - 1);
			_currentTagBlock = _tagBlocks.Count > 0 ? _tagBlocks[_tagBlocks.Count - 1] : null;
		}

		#endregion

		#region Integers

		public void VisitUInt8(string name, uint offset, bool visible, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new Uint8Data(name, offset, 0, "uint8", 0, pluginLine));
		}

		public void VisitInt8(string name, uint offset, bool visible, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new Int8Data(name, offset, 0, "int8", 0, pluginLine));
		}

		public void VisitUInt16(string name, uint offset, bool visible, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new Uint16Data(name, offset, 0, "uint16", 0, pluginLine));
		}

		public void VisitInt16(string name, uint offset, bool visible, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new Int16Data(name, offset, 0, "int16", 0, pluginLine));
		}

		public void VisitUInt32(string name, uint offset, bool visible, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new Uint32Data(name, offset, 0, "uint32", 0, pluginLine));
		}

		public void VisitInt32(string name, uint offset, bool visible, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new Int32Data(name, offset, 0, "int32", 0, pluginLine));
		}

		public void VisitFloat32(string name, uint offset, bool visible, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new Float32Data(name, offset, 0, "float32", 0, pluginLine));
		}

		public void VisitUndefined(string name, uint offset, bool visible, uint pluginLine)
		{
			if (visible || _showInvisibles)
				AddValue(new Float32Data(name, offset, 0, "undefined", 0, pluginLine));
		}

		#endregion

		#region Revisions

		public bool EnterRevisions()
		{
			return true;
		}

		public void VisitRevision(PluginRevision revision)
		{
			_pluginRevisions.Add(revision);
		}

		public void LeaveRevisions()
		{
		}

		#endregion

		private void AddValue(TagDataField value)
		{
			if (_tagBlocks.Count > 0)
				_tagBlocks[_tagBlocks.Count - 1].Template.Add(value);
			else
				Values.Add(value);
		}
	}
}