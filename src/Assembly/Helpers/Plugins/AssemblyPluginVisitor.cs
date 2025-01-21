using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Assembly.Metro.Controls.PageTemplates.Games;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using Blamite.Blam.Shaders;
using Blamite.IO;
using Blamite.Plugins;
using Blamite.Util;
using System.Windows.Media;

namespace Assembly.Helpers.Plugins
{
	public class AssemblyPluginVisitor : IPluginVisitor
	{
		// Private Members

		private readonly FileSegmentGroup _metaArea;
		private readonly IList<PluginRevision> _pluginRevisions = new List<PluginRevision>();
		private readonly List<TagBlockData> _tagBlocks = new List<TagBlockData>();
		private readonly bool _showInvisibles;
		private readonly bool _alwaysBasicColors;
		private readonly bool _viewValueAs;
		private readonly Trie _stringIDTrie;
		private readonly TagHierarchy _tags;
		private FlagData _currentFlags;
		private EnumData _currentEnum;
		private TagBlockData _currentTagBlock;
		private TagDataCommandState _tagCommandState;

		public bool ShowComments
		{
			get { return App.AssemblyStorage.AssemblySettings.PluginsShowComments; }
		}
		
		public AssemblyPluginVisitor(TagHierarchy tags, Trie stringIDTrie, FileSegmentGroup metaArea, bool showInvisibles, TagDataCommandState tagCommandState, bool alwaysBasicColor, bool viewValueAs = false)
		{
			_tags = tags;
			_stringIDTrie = stringIDTrie;
			_metaArea = metaArea;

			Values = new ObservableCollection<MetaField>();
			TagBlocks = new ObservableCollection<TagBlockData>();
			_showInvisibles = showInvisibles;
			_alwaysBasicColors = alwaysBasicColor;
			_viewValueAs = viewValueAs;
			_tagCommandState = tagCommandState;
		}

		// Public Members
		public IList<PluginRevision> PluginRevisions
		{
			get { return _pluginRevisions; }
		}

		public ObservableCollection<MetaField> Values { get; private set; }
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
			if (ShowComments)
				AddValue(new CommentData(title, text, pluginLine));
		}

