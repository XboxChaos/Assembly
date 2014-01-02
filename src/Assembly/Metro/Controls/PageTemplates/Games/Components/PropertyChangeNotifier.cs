using System.Collections.Generic;
using System.ComponentModel;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
	public class PropertyChangeNotifier : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		///     Raises the PropertyChanged event for a property.
		/// </summary>
		/// <param name="name">The property's name.</param>
		protected void NotifyPropertyChanged(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}
	}
}