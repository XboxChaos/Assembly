using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	internal class FlattenedReflexive
	{
		private readonly FieldChangeSet _changes;
		private readonly List<bool> _fieldVisibility = new List<bool>();
		private readonly ObservableCollection<MetaField> _loadedFields = new ObservableCollection<MetaField>();
		private readonly FlattenedReflexive _parent;
		private readonly List<ReflexiveData> _synchronizedReflexives = new List<ReflexiveData>();
		private readonly ReflexiveData _template;
		private readonly ObservableCollection<MetaField> _topLevelFields;
		private readonly FieldChangeTracker _tracker;
		private readonly List<WrappedReflexiveEntry> _wrappers = new List<WrappedReflexiveEntry>();
		private ReflexiveData _activeReflexive;
		private bool _expanded = true;
		private ReflexivePage _lastPage;

		public FlattenedReflexive(FlattenedReflexive parent, ReflexiveData template,
			ObservableCollection<MetaField> topLevelFields, FieldChangeTracker tracker, FieldChangeSet changes)
		{
			_parent = parent;
			_template = template;
			_activeReflexive = template;
			_synchronizedReflexives.Add(template);
			if (template.HasChildren)
				_lastPage = template.Pages[template.CurrentIndex];
			_topLevelFields = topLevelFields;
			_tracker = tracker;
			_changes = changes;
		}

		public FlattenedReflexive Parent
		{
			get { return _parent; }
		}

		public ObservableCollection<MetaField> LoadedFields
		{
			get { return _loadedFields; }
		}

		public IList<WrappedReflexiveEntry> Wrappers
		{
			get { return _wrappers.AsReadOnly(); }
		}

		/// <summary>
		///     Synchronizes the expansion state of a reflexive with the expansion state of this FlattenedReflexive.
		/// </summary>
		/// <param name="other">The ReflexiveData to synchronize the expansion state of.</param>
		public void SynchronizeWith(ReflexiveData other)
		{
			_synchronizedReflexives.Add(other);
		}

		public WrappedReflexiveEntry WrapField(MetaField field, double width, bool last)
		{
			_loadedFields.Add(field);
			_fieldVisibility.Add(true);
			_tracker.AttachTo(field);

			var wrapper = new WrappedReflexiveEntry(_loadedFields, _wrappers.Count, width, last);
			_wrappers.Add(wrapper);
			return wrapper;
		}

		public bool Expand()
		{
			if (_expanded || _activeReflexive.Length == 0)
				return false;

			_expanded = true;
			SynchronizeExpansion();

			ShowFields(_parent, 0, _wrappers.Count);
			return true;
		}

		public bool Contract()
		{
			if (!_expanded)
				return false;

			_expanded = false;
			SynchronizeExpansion();

			HideFields(_parent, 0, _wrappers.Count);
			return true;
		}

		public void LoadPage(ReflexiveData reflexive, int index)
		{
			_activeReflexive = reflexive;
			if (!reflexive.HasChildren)
				return;

			if (index >= 0 && index < reflexive.Length && reflexive.Pages[index] == _lastPage)
				return;

			UnloadPage();
			if (index < 0 || index >= reflexive.Length)
			{
				_lastPage = null;
				return;
			}

			_lastPage = reflexive.Pages[index];
			for (int i = 0; i < _lastPage.Fields.Length; i++)
			{
				// if _lastPage.Fields[i] is null, then we can just re-use the field from the template
				MetaField newField;
				if (_lastPage.Fields[i] != null)
					newField = _lastPage.Fields[i];
				else
					newField = reflexive.Template[i];

				// HACK: synchronize the opacity
				newField.Opacity = _loadedFields[i].Opacity;

				_loadedFields[i] = newField;
			}
		}

		public WrappedReflexiveEntry GetTopLevelWrapper(WrappedReflexiveEntry wrapper)
		{
			WrappedReflexiveEntry result = wrapper;
			FlattenedReflexive reflexive = _parent;
			while (reflexive != null)
			{
				int index = reflexive._template.Template.IndexOf(result);
				result = reflexive._wrappers[index];
				reflexive = reflexive._parent;
			}
			return result;
		}

		public void UnloadPage()
		{
			if (_lastPage != null)
				_lastPage.CloneChanges(_loadedFields, _tracker, _changes);
			_lastPage = null;
		}

		private void SynchronizeExpansion()
		{
			foreach (ReflexiveData reflexive in _synchronizedReflexives)
				reflexive.IsExpanded = _expanded;
		}

		private void ShowFields(FlattenedReflexive reflexive, int start, int end)
		{
			if (end <= start)
				return;

			if (reflexive != null)
			{
				// Set the visibility of everything
				int baseIndex = reflexive._template.Template.IndexOf(_template) + 1;
				for (int i = start; i < end; i++)
					reflexive._fieldVisibility[baseIndex + i] = _fieldVisibility[i];

				// Update the IsLast states in case we added wrappers after the "last" one
				if (baseIndex + start > 0 && reflexive._wrappers[baseIndex + start - 1].IsLast)
				{
					int lastVisible = reflexive._fieldVisibility.FindLastIndex(v => v);
					if (lastVisible >= 0)
					{
						reflexive._wrappers[baseIndex + start - 1].IsLast = false;
						reflexive._wrappers[lastVisible].IsLast = true;
					}
				}

				// Recurse through ancestors if we're expanded
				if (reflexive._expanded)
					reflexive.ShowFields(reflexive._parent, baseIndex + start, baseIndex + end);
			}
			else
			{
				// Insert our fields into the top-level
				int insertIndex = _topLevelFields.IndexOf(_template) + 1 + GetVisibleFieldOffset(start);
				for (int i = start; i < end; i++)
				{
					if (_fieldVisibility[i])
					{
						_topLevelFields.Insert(insertIndex, _wrappers[i]);
						insertIndex++;
					}
				}
			}
		}

		private void HideFields(FlattenedReflexive reflexive, int start, int end)
		{
			if (end <= start)
				return;

			if (reflexive != null)
			{
				int baseIndex = reflexive._template.Template.IndexOf(_template) + 1;
				if (reflexive._expanded)
					reflexive.HideFields(reflexive._parent, baseIndex + start, baseIndex + end);

				bool adjustLast = false;
				for (int i = start; i < end; i++)
				{
					reflexive._fieldVisibility[baseIndex + i] = false;
					if (reflexive._wrappers[baseIndex + i].IsLast)
					{
						reflexive._wrappers[baseIndex + i].IsLast = false;
						adjustLast = true;
					}
				}

				if (adjustLast && start + baseIndex > 0)
				{
					int lastVisible = reflexive._fieldVisibility.FindLastIndex(start + baseIndex - 1, v => v);
					if (lastVisible >= 0)
						reflexive._wrappers[lastVisible].IsLast = true;
				}
			}
			else
			{
				int baseIndex = _topLevelFields.IndexOf(_template) + 1 + GetVisibleFieldOffset(start);
				for (int i = start; i < end; i++)
				{
					if (_fieldVisibility[i])
						_topLevelFields.RemoveAt(baseIndex);
				}
			}
		}

		private int GetVisibleFieldOffset(int wrapperIndex)
		{
			int offset = 0;
			for (int i = 0; i < wrapperIndex; i++)
			{
				if (_fieldVisibility[i])
					offset++;
			}
			return offset;
		}
	}

	public class ReflexiveFlattener : IMetaFieldVisitor
	{
		private readonly FieldChangeSet _changes;

		private readonly Dictionary<ReflexiveData, FlattenedReflexive> _flattenInfo =
			new Dictionary<ReflexiveData, FlattenedReflexive>();

		private readonly MetaReader _reader;
		private readonly FieldChangeTracker _tracker;
		private ObservableCollection<MetaField> _fields;
		private FlattenedReflexive _flatParent;
		private int _index;
		private bool _loading;
		private ObservableCollection<MetaField> _topLevelFields;

		public ReflexiveFlattener(MetaReader reader, FieldChangeTracker tracker, FieldChangeSet changes)
		{
			_reader = reader;
			_tracker = tracker;
			_changes = changes;
		}

		public void VisitBitfield(BitfieldData field)
		{
		}

		public void VisitComment(CommentData field)
		{
		}

		public void VisitEnum(EnumData field)
		{
		}

		public void VisitUint8(Uint8Data field)
		{
		}

		public void VisitInt8(Int8Data field)
		{
		}

		public void VisitUint16(Uint16Data field)
		{
		}

		public void VisitInt16(Int16Data field)
		{
		}

		public void VisitUint32(Uint32Data field)
		{
		}

		public void VisitInt32(Int32Data field)
		{
		}

		public void VisitFloat32(Float32Data field)
		{
		}

		public void VisitColourInt(ColourData field)
		{
		}

		public void VisitColourFloat(ColourData field)
		{
		}

		public void VisitReflexive(ReflexiveData field)
		{
			// Create flatten information for the reflexive and attach event handlers to it
			var flattened = new FlattenedReflexive(_flatParent, field, _topLevelFields, _tracker, _changes);
			AttachTo(field, flattened);

			FlattenedReflexive oldParent = _flatParent;
			_flatParent = flattened;
			Flatten(field.Template);
			field.UpdateWidth();
			_flatParent = oldParent;

			for (int i = 0; i < field.Template.Count; i++)
			{
				WrappedReflexiveEntry wrapper = flattened.WrapField(field.Template[i], field.Width, i == field.Template.Count - 1);
				_index++;
				_fields.Insert(_index, wrapper);
			}
		}

		public void VisitReflexiveEntry(WrappedReflexiveEntry field)
		{
		}

		public void VisitString(StringData field)
		{
		}

		public void VisitStringID(StringIDData field)
		{
		}

		public void VisitRawData(RawData field)
		{
		}

		public void VisitDataRef(DataRef field)
		{
		}

		public void VisitTagRef(TagRefData field)
		{
		}

		public void VisitVector(VectorData field)
		{
		}

		public void VisitDegree(DegreeData field)
		{
		}

		public void VisitShaderRef(ShaderRef field)
		{
		}

		public void Flatten(ObservableCollection<MetaField> fields)
		{
			if (_topLevelFields == null)
				_topLevelFields = fields;

			int oldIndex = _index;
			ObservableCollection<MetaField> oldFields = _fields;

			_fields = fields;
			for (_index = 0; _index < fields.Count; _index++)
				fields[_index].Accept(this);

			_fields = oldFields;
			_index = oldIndex;

			if (_topLevelFields == fields)
				_topLevelFields = null;
		}

		public void EnumWrappers(ReflexiveData reflexive, Action<WrappedReflexiveEntry> wrapperProcessor)
		{
			FlattenedReflexive flattened;
			if (!_flattenInfo.TryGetValue(reflexive, out flattened))
				return;

			foreach (WrappedReflexiveEntry wrapper in flattened.Wrappers)
				wrapperProcessor(wrapper);
		}

		public WrappedReflexiveEntry GetTopLevelWrapper(ReflexiveData reflexive, WrappedReflexiveEntry wrapper)
		{
			FlattenedReflexive flattened;
			if (_flattenInfo.TryGetValue(reflexive, out flattened))
				return flattened.GetTopLevelWrapper(wrapper);
			return null;
		}

		/// <summary>
		///     Forcibly expands a reflexive and all of its ancestors.
		/// </summary>
		/// <param name="reflexive">The reflexive to make visible.</param>
		public void ForceVisible(ReflexiveData reflexive)
		{
			FlattenedReflexive flattened;
			if (!_flattenInfo.TryGetValue(reflexive, out flattened))
				return;

			while (flattened != null && flattened.Expand())
				flattened = flattened.Parent;
		}

		private void AttachTo(ReflexiveData field, FlattenedReflexive flattened)
		{
			field.PropertyChanged += ReflexivePropertyChanged;
			field.Cloned += ReflexiveCloned;
			_flattenInfo[field] = flattened;
		}

		private void ReflexiveCloned(object sender, FieldCachedEventArgs e)
		{
			FlattenedReflexive flattened = _flattenInfo[e.Old];
			AttachTo(e.Clone, flattened);
			flattened.SynchronizeWith(e.Clone);
		}

		private void ReflexivePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var reflexive = (ReflexiveData) sender;
			FlattenedReflexive flattenedField = _flattenInfo[reflexive];
			if (e.PropertyName == "IsExpanded")
			{
				if (reflexive.IsExpanded)
					flattenedField.Expand();
				else
					flattenedField.Contract();
			}
			else if (!_loading && (e.PropertyName == "CurrentIndex" || e.PropertyName == "FirstEntryAddress" || e.PropertyName == "EntrySize"))
			{
				_loading = true;
				_tracker.Enabled = false;

				if (e.PropertyName == "FirstEntryAddress")
				{
					// Throw out any cached changes and reset the current index
					RecursiveReset(flattenedField.LoadedFields);
					if (reflexive.Length > 0)
						reflexive.CurrentIndex = 0;
					else
						reflexive.CurrentIndex = -1;
				}
				else
				{
					// Cache any changes made to the current page
					RecursiveUnload(flattenedField.LoadedFields);
				}

				// Load the new page in
				flattenedField.LoadPage(reflexive, reflexive.CurrentIndex);

				// Read any non-cached fields in the page
				if (_reader != null)
					_reader.ReadReflexiveChildren(reflexive);
				RecursiveLoad(flattenedField.LoadedFields);

				_tracker.Enabled = true;
				_loading = false;
			}
		}

		private void RecursiveUnload(IEnumerable<MetaField> fields)
		{
			foreach (MetaField field in fields)
			{
				var reflexive = field as ReflexiveData;
				if (reflexive != null)
				{
					FlattenedReflexive flattened = _flattenInfo[reflexive];
					RecursiveUnload(flattened.LoadedFields);
					_flattenInfo[reflexive].UnloadPage();
				}
			}
		}

		private void RecursiveReset(IEnumerable<MetaField> fields)
		{
			foreach (MetaField field in fields)
			{
				_tracker.MarkUnchanged(field);

				var reflexive = field as ReflexiveData;
				if (reflexive != null)
				{
					FlattenedReflexive flattened = _flattenInfo[reflexive];
					RecursiveReset(flattened.LoadedFields);
					reflexive.ResetPages();
				}
			}
		}

		private void RecursiveLoad(IEnumerable<MetaField> fields)
		{
			foreach (MetaField field in fields)
			{
				var reflexive = field as ReflexiveData;
				if (reflexive != null)
				{
					FlattenedReflexive flattened = _flattenInfo[reflexive];
					_flattenInfo[reflexive].LoadPage(reflexive, reflexive.CurrentIndex);
					RecursiveLoad(flattened.LoadedFields);
				}
			}
		}
	}
}