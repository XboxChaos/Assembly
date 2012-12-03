using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Extryze.Plugins
{
    public abstract class NamedPluginEntry : PluginEntry
    {
        private string _name;

        public NamedPluginEntry(string name, int offset, bool visible)
            : base(offset, visible)
        {
            _name = name;
        }

        public string Name
        {
            get { return _name; }
        }
    }
}
