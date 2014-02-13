using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	internal class FlattenedTagBlock
	{
		private readonly FieldChangeSet _changes;
		private readonly List<bool> _fieldVisibility = new List<bool>();
		private readonly ObservableCollection<TagDataField> _loadedFields = new ObservableCollection<TagDataField>();
		private readonly FlattenedTagBlock _parent;
		private readonly List<TagBlockData> _synchronizedTagBlocks = new List<TagBlockData>();
		private readonly TagBlockData _template;
		private readonly ObservableCollection<TagDataField> _topLevelFields;
		private readonly FieldChangeTracker _tracker;
		private readonly List<WrappedTagBlockEntry> _wrappers = new List<WrappedTagBlockEntry>();
		private TagBlockData _activeTagBlock;
		private bool _expanded = true;
		private TagBlockPage _lastPage;

		public FlattenedTagBlock(FlattenedTagBlock parent, TagBlockData template,
			ObservableCollection<TagDataField> topLevelFields, FieldChangeTracker tracker, FieldChangeSet changes)
		{
			_parent = parent;
			_template = template;
			_activeTagBlock = template;
			_synchronizedTagBlocks.Add(template);
			if (template.HasChildren)
				_lastPage = template.Pages[template.CurrentIndex];
			_topLevelFields = topLevelFields;
			_tracker = tracker;
			_changes = changes;
		}

		public FlattenedTagBlock Parent
		{
			get { return _parent; }
		}

		public ObservableCollection<TagDataField> LoadedFields
		{
			get { return _loadedFields; }
		}

		public IList<WrappedTagBlockEntry> Wrappers
		{
			get { return _wrappers.AsReadOnly(); }
		}

		/// <summary>
		///     Synchronizes the expansion state of a reflexive with the expansion state of this FlattenedReflexive.
		/// </summary>
		/// <param name="other">The ReflexiveData to synchronize the expansion state of.</param>
		public void SynchronizeWith(TagBlockData other)
		{
			_synchronizedTagBlocks.Add(other);
		}

		public WrappedTagBlockEntry WrapField(TagDataField field, double width, bool last)
		{
			_loadedFields.Add(field);
			_fieldVisibility.Add(true);
			_tracker.AttachTo(field);

			var wrapper = new WrappedTagBlockEntry(_loadedFields, _wrappers.Count, width, last);
			_wrappers.Add(wrapper);
			return wrapper;
		}

		public bool Expand()
		{
			if (_expanded || _activeTagBlock.Length == 0)
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

		public void LoadPage(TagBlockData tagBlock, int index)
		{
			_activeTagBlock = tagBlock;
			if (!tagBlock.HasChildren)
				return;

			if (index >= 0 && index < tagBlock.Length && tagBlock.Pages[index] == _lastPage)
				return;

			UnloadPage();
			if (index < 0 || index >= tagBlock.Length)
			{
				_lastPage = null;
				return;
			}

			_lastPage = tagBlock.Pages[index];
			for (int i = 0; i < _lastPage.Fields.Length; i++)
			{
				// if _lastPage.Fields[i] is null, then we can just re-use the field from the template
				TagDataField newField;
				if (_lastPage.Fields[i] != null)
					newField = _lastPage.Fields[i];
				else
					newField = tagBlock.Template[i];

				// HACK: synchronize the opacity
				newField.Opacity = _loadedFields[i].Opacity;

				_loadedFields[i] = newField;
			}
		}

		public WrappedTagBlockEntry GetTopLevelWrapper(WrappedTagBlockEntry wrapper)
		{
			WrappedTagBlockEntry result = wrapper;
			FlattenedTagBlock tagBlock = _parent;
			while (tagBlock != null)
			{
				int index = tagBlock._template.Template.IndexOf(result);
				result = tagBlock._wrappers[index];
				tagBlock = tagBlock._parent;
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
			foreach (TagBlockData reflexive in _synchronizedTagBlocks)
				reflexive.IsExpanded = _expanded;
		}

		private void ShowFields(FlattenedTagBlock reflexive, int start, int end)
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

		private void HideFields(FlattenedTagBlock TagBlock, int start, int end)
		{
			if (end <= start)
				return;

			if (TagBlock != null)
			{
				int baseIndex = TagBlock._template.Template.IndexOf(_template) + 1;
				if (TagBlock._expanded)
					TagBlock.HideFields(TagBlock._parent, baseIndex + start, baseIndex + end);

				bool adjustLast = false;
				for (int i = start; i < end; i++)
				{
					TagBlock._fieldVisibility[baseIndex + i] = false;
					if (TagBlock._wrappers[baseIndex + i].IsLast)
					{
						TagBlock._wrappers[baseIndex + i].IsLast = false;
						adjustLast = true;
					}
				}

				if (adjustLast && start + baseIndex > 0)
				{
					int lastVisible = TagBlock._fieldVisibility.FindLastIndex(start + baseIndex - 1, v => v);
					if (lastVisible >= 0)
						TagBlock._wrappers[lastVisible].IsLast = true;
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

	public class TagBlockFlattener : ITagDataFieldVisitor
	{
		private readonly FieldChangeSet _changes;

		private readonly Dictionary<TagBlockData, FlattenedTagBlock> _flattenInfo =
			new Dictionary<TagBlockData, FlattenedTagBlock>();

		private readonly TagDataReader _reader;
		private readonly FieldChangeTracker _tracker;
		private ObservableCollection<TagDataField> _fields;
		private FlattenedTagBlock _flatParent;
		private int _index;
		private bool _loading;
		private ObservableCollection<TagDataField> _topLevelFields;

		public TagBlockFlattener(TagDataReader reader, FieldChangeTracker tracker, FieldChangeSet changes)
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

		public void VisitReflexive(TagBlockData field)
		{
			// Create flatten information for the reflexive and attach event handlers to it
			var flattened = new FlattenedTagBlock(_flatParent, field, _topLevelFields, _tracker, _changes);
			AttachTo(field, flattened);

			FlattenedTagBlock oldParent = _flatParent;
			_flatParent = flattened;
			Flatten(field.Template);
			field.UpdateWidth();
			_flatParent = oldParent;

			for (int i = 0; i < field.Template.Count; i++)
			{
				WrappedTagBlockEntry wrapper = flattened.WrapField(field.Template[i], field.Width, i == field.Template.Count - 1);
				_index++;
				_fields.Insert(_index, wrapper);
			}
		}

		public void VisitReflexiveEntry(WrappedTagBlockEntry field)
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

		public void Flatten(ObservableCollection<TagDataField> fields)
		{
			if (_topLevelFields == null)
				_topLevelFields = fields;

			int oldIndex = _index;
			ObservableCollection<TagDataField> oldFields = _fields;

			_fields = fields;
			for (_index = 0; _index < fields.Count; _index++)
				fields[_index].Accept(this);

			_fields = oldFields;
			_index = oldIndex;

			if (_topLevelFields == fields)
				_topLevelFields = null;
		}

		public void EnumWrappers(TagBlockData tagBlock, Action<WrappedTagBlockEntry> wrapperProcessor)
		{
			FlattenedTagBlock flattened;
			if (!_flattenInfo.TryGetValue(tagBlock, out flattened))
				return;

			foreach (WrappedTagBlockEntry wrapper in flattened.Wrappers)
				wrapperProcessor(wrapper);
		}

		public WrappedTagBlockEntry GetTopLevelWrapper(TagBlockData tagBlock, WrappedTagBlockEntry wrapper)
		{
			FlattenedTagBlock flattened;
			if (_flattenInfo.TryGetValue(tagBlock, out flattened))
				return flattened.GetTopLevelWrapper(wrapper);
			return null;
		}

		/// <summary>
		///     Forcibly expands a tagBlock and all of its ancestors.
		/// </summary>
		/// <param name="tagBlock">The tagBlock to make visible.</param>
		public void ForceVisible(TagBlockData tagBlock)
		{
			FlattenedTagBlock flattened;
			if (!_flattenInfo.TryGetValue(tagBlock, out flattened))
				return;

			while (flattened != null && flattened.Expand())
				flattened = flattened.Parent;
		}

		private void AttachTo(TagBlockData field, FlattenedTagBlock flattened)
		{
			field.PropertyChanged += TagBlockPropertyChanged;
			field.Cloned += TagBlockCloned;
			_flattenInfo[field] = flattened;
		}

		private void TagBlockCloned(object sender, TagBlockClonedEventArgs e)
		{
			FlattenedTagBlock flattened = _flattenInfo[e.Old];
			AttachTo(e.Clone, flattened);
			flattened.SynchronizeWith(e.Clone);
		}

		private void TagBlockPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var tagBlock = (TagBlockData)sender;
			FlattenedTagBlock flattenedField = _flattenInfo[tagBlock];
			if (e.PropertyName == "IsExpanded")
			{
				if (tagBlock.IsExpanded)
					flattenedField.Expand();
				else
					flattenedField.Contract();
			}
			else if (!_loading &&
			         (e.PropertyName == "CurrentIndex" || e.PropertyName == "FirstEntryAddress" || e.PropertyName == "EntrySize"))
			{
				_loading = true;
				_tracker.Enabled = false;

				if (e.PropertyName == "FirstEntryAddress")
				{
					// Throw out any cached changes and reset the current index
					RecursiveReset(flattenedField.LoadedFields);
					if (tagBlock.Length > 0)
						tagBlock.CurrentIndex = 0;
					else
						tagBlock.CurrentIndex = -1;
				}
				else
				{
					// Cache any changes made to the current page
					RecursiveUnload(flattenedField.LoadedFields);
				}

				// Load the new page in
				flattenedField.LoadPage(tagBlock, tagBlock.CurrentIndex);

				// Read any non-cached fields in the page
				_reader.ReadTagBlockChildren(tagBlock);
				RecursiveLoad(flattenedField.LoadedFields);

				_tracker.Enabled = true;
				_loading = false;
			}
		}

		private void RecursiveUnload(IEnumerable<TagDataField> fields)
		{
			foreach (var field in fields)
			{
				var tagBlock = field as TagBlockData;
				if (tagBlock == null) continue;

				var flattened = _flattenInfo[tagBlock];
				RecursiveUnload(flattened.LoadedFields);
				_flattenInfo[tagBlock].UnloadPage();
			}
		}

		private void RecursiveReset(IEnumerable<TagDataField> fields)
		{
			foreach (var field in fields)
			{
				_tracker.MarkUnchanged(field);

				var tagBlock = field as TagBlockData;
				if (tagBlock == null) continue;

				var flattened = _flattenInfo[tagBlock];
				RecursiveReset(flattened.LoadedFields);
				tagBlock.ResetPages();
			}
		}

		private void RecursiveLoad(IEnumerable<TagDataField> fields)
		{
			foreach (var field in fields)
			{
				var tagBlock = field as TagBlockData;
				if (tagBlock == null) continue;

				var flattened = _flattenInfo[tagBlock];
				_flattenInfo[tagBlock].LoadPage(tagBlock, tagBlock.CurrentIndex);
				RecursiveLoad(flattened.LoadedFields);
			}
		}
	}
}