using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    /// <summary>
    /// A set of fields that have been changed.
    /// </summary>
    /// <seealso cref="FieldChangeTracker"/>
    public class FieldChangeSet
    {
        private HashSet<MetaField> _fields = new HashSet<MetaField>();

        /// <summary>
        /// Marks a field as changed.
        /// </summary>
        /// <param name="field">The field to mark as changed.</param>
        public void MarkChanged(MetaField field)
        {
            _fields.Add(field);
        }

        /// <summary>
        /// Marks a field as unchanged.
        /// </summary>
        /// <param name="field">The field to mark as unchanged.</param>
        public void MarkUnchanged(MetaField field)
        {
            _fields.Remove(field);
        }

        /// <summary>
        /// Returns whether or not a field is marked as changed.
        /// </summary>
        /// <param name="field">The field to check.</param>
        /// <returns>true if the field has been marked as changed.</returns>
        public bool HasChanged(MetaField field)
        {
            return _fields.Contains(field);
        }

        /// <summary>
        /// Marks all fields as unchanged.
        /// </summary>
        public void MarkAllUnchanged()
        {
            _fields.Clear();
        }
    }
}
