using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public enum BitfieldType
    {
        Bitfield8,
        Bitfield16,
        Bitfield32
    }

    public class BitfieldData : ValueField
    {
        private SortedList<int, BitData> _bits = new SortedList<int, BitData>();
        private uint _value, _originalValue;
        private BitfieldType _type;

        public BitfieldData(string name, uint offset, BitfieldType type, uint pluginLine)
            : base(name, offset, pluginLine)
        {
            _type = type;
        }

        public void DefineBit(int index, string name)
        {
            BitData data = new BitData(this, name, index);
            _bits[index] = data;
        }

        public uint Value
        {
            get { return _value; }
            set
            {
                _value = value;
                NotifyPropertyChanged("Value");
                RefreshBits();
            }
        }

        public BitfieldType Type
        {
            get { return _type; }
            set { _type = value; NotifyPropertyChanged("Type"); NotifyPropertyChanged("TypeStr"); }
        }

        public string TypeStr
        {
            get { return _type.ToString().ToLower(); }
        }

        public IEnumerable<BitData> Bits
        {
            get { return _bits.Values; }
        }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitBitfield(this);
        }

        public override MetaField DeepClone()
        {
            BitfieldData result = new BitfieldData(Name, Offset, _type, base.PluginLine);
            foreach (KeyValuePair<int, BitData> bit in _bits)
                result.DefineBit(bit.Key, bit.Value.Name);
            result.Value = _value;
            result._originalValue = _originalValue;
            return result;
        }

        public override bool HasChanged
        {
            get { return Value != _originalValue; }
        }

        public override void Reset()
        {
            Value = _originalValue;
        }

        public override void KeepChanges()
        {
            _originalValue = Value;
        }

        private void RefreshBits()
        {
            foreach (BitData bit in Bits)
                bit.Refresh();
        }
    }

    public class BitData : PropertyChangeNotifier
    {
        private BitfieldData _parent;
        private string _name;
        private uint _mask;

        public BitData(BitfieldData parent, string name, int index)
        {
            _parent = parent;
            _name = name;
            _mask = 1U << index;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyPropertyChanged("Name"); }
        }

        public bool IsSet
        {
            get { return (_parent.Value & _mask) > 0; }
            set
            {
                if (value)
                    _parent.Value |= _mask;
                else
                    _parent.Value &= ~_mask;

                // Changing the parent value causes a refresh,
                // so no need to call NotifyPropertyChanged
            }
        }

        public void Refresh()
        {
            NotifyPropertyChanged("IsSet");
        }
    }
}
