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
        /// Clones the field such that the clone will have the same value as the original field when shown in the editor,
        /// but so that editing the clone's value as shown in the editor will not alter the value of the original field.
        /// </summary>
        /// <returns>The cloned field with a value identical to the source field.</returns>
        public abstract MetaField CloneValue();

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
