using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Extryze.Plugins
{
    /// <summary>
    /// Abstract base class representing a field in a plugin.
    /// </summary>
    public abstract class PluginEntry
    {
        private int _offset;
        private bool _visible;

        /// <summary>
        /// Constructs a PluginEntry's base.
        /// </summary>
        /// <param name="offset">The entry's offset</param>
        /// <param name="visible">Whether or not the entry is visible by default</param>
        public PluginEntry(int offset, bool visible)
        {
            _offset = offset;
            _visible = visible;
        }

        /// <summary>
        /// Whether or not the entry should be visible by default.
        /// </summary>
        public bool Visible
        {
            get { return _visible; }
        }

        /// <summary>
        /// The field's offset from the start of its group.
        /// </summary>
        public int Offset
        {
            get { return _offset; }
        }

        /// <summary>
        /// Calls the IPluginEntryVisitor method corresponding to this entry's type.
        /// </summary>
        /// <param name="visitor">The IPluginEntryVisitor to call.</param>
        public abstract void Accept(IPluginEntryVisitor visitor);
    }
}
