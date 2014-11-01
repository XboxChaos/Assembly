using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Assembly.Helpers.TagEditor.Fields;

namespace Assembly.Helpers.TagEditor
{
	/// <summary>
	/// Monitors tag fields for changes made to their value by the user.
	/// </summary>
	public class TagFieldMonitor
	{
		/// <summary>
		/// Names of properties that are monitored for changes.
		/// </summary>
		private static readonly HashSet<string> PropertyNames = new HashSet<string>
		{
			"Value"
		};

		/// <summary>
		/// Initializes a new instance of the <see cref="TagFieldMonitor"/> class.
		/// </summary>
		public TagFieldMonitor()
		{
			Enabled = true;
		}

		/// <summary>
		/// Occurs when a tag field's value is changed.
		/// </summary>
		public event EventHandler<TagFieldChangedEventArgs> TagFieldChanged;

		/// <summary>
		/// Gets or sets whether or not the change tracker is enabled.
		/// While disabled, changes made to attached fields will not be reported.
		/// Defaults to <c>true</c> (enabled).
		/// </summary>
		public bool Enabled { get; set; }

		/// <summary>
		/// Attaches to a collection of fields, listening for changes on them.
		/// </summary>
		/// <param name="fields">The collection of fields to attach to.</param>
		public void AttachTo(IEnumerable<TagField> fields)
		{
			foreach (var field in fields)
				AttachTo(field);
		}

		/// <summary>
		/// Attaches to a single field, listening for changes made to it.
		/// </summary>
		/// <param name="field">The field to attach to.</param>
		public void AttachTo(TagField field)
		{
			field.PropertyChanged += field_PropertyChanged;
		}

		private void field_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (Enabled && PropertyNames.Contains(e.PropertyName))
				OnTagFieldChanged(new TagFieldChangedEventArgs((TagField)sender));
		}

		/// <summary>
		/// Raises the <see cref="E:TagFieldChanged" /> event.
		/// </summary>
		/// <param name="e">The <see cref="TagFieldChangedEventArgs"/> instance containing the event data.</param>
		protected void OnTagFieldChanged(TagFieldChangedEventArgs e)
		{
			if (TagFieldChanged != null)
				TagFieldChanged(this, e);
		}
	}

	/// <summary>
	/// Provides additional information for tag field change events.
	/// </summary>
	public class TagFieldChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TagFieldChangedEventArgs"/> class.
		/// </summary>
		/// <param name="field">The field that changed.</param>
		public TagFieldChangedEventArgs(TagField field)
		{
			Field = field;
		}

		/// <summary>
		/// Gets the field that changed.
		/// </summary>
		public TagField Field { get; private set; }
	}
}
