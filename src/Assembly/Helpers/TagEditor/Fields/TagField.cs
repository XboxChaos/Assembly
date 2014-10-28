using Assembly.Metro.Controls.PageTemplates.Games.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.TagEditor.Fields
{
	/// <summary>
	/// Base class for a field in the tag editor.
	/// </summary>
	public abstract class TagField : PropertyChangeNotifier
	{
		private float _opacity = 1.0f;

		/// <summary>
		/// Gets or sets the field's opacity, as a percentage between 0 and 1.
		/// </summary>
		public float Opacity
		{
			get { return _opacity; }
			set
			{
				_opacity = value;
				NotifyPropertyChanged("Opacity");
			}
		}

		/// <summary>
		/// Gets or sets the plugin line that the original field belonged to. Can be 0.
		/// </summary>
		public uint PluginLine { get; set; }

		public abstract void Accept(ITagFieldVisitor visitor);
	}
}
