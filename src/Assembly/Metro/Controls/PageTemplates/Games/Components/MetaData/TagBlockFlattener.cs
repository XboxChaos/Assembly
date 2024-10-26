using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	internal class FlattenedTagBlock
	{
		private readonly FieldChangeSet _changes;
		private readonly List<bool> _fieldVisibility = new List<bool>();
		private readonly ObservableCollection<MetaField> _loadedFields = new ObservableCollection<MetaField>();
		private readonly FlattenedTagBlock _parent;
		private readonly List<TagBlockData> _synchronizedTagBlocks = new List<TagBlockData>();
		private readonly TagBlockData _template;
		private readonly ObservableCollection<MetaField> _topLevelFields;
		private readonly FieldChangeTracker _tracker;
		private readonly List<WrappedTagBlockEntry> _wrappers = new List<WrappedTagBlockEntry>();
		private TagBlockData _activeTagBlock;
		private bool _expanded = true;
		private TagBlockPage _lastPage;

		public FlattenedTagBlock(FlattenedTagBlock parent, TagBlockData template,
			ObservableCollection<MetaField> topLevelFields, FieldChangeTracker tracker, FieldChangeSet changes)
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

		public ObservableCollection<MetaField> LoadedFields
		{
			get { return _loadedFields; }
		}

		public IList<WrappedTagBlockEntry> Wrappers
		{
			get { return _wrappers.AsReadOnly(); }
		}

		/// <summary>
		///     Synchronizes the expansion state of a block with the expansion state of this FlattenedTagBlock.
		/// </summary>
		/// <param name="other">The TagBlock to synchronize the expansion state of.</param>
		public void SynchronizeWith(TagBlockData other)
		{
			_synchronizedTagBlocks.Add(other);
		}

		public WrappedTagBlockEntry WrapField(MetaField field, double width, bool last)
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

		public void LoadPage(TagBlockData block, int index)
		{
			_activeTagBlock = block;
			if (!block.HasChildren)
				return;

			if (index >= 0 && index < block.Length && block.Pages[index] == _lastPage)
				return;

			UnloadPage();
			if (index < 0 || index >= block.Length)
			{
				_lastPage = null;
				return;
			}

			_lastPage = block.Pages[index];
			for (int i = 0; i < _lastPage.Fields.Length; i++)
			{
				// if _lastPage.Fields[i] is null, then we can just re-use the field from the template
				MetaField newField;
				if (_lastPage.Fields[i] != null)
					newField = _lastPage.Fields[i];
				else
					newField = block.Template[i];

				// HACK: synchronize the opacity
				newField.Opacity = _loadedFields[i].Opacity;

				_loadedFields[i] = newField;
			}
		}

		public WrappedTagBlockEntry GetTopLevelWrapper(WrappedTagBlockEntry wrapper)
		{
			WrappedTagBlockEntry result = wrapper;
			FlattenedTagBlock block = _parent;
			while (block != null)
			{
				int index = block._template.Template.IndexOf(result);
				result = block._wrappers[index];
				block = block._parent;
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
			foreach (TagBlockData block in _synchronizedTagBlocks)
				block.IsExpanded = _expanded;
		}

		private void ShowFields(FlattenedTagBlock block, int start, int end)
		{
			if (end <= start)
				return;

			if (block != null)
			{
				// Set the visibility of everything
				int baseIndex = block._template.Template.IndexOf(_template) + 1;
				for (int i = start; i < end; i++)
					block._fieldVisibility[baseIndex + i] = _fieldVisibility[i];

				// Update the IsLast states in case we added wrappers after the "last" one
				if (baseIndex + start > 0 && block._wrappers[baseIndex + start - 1].IsLast)
				{
					int lastVisible = block._fieldVisibility.FindLastIndex(v => v);
					if (lastVisible >= 0)
					{
						block._wrappers[baseIndex + start - 1].IsLast = false;
						block._wrappers[lastVisible].IsLast = true;
					}
				}

				// Recurse through ancestors if we're expanded
				if (block._expanded)
					block.ShowFields(block._parent, baseIndex + start, baseIndex + end);
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

		private void HideFields(FlattenedTagBlock block, int start, int end)
		{
			if (end <= start)
				return;

			if (block != null)
			{
				int baseIndex = block._template.Template.IndexOf(_template) + 1;
				if (block._expanded)
					block.HideFields(block._parent, baseIndex + start, baseIndex + end);

				bool adjustLast = false;
				for (int i = start; i < end; i++)
				{
					block._fieldVisibility[baseIndex + i] = false;
					if (block._wrappers[baseIndex + i].IsLast)
					{
						block._wrappers[baseIndex + i].IsLast = false;
						adjustLast = true;
					}
				}

				if (adjustLast && start + baseIndex > 0)
				{
					int lastVisible = block._fieldVisibility.FindLastIndex(start + baseIndex - 1, v => v);
					if (lastVisible >= 0)
						block._wrappers[lastVisible].IsLast = true;
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

	public class TagBlockFlattener : IMetaFieldVisitor
	{
		private readonly FieldChangeSet _changes;

		private readonly Dictionary<TagBlockData, FlattenedTagBlock> _flattenInfo =
			new Dictionary<TagBlockData, FlattenedTagBlock>();

		private readonly MetaReader _reader;
		private readonly FieldChangeTracker _tracker;
		private ObservableCollection<MetaField> _fields;
		private FlattenedTagBlock _flatParent;
		private int _index;
		private bool _loading;
		private ObservableCollection<MetaField> _topLevelFields;

		public TagBlockFlattener(MetaReader reader, FieldChangeTracker tracker, FieldChangeSet changes)
		{
			_reader = reader;
			_tracker = tracker;
			_changes = changes;
		}

		public void VisitFlags(FlagData field)
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

		public void VisitUint64(Uint64Data field)
		{
		}

		public void VisitInt64(Int64Data field)
		{
		}

		public void VisitFloat32(Float32Data field)
		{
		}

		public void VisitColourInt(ColorData field)
		{
		}

		public void VisitColourFloat(ColorData field)
		{
		}

		public void VisitTagBlock(TagBlockData field)
		{
			// Create flatten information for the block and attach event handlers to it
			var flattened = new FlattenedTagBlock(_flatParent, field, _topLevelFields, _tracker, _changes);
			AttachToTagBlock(field, flattened);

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

		public void VisitTagBlockEntry(WrappedTagBlockEntry field)
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
			field.PropertyChanged += DataRefPropertyChanged;

		}

		public void VisitTagRef(TagRefData field)
		{
		}

		public void VisitVector2(Vector2Data field)
		{
		}

		public void VisitVector3(Vector3Data field)
		{
		}

		public void VisitVector4(Vector4Data field)
		{
		}

		public void VisitPoint2(Point2Data field)
		{
		}

		public void VisitPoint3(Point3Data field)
		{
		}

		public void VisitPlane2(Plane2Data field)
		{
		}

		public void VisitPlane3(Plane3Data field)
		{
		}

		public void VisitDegree(DegreeData field)
		{
		}

		public void VisitDegree2(Degree2Data field)
		{
		}

		public void VisitDegree3(Degree3Data field)
		{
		}

		public void VisitRect16(RectangleData field)
		{
		}

		public void VisitQuat16(Quaternion16Data field)
		{
		}

		public void VisitPoint16(Point16Data field)
		{
		}

		public void VisitShaderRef(ShaderRef field)
		{
		}

		public void VisitRangeInt16(RangeInt16Data field)
		{
		}

		public void VisitRangeFloat32(RangeFloat32Data field)
		{
		}

		public void VisitRangeDegree(RangeDegreeData field)
		{
		}

		public void VisitDatum(DatumData field)
		{
		}

		public void VisitOldStringID(OldStringIDData field)
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

		public void EnumWrappers(TagBlockData block, Action<WrappedTagBlockEntry> wrapperProcessor)
		{
			FlattenedTagBlock flattened;
			if (!_flattenInfo.TryGetValue(block, out flattened))
				return;

			foreach (WrappedTagBlockEntry wrapper in flattened.Wrappers)
				wrapperProcessor(wrapper);
		}

		public WrappedTagBlockEntry GetTopLevelWrapper(TagBlockData block, WrappedTagBlockEntry wrapper)
		{
			FlattenedTagBlock flattened;
			if (_flattenInfo.TryGetValue(block, out flattened))
				return flattened.GetTopLevelWrapper(wrapper);
			return null;
		}

		/// <summary>
		///     Forcibly expands a block and all of its ancestors.
		/// </summary>
		/// <param name="block">The block to make visible.</param>
		public void ForceVisible(TagBlockData block)
		{
			FlattenedTagBlock flattened;
			if (!_flattenInfo.TryGetValue(block, out flattened))
				return;

			while (flattened != null && flattened.Expand())
				flattened = flattened.Parent;
		}

		private void AttachToTagBlock(TagBlockData field, FlattenedTagBlock flattened)
		{
			field.PropertyChanged += TagBlockPropertyChanged;
			field.Cloned += TagBlockCloned;
			_flattenInfo[field] = flattened;
		}

		private void TagBlockCloned(object sender, TagBlockClonedEventArgs e)
		{
			FlattenedTagBlock flattened = _flattenInfo[e.Old];
			AttachToTagBlock(e.Clone, flattened);
			flattened.SynchronizeWith(e.Clone);
		}

		private void TagBlockPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var block = (TagBlockData) sender;
			FlattenedTagBlock flattenedField = _flattenInfo[block];
			if (e.PropertyName == "IsExpanded")
			{
				if (block.IsExpanded)
					flattenedField.Expand();
				else
					flattenedField.Contract();
			}
			else if (!_loading &&
			         (e.PropertyName == "CurrentIndex" || e.PropertyName == "FirstElementAddress" || e.PropertyName == "ElementSize"))
			{
				_loading = true;
				_tracker.Enabled = false;

				if (e.PropertyName == "FirstElementAddress")
				{
					// Throw out any cached changes and reset the current index
					RecursiveReset(flattenedField.LoadedFields);
					if (block.Length > 0)
						block.CurrentIndex = 0;
					else
						block.CurrentIndex = -1;
				}
				else
				{
					// Cache any changes made to the current page
					RecursiveUnload(flattenedField.LoadedFields);
				}

				// Load the new page in
				flattenedField.LoadPage(block, block.CurrentIndex);

				// Read any non-cached fields in the page
				_reader.ReadTagBlockChildren(block);
				RecursiveLoad(flattenedField.LoadedFields);

				_tracker.Enabled = true;
				_loading = false;
			}
		}

		private void RecursiveUnload(IEnumerable<MetaField> fields)
		{
			foreach (MetaField field in fields)
			{
				var block = field as TagBlockData;
				if (block != null)
				{
					FlattenedTagBlock flattened = _flattenInfo[block];
					RecursiveUnload(flattened.LoadedFields);
					_flattenInfo[block].UnloadPage();
				}
			}
		}

		private void RecursiveReset(IEnumerable<MetaField> fields)
		{
			foreach (MetaField field in fields)
			{
				_tracker.MarkUnchanged(field);

				var block = field as TagBlockData;
				if (block != null)
				{
					FlattenedTagBlock flattened = _flattenInfo[block];
					RecursiveReset(flattened.LoadedFields);
					block.ResetPages();
				}
			}
		}

		private void RecursiveLoad(IEnumerable<MetaField> fields)
		{
			foreach (MetaField field in fields)
			{
				var block = field as TagBlockData;
				if (block != null)
				{
					FlattenedTagBlock flattened = _flattenInfo[block];
					_flattenInfo[block].LoadPage(block, block.CurrentIndex);
					RecursiveLoad(flattened.LoadedFields);
				}
			}
		}

		private void DataRefPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var field = (DataRef)sender;

			if (!_loading && !field.ShowingNotice &&
				(e.PropertyName == "Length" || e.PropertyName == "DataAddress" || e.PropertyName == "ShowingNotice"))
			{
				_loading = true;
				_tracker.Enabled = false;

				_reader.ReadDataRefContents(field);

				_tracker.Enabled = true;
				_loading = false;
			}
		}
	}
}