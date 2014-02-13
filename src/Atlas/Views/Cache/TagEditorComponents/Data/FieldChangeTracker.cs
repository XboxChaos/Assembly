using System.Collections.Generic;
using System.ComponentModel;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	/// <summary>
	///     Listens for and records changes made to meta fields.
	/// </summary>
	/// <seealso cref="FieldChangeSet" />
	public class FieldChangeTracker
	{
		private static HashSet<string> _propertyNames; // Names of properties that will be monitored for changes

		private readonly List<FieldChangeSet> _changeSets = new List<FieldChangeSet>(); // Registered change sets

		/// <summary>
		///     Constructs a new FieldChangeTracker.
		/// </summary>
		public FieldChangeTracker()
		{
			Enabled = true;
			RegisterPropertyNames();
		}

		/// <summary>
		///     Gets or sets whether or not the change tracker is enabled.
		///     While disabled, changes made to attached fields will not be recorded.
		///     Defaults to true.
		/// </summary>
		public bool Enabled { get; set; }

		/// <summary>
		///     Registers a FieldChangeSet with the tracker.
		///     Any time a field is changed, it will be added to all registered sets.
		/// </summary>
		/// <param name="set">The FieldChangeSet to register.</param>
		/// <seealso cref="UnregisterChangeSets" />
		public void RegisterChangeSet(FieldChangeSet set)
		{
			_changeSets.Add(set);
		}

		/// <summary>
		///     Unregisters all registered FieldChangeSets.
		/// </summary>
		/// <seealso cref="RegisterChangeSet" />
		public void UnregisterChangeSets()
		{
			_changeSets.Clear();
		}

		/// <summary>
		///     Attaches to a collection of fields, listening for changes on them.
		/// </summary>
		/// <param name="fields">The collection of fields to attach to.</param>
		public void Attach(IEnumerable<TagDataField> fields)
		{
			foreach (TagDataField field in fields)
				AttachTo(field);
		}

		/// <summary>
		///     Attaches to a single field, listening for changes made to it.
		/// </summary>
		/// <param name="field">The field to attach to.</param>
		public void AttachTo(TagDataField field)
		{
			field.PropertyChanged += field_PropertyChanged;

			// If the field is a raw data field (raw or dataref),
			// then subscribe to the TextChanged event on its document
			var rawField = field as RawData;
			if (rawField != null)
			{
				rawField.Document.TextChanged += delegate
				{
					if (Enabled)
						MarkChanged(rawField);
				};
			}
		}

		/// <summary>
		///     Marks a field as changed in all registered change sets.
		/// </summary>
		/// <param name="field">The field to mark as changed.</param>
		public void MarkChanged(TagDataField field)
		{
			foreach (FieldChangeSet set in _changeSets)
				set.MarkChanged(field);
		}

		/// <summary>
		///     Marks a field as unchanged in all registered change sets.
		/// </summary>
		/// <param name="field">The field to mark as unchanged.</param>
		public void MarkUnchanged(TagDataField field)
		{
			foreach (FieldChangeSet set in _changeSets)
				set.MarkUnchanged(field);
		}

		private void field_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (Enabled && _propertyNames.Contains(e.PropertyName))
				MarkChanged((TagDataField) sender);
		}

		/// <summary>
		///     Adds names of properties to look for to the _propertyNames set
		///     if it hasn't been created yet.
		/// </summary>
		private void RegisterPropertyNames()
		{
			if (_propertyNames != null)
				return;
			_propertyNames = new HashSet<string>();

			// All right, so this is dependent upon the exact property names,
			// but IOnPropertyChanged has that problem anyway...
			_propertyNames.Add("Value");
			_propertyNames.Add("DataAddress"); // Datarefs
			_propertyNames.Add("Length"); // Tag Blocks
			_propertyNames.Add("EntrySize"); // Tag Blocks
			_propertyNames.Add("FirstEntryAddress"); // Tag Blocks
			_propertyNames.Add("Class"); // Tagrefs
			_propertyNames.Add("Degree"); // Degrees
			_propertyNames.Add("Radian"); // Degrees
			_propertyNames.Add("X"); // Vectors
			_propertyNames.Add("Y"); // Vectors
			_propertyNames.Add("Z"); // Vectors
		}
	}
}