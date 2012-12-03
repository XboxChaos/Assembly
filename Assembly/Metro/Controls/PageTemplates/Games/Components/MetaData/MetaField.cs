using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    /// <summary>
    /// Base class for meta field data.
    /// </summary>
    public abstract class MetaField : PropertyChangeNotifier
    {
        private float _opacity = 1.0f;

        public abstract void Accept(IMetaFieldVisitor visitor);

        /// <summary>
        /// Deeply clones the MetaField. Any fields contained inside of it will
        /// be cloned as well.
        /// </summary>
        /// <returns>The new clone.</returns>
        public abstract MetaField DeepClone();

        /// <summary>
        /// Returns true if the field's value has been altered.
        /// </summary>
        public abstract bool HasChanged { get; }

        /// <summary>
        /// Resets the field's value to its original one.
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// Marks the value as unchanged, setting its original value to the current one.
        /// </summary>
        public abstract void KeepChanges();

        /// <summary>
        /// The field's opacity, as a percentage between 0 and 1.
        /// </summary>
        public float Opacity
        {
            get { return _opacity; }
            set { _opacity = value; NotifyPropertyChanged("Opacity"); }
        }
    }
}