		public void VisitPoint2(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Point2Data(name, offset, 0, "point2", 0, 0, pluginLine, tooltip));
		}

		public void VisitPoint3(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Point3Data(name, offset, 0, "point3", 0, 0, 0, pluginLine, tooltip));
		}

		public void VisitVector2(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Vector2Data(name, offset, 0, "vector2", 0, 0, pluginLine, tooltip));
		}

		public void VisitVector3(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Vector3Data(name, offset, 0, "vector3", 0, 0, 0, pluginLine, tooltip));
		}

		public void VisitVector4(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Vector4Data(name, offset, 0, "quaternion", 0, 0, 0, 0, pluginLine, tooltip));
		}

		public void VisitDegree2(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Degree2Data(name, offset, 0, "degree2", 0, 0, pluginLine, tooltip));
		}

		public void VisitDegree3(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Degree3Data(name, offset, 0, "degree3", 0, 0, 0, pluginLine, tooltip));
		}

		public void VisitPlane2(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Plane2Data(name, offset, 0, "plane2", 0, 0, 0, pluginLine, tooltip));
		}

		public void VisitPlane3(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Plane3Data(name, offset, 0, "plane3", 0, 0, 0, 0, pluginLine, tooltip));
		}

		public void VisitRect16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new RectangleData(name, offset, 0, "rectangle16", 0, 0, 0, 0, pluginLine, tooltip));
		}

		public void VisitQuat16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Quaternion16Data(name, offset, 0, "quaternion16", 0, 0, 0, 0, pluginLine, tooltip));
		}

		public void VisitPoint16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Point16Data(name, offset, 0, "point16", 0, 0, pluginLine, tooltip));
		}

		public void VisitDegree(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new DegreeData(name, offset, 0, "degree", 0, pluginLine, tooltip));
		}

		public void VisitColorInt(string name, uint offset, bool visible, bool alpha, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new ColorData(name, offset, 0, alpha, true, "color32", Colors.Transparent, pluginLine, tooltip));
		}

		public void VisitColorF(string name, uint offset, bool visible, bool alpha, bool basic, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new ColorData(name, offset, 0, alpha, basic || _alwaysBasicColors, "colorf", Colors.Transparent, pluginLine, tooltip));
		}

		public void VisitStringID(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new StringIDData(name, offset, 0, "", _stringIDTrie, pluginLine, tooltip));
		}

		public void VisitAscii(string name, uint offset, bool visible, int size, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new StringData(name, offset, 0, StringType.ASCII, "", size, pluginLine, tooltip));
		}

		public void VisitUtf16(string name, uint offset, bool visible, int size, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new StringData(name, offset, 0, StringType.UTF16, "", size, pluginLine, tooltip));
		}

		public void VisitHexString(string name, uint offset, bool visible, int size, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new StringData(name, offset, 0, StringType.Hex, "", size, pluginLine, tooltip));
		}

		public void VisitRawData(string name, uint offset, bool visible, int size, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new RawData(name, offset, "bytes", 0, "", size, pluginLine, tooltip, _metaArea, _tagCommandState));
		}

		public void VisitTagReference(string name, uint offset, bool visible, bool withGroup, uint pluginLine, string tooltip)
		{
			if (!visible && !_showInvisibles) return;

			AddValue(new TagRefData(name, offset, 0, _tags, !_viewValueAs, withGroup, pluginLine, tooltip));
		}

		public void VisitDataReference(string name, uint offset, string format, bool visible, int align, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new DataRef(name, offset, format, 0, 0, "", 0, pluginLine, tooltip, _metaArea, _tagCommandState));
		}

		public void VisitShader(string name, uint offset, bool visible, ShaderType type, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new ShaderRef(name, offset, 0, type, null, pluginLine, tooltip));
		}

		public void VisitUnicList(string name, uint offset, bool visible, int languages, uint pluginLine, string tooltip)
		{
			if (!visible && !_showInvisibles)
				return;
			for (var i = 0; i < languages; i++)
			{
				AddValue(new Uint16Data("Language " + i + " " + name + " Index", (uint)(offset + i * 4), 0, "uint16", 0, pluginLine, tooltip));
				AddValue(new Uint16Data("Language " + i + " " + name + " Count", (uint)(offset + i * 4 + 2), 0, "uint16", 0, pluginLine, tooltip));
			}
		}

		public void VisitDatum(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new DatumData(name, offset, 0, 0, 0, pluginLine, tooltip));
		}

		public void VisitOldStringID(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new OldStringIDData(name, offset, 0, "", _stringIDTrie, pluginLine, tooltip));
		}

		#region Range

		public void VisitRangeInt16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new RangeInt16Data(name, offset, 0, "range16", 0, 0, pluginLine, tooltip));
		}

		public void VisitRangeFloat32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new RangeFloat32Data(name, offset, 0, "rangeF", 0, 0, pluginLine, tooltip));
		}

		public void VisitRangeDegree(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new RangeDegreeData(name, offset, 0, "rangeD", 0, 0, pluginLine, tooltip));
		}

		#endregion

		#region Flags

		public bool EnterFlags8(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			return EnterFlags(FlagsType.Flags8, name, offset, visible, pluginLine, tooltip);
		}

		public bool EnterFlags16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			return EnterFlags(FlagsType.Flags16, name, offset, visible, pluginLine, tooltip);
		}

		public bool EnterFlags32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			return EnterFlags(FlagsType.Flags32, name, offset, visible, pluginLine, tooltip);
		}

		public bool EnterFlags64(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			return EnterFlags(FlagsType.Flags64, name, offset, visible, pluginLine, tooltip);
		}

		public void VisitBit(string name, int index, string tooltip)
		{
			if (_currentFlags != null)
				_currentFlags.DefineBit(index, name, tooltip);
			else
				throw new InvalidOperationException("Cannot add a bit to a non-existant flags field");
		}

		public void LeaveFlags()
		{
			if (_currentFlags == null)
				throw new InvalidOperationException("Cannot leave a flags field if one isn't active");

			AddValue(_currentFlags);
			_currentFlags = null;
		}

		private bool EnterFlags(FlagsType type, string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
			{
				_currentFlags = new FlagData(name, offset, 0, type, pluginLine, tooltip);
				return true;
			}
			return false;
		}

		#endregion

		#region Enum

		public bool EnterEnum8(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			return EnterEnum(EnumType.Enum8, name, offset, visible, pluginLine, tooltip);
		}

		public bool EnterEnum16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			return EnterEnum(EnumType.Enum16, name, offset, visible, pluginLine, tooltip);
		}

		public bool EnterEnum32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			return EnterEnum(EnumType.Enum32, name, offset, visible, pluginLine, tooltip);
		}

		public void VisitOption(string name, int value, string tooltip)
		{
			if (_currentEnum != null)
				_currentEnum.Values.Add(new EnumValue(name, value, tooltip));
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

		private bool EnterEnum(EnumType type, string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
			{
				_currentEnum = new EnumData(name, offset, 0, type, 0, pluginLine, tooltip);
				return true;
			}
			return false;
		}

		#endregion

		#region Tag Block

		public bool EnterTagBlock(string name, uint offset, bool visible, uint elementSize, int align, bool sort, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
			{
				var data = new TagBlockData(name, offset, 0, elementSize, align, sort, pluginLine, tooltip, _metaArea, _tagCommandState);
				AddValue(data);

				_tagBlocks.Add(data);
				TagBlocks.Add(data);
				_currentTagBlock = data;
				return true;
			}
			return false;
		}

		public void LeaveTagBlock()
		{
			if (_currentTagBlock == null)
				throw new InvalidOperationException("Not in a tagBlock");

			_tagBlocks.RemoveAt(_tagBlocks.Count - 1);
			_currentTagBlock = _tagBlocks.Count > 0 ? _tagBlocks[_tagBlocks.Count - 1] : null;
		}

		#endregion

		#region Integers

		public void VisitUInt8(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Uint8Data(name, offset, 0, "uint8", 0, pluginLine, tooltip));
		}

		public void VisitInt8(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Int8Data(name, offset, 0, "int8", 0, pluginLine, tooltip));
		}

		public void VisitUInt16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Uint16Data(name, offset, 0, "uint16", 0, pluginLine, tooltip));
		}

		public void VisitInt16(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Int16Data(name, offset, 0, "int16", 0, pluginLine, tooltip));
		}

		public void VisitUInt32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Uint32Data(name, offset, 0, "uint32", 0, pluginLine, tooltip));
		}

		public void VisitInt32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Int32Data(name, offset, 0, "int32", 0, pluginLine, tooltip));
		}

		public void VisitUInt64(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Uint64Data(name, offset, 0, "uint64", 0, pluginLine, tooltip));
		}

		public void VisitInt64(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Int64Data(name, offset, 0, "int64", 0, pluginLine, tooltip));
		}

		public void VisitFloat32(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Float32Data(name, offset, 0, "float32", 0, pluginLine, tooltip));
		}

		public void VisitUndefined(string name, uint offset, bool visible, uint pluginLine, string tooltip)
		{
			if (visible || _showInvisibles)
				AddValue(new Float32Data(name, offset, 0, "undefined", 0, pluginLine, tooltip));
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

		private void AddValue(MetaField value)
		{
			if (_tagBlocks.Count > 0)
				_tagBlocks[_tagBlocks.Count - 1].Template.Add(value);
			else
				Values.Add(value);
			/*MetaField wrappedValue = value;
            for (int i = _tagBlocks.Count - 1; i >= 0; i--)
            {
                TagBlockData tagblock = _tagBlocks[i];
                tagblock.Pages[0].Fields.Add(wrappedValue);

                // hax, use a null parent for now so MetaReader doesn't have to cause it to unsubscribe
                wrappedValue = new WrappedTagBlockEntry(null, tagblock.Pages[0].Fields.Count - 1);
            }

            Values.Add(wrappedValue);*/
		}
	}
}