using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Extryze.Plugins
{
    public class BasicPluginEntry : NamedPluginEntry
    {
        private BasicPluginType _type;

        public BasicPluginEntry(string name, int offset, bool visible, BasicPluginType type)
            : base(name, offset, visible)
        {
            _type = type;
        }

        public BasicPluginType Type
        {
            get { return _type; }
        }

        public override void Accept(IPluginEntryVisitor visitor)
        {
            visitor.VisitBasicEntry(this);
        }
    }
}
