/*
 * TODO LOG:
 *      - ColourF
 *      - ColourInt
 * 
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Assembly.Metro.Controls.PageTemplates.Games;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using ExtryzeDLL.Plugins;

namespace Assembly.Helpers.Plugins
{
    public class ThirdGenPluginVisitor : IPluginVisitor
    {
        // Private Members
        private readonly TagHierarchy _tags;

        private readonly List<ReflexiveData> _reflexives = new List<ReflexiveData>();
	    private ReflexiveData _currentReflexive;
        private readonly bool _showInvisibles;

	    private EnumData _currentEnum;
	    private BitfieldData _currentBitfield;
        private readonly IList<PluginRevision> _pluginRevisions = new List<PluginRevision>();

        // Public Members
        public IList<PluginRevision> PluginRevisions { get { return _pluginRevisions; } }
        public ObservableCollection<MetaField> Values { get; private set; }
        public ObservableCollection<ReflexiveData> Reflexives { get; private set; }

        public ThirdGenPluginVisitor(TagHierarchy tags, bool showInvisibles)
        {
            _tags = tags;

            Values = new ObservableCollection<MetaField>();
            Reflexives = new ObservableCollection<ReflexiveData>();
            _showInvisibles = showInvisibles;
        }

        public bool EnterPlugin(int baseSize) { return true; }
        public void LeavePlugin() { }

        #region Revisions
        public bool EnterRevisions() { return true; }
        public void VisitRevision(PluginRevision revision) { _pluginRevisions.Add(revision); }
        public void LeaveRevisions() { }
        #endregion

        public void VisitComment(string title, string text)
        {
            AddValue(new CommentData(title, text));
        }

        #region Integers
        public void VisitUInt8(string name, uint offset, bool visible, uint pluginLine)
        {
            if (visible || _showInvisibles)
                AddValue(new Uint8Data(name, offset, "uint8", 0, pluginLine));
        }
        public void VisitInt8(string name, uint offset, bool visible, uint pluginLine)
        {
            if (visible || _showInvisibles)
                AddValue(new Int8Data(name, offset, "int8", 0, pluginLine));
        }

        public void VisitUInt16(string name, uint offset, bool visible, uint pluginLine)
        {
            if (visible || _showInvisibles)
                AddValue(new Uint16Data(name, offset, "uint16", 0, pluginLine));
        }
        public void VisitInt16(string name, uint offset, bool visible, uint pluginLine)
        {
            if (visible || _showInvisibles)
                AddValue(new Int16Data(name, offset, "int16", 0, pluginLine));
        }

        public void VisitUInt32(string name, uint offset, bool visible, uint pluginLine)
        {
            if (visible || _showInvisibles)
                AddValue(new Uint32Data(name, offset, "uint32", 0, pluginLine));
        }
        public void VisitInt32(string name, uint offset, bool visible, uint pluginLine)
        {
            if (visible || _showInvisibles)
                AddValue(new Int32Data(name, offset, "int32", 0, pluginLine));
        }

        public void VisitFloat32(string name, uint offset, bool visible, uint pluginLine)
        {
            if (visible || _showInvisibles)
                AddValue(new Float32Data(name, offset, "float32", 0, pluginLine));
        }

        public void VisitUndefined(string name, uint offset, bool visible, uint pluginLine)
        {
            if (visible || _showInvisibles)
                AddValue(new Float32Data(name, offset, "undefined", 0, pluginLine));
        }
        #endregion

        public void VisitVector3(string name, uint offset, bool visible, uint pluginLine)
        {
            if (visible || _showInvisibles)
                AddValue(new VectorData(name, offset, 0, 0, 0, pluginLine));
        }

        public void VisitRange(string name, uint offset, bool visible, string type, double minval, double maxval, double smallchange, double largechange, uint pluginLine)
        {
            /*TrackBar metaComponents = new TrackBar();
            metaComponents.LoadValues(name, type, minval, maxval, smallchange, largechange);

            AddUIElement(metaComponents, visible);*/
        }
        public void VisitColorInt(string name, uint offset, bool visible, string format, uint pluginLine)
        {
            //Debug.WriteLine("WOAH KID. I HAVEN'T CODED THIS. GET OUT OF HERE");
        }
        public void VisitColorF(string name, uint offset, bool visible, string format, uint pluginLine)
        {
            //Debug.WriteLine("WOAH KID. I HAVEN'T CODED THIS. GET OUT OF HERE");
        }

        public void VisitStringID(string name, uint offset, bool visible, uint pluginLine)
        {
            if (visible || _showInvisibles)
                AddValue(new StringIDData(name, offset, 0, pluginLine));
        }
        public void VisitAscii(string name, uint offset, bool visible, int length, uint pluginLine)
        {
            if (visible || _showInvisibles)
                AddValue(new StringData(name, offset, "", length, pluginLine));
        }

        public void VisitRawData(string name, uint offset, bool visible, int size, uint pluginLine)
        {
            if (visible || _showInvisibles)
                AddValue(new RawData(name, offset, "", size, pluginLine));
        }

        public void VisitTagReference(string name, uint offset, bool visible, bool withClass, bool showJumpTo, uint pluginLine)
        {
	        if (!visible && !_showInvisibles) return;
	        
			var jumpTo = showJumpTo ?
		                                Visibility.Visible : Visibility.Hidden;

	        AddValue(new TagRefData(name, offset, _tags, jumpTo, withClass, pluginLine));
        }

        public void VisitDataReference(string name, uint offset, bool visible, uint pluginLine)
        {
            if (visible || _showInvisibles)
                AddValue(new DataRef(name, offset, 0, "", 0, pluginLine));
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

        private bool EnterBitfield(BitfieldType type, string name, uint offset, bool visible, uint pluginLine)
        {
            if (visible || _showInvisibles)
            {
                _currentBitfield = new BitfieldData(name, offset, type, pluginLine);
                return true;
            }
            return false;
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

        private bool EnterEnum(EnumType type, string name, uint offset, bool visible, uint pluginLine)
        {
            if (visible || _showInvisibles)
            {
                _currentEnum = new EnumData(name, offset, type, 0, pluginLine);
                return true;
            }
            return false;
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
        #endregion

        #region Reflexive
        public bool EnterReflexive(string name, uint offset, bool visible, uint entrySize, uint pluginLine)
        {
            if (visible || _showInvisibles)
            {
                var data = new ReflexiveData(name, offset, entrySize, pluginLine);
                AddValue(data);

                _reflexives.Add(data);
                Reflexives.Add(data);
                _currentReflexive = data;
                return true;
            }
            return false;
        }

        public void LeaveReflexive()
        {
            if (_currentReflexive == null)
                throw new InvalidOperationException("Not in a reflexive");

            _reflexives.RemoveAt(_reflexives.Count - 1);
            _currentReflexive = _reflexives.Count > 0 ? _reflexives[_reflexives.Count - 1] : null;
        }
        #endregion

        private void AddValue(MetaField value)
        {
            if (_reflexives.Count > 0)
                _reflexives[_reflexives.Count - 1].Template.Add(value);
            else
                Values.Add(value);
            /*MetaField wrappedValue = value;
            for (int i = _reflexives.Count - 1; i >= 0; i--)
            {
                ReflexiveData reflexive = _reflexives[i];
                reflexive.Pages[0].Fields.Add(wrappedValue);

                // hax, use a null parent for now so MetaReader doesn't have to cause it to unsubscribe
                wrappedValue = new WrappedReflexiveEntry(null, reflexive.Pages[0].Fields.Count - 1);
            }

            Values.Add(wrappedValue);*/
        }
    }
}