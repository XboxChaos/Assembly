using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    /// <summary>
    /// Abstract base class for meta fields that have values associated with them.
    /// </summary>
    public abstract class ValueField : MetaField
    {
        private string _name;
        private uint _offset, _pluginLine, _address;

        public ValueField(string name, uint offset, uint address, uint pluginLine)
        {
            _name = name;
            _offset = offset;
            _address = address;
            _pluginLine = pluginLine;
        }

        /// <summary>
        /// The value's name.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyPropertyChanged("Name"); }
        }

        /// <summary>
        /// The line from the plugin that created the value.
        /// </summary>
        public uint PluginLine
        {
            get { return _pluginLine; }
            set { _pluginLine = value; NotifyPropertyChanged("PluginLine"); }
        }

        /// <summary>
        /// The offset, from the start of the current meta block or reflexive, of the field's value.
        /// </summary>
        public uint Offset
        {
            get { return _offset; }
            set { _offset = value; NotifyPropertyChanged("Offset"); }
        }

        /// <summary>
        /// The estimated memory address of the field itself.
        /// Do not rely upon the accuracy of this value, especially when saving or poking.
        /// </summary>
        public uint FieldAddress
        {
            get { return _address; }
            set { _address = value; NotifyPropertyChanged("FieldAddress"); }
        }
    }
}