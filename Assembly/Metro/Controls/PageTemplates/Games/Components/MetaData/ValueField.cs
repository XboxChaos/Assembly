using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public abstract class ValueField : MetaField
    {
        private string _name;
        private uint _offset, _memoryAddress, _cacheOffset, _pluginLine;

        public ValueField(string name, uint offset, uint pluginLine)
        {
            _name = name;
            _offset = offset;
            _pluginLine = pluginLine;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyPropertyChanged("Name"); }
        }

        public uint PluginLine
        {
            get { return _pluginLine; }
            set { _pluginLine = value; NotifyPropertyChanged("PluginLine"); }
        }

        public uint Offset
        {
            get { return _offset; }
            set { _offset = value; NotifyPropertyChanged("Offset"); }
        }

        /// <summary>
        /// Returns the offset of the value in Memory
        /// </summary>
        public uint MemoryAddress
        {
            get { return _memoryAddress; }
            set { _memoryAddress = value; }
        }

        /// <summary>
        /// Returns the offset of the value in Memory
        /// </summary>
        public uint CacheOffset
        {
            get { return _cacheOffset; }
            set { _cacheOffset = value; }
        }
    }
}