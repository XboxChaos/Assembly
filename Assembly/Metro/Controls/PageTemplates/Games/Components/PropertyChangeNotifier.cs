using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
    public class PropertyChangeNotifier : INotifyPropertyChanged
    {
        private Dictionary<string, PropertyChangedEventArgs> _argsCache = new Dictionary<string, PropertyChangedEventArgs>();

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for a property.
        /// </summary>
        /// <param name="name">The property's name.</param>
        protected void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, GetArgs(name));
        }

        private PropertyChangedEventArgs GetArgs(string propertyName)
        {
            PropertyChangedEventArgs result;
            if (_argsCache.TryGetValue(propertyName, out result))
                return result;

            result = new PropertyChangedEventArgs(propertyName);
            _argsCache[propertyName] = result;
            return result;
        }
    }
}
