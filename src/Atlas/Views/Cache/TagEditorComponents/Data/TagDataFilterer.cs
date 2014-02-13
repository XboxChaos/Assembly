using System.Collections.Generic;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	public class TagDataFilterer : ITagDataFieldVisitor
	{
		public delegate void FieldHighlighter(TagDataField field, bool highlight);

		public delegate void ResultCollector(TagDataField foundField, TagDataField listField, TagBlockData parent);

		private readonly TagBlockFlattener _flattener;
		private readonly FieldHighlighter _highlighter;
		private readonly ResultCollector _resultCollector;

		private TagBlockData _currentTagBlocks;
		private string _filter;
		private int _highlightLevel; // If greater than zero, then always highlight fields
		private float? _numberFilter;
		private TagDataField _topLevelField;

		public TagDataFilterer(TagBlockFlattener flattener, ResultCollector resultCollector, FieldHighlighter highlighter)
		{
			_flattener = flattener;
			_resultCollector = resultCollector;
			_highlighter = highlighter;
		}

		public void VisitBitfield(BitfieldData field)
		{
			FilterString(field, field.Name);
		}

		public void VisitComment(CommentData field)
		{
			if (!FilterString(field, field.Name))
				FilterString(field, field.Text);
		}

		public void VisitEnum(EnumData field)
		{
			FilterString(field, field.Name);
		}

		public void VisitUint8(Uint8Data field)
		{
			if (!FilterString(field, field.Name))
				FilterNumber(field, field.Value);
		}

		public void VisitInt8(Int8Data field)
		{
			if (!FilterString(field, field.Name))
				FilterNumber(field, field.Value);
		}

		public void VisitUint16(Uint16Data field)
		{
			if (!FilterString(field, field.Name))
				FilterNumber(field, field.Value);
		}

		public void VisitInt16(Int16Data field)
		{
			if (!FilterString(field, field.Name))
				FilterNumber(field, field.Value);
		}

		public void VisitUint32(Uint32Data field)
		{
			if (!FilterString(field, field.Name))
				FilterNumber(field, field.Value);
		}

		public void VisitInt32(Int32Data field)
		{
			if (!FilterString(field, field.Name))
				FilterNumber(field, field.Value);
		}

		public void VisitFloat32(Float32Data field)
		{
			if (!FilterString(field, field.Name))
				FilterNumber(field, field.Value);
		}

		public void VisitColourInt(ColourData field)
		{
			if (!FilterString(field, field.Name))
				FilterString(field, field.Value);
		}

		public void VisitColourFloat(ColourData field)
		{
			if (!FilterString(field, field.Name))
				FilterString(field, field.Value);
		}

		public void VisitReflexive(TagBlockData field)
		{
			// Don't enter empty reflexives
			var oldTagBlock = _currentTagBlocks;
			_currentTagBlocks = field;

			if (FilterString(field, field.Name) && field.Length > 0)
			{
				// Forcibly highlight everything inside it
				_highlightLevel++;
				_flattener.EnumWrappers(field, TagBlockFlattener_HandleWrapper);
				_highlightLevel--;
			}
			else if (field.Length > 0)
			{
				_flattener.EnumWrappers(field, TagBlockFlattener_HandleWrapper);
			}

			_currentTagBlocks = oldTagBlock;
		}

		public void VisitReflexiveEntry(WrappedTagBlockEntry field)
		{
			// Ignore - wrapper handling is done inside VisitReflexive/HandleWrapper to ensure that
			// closed reflexives aren't skipped over
		}

		public void VisitString(StringData field)
		{
			if (!FilterString(field, field.Name))
				FilterString(field, field.Value);
		}

		public void VisitStringID(StringIDData field)
		{
			// TODO: Filter StringIDs by value
			FilterString(field, field.Name);
		}

		public void VisitRawData(RawData field)
		{
			// AvalonEdit doesn't let us access the text from a different thread
			/*if (!FilterString(field, field.Name))
				FilterString(field, field.Value);*/
			FilterString(field, field.Name);
		}

		public void VisitDataRef(DataRef field)
		{
			// AvalonEdit doesn't let us access the text from a different thread
			/*if (!FilterString(field, field.Name))
				FilterString(field, field.Value);*/
			FilterString(field, field.Name);
		}

		public void VisitTagRef(TagRefData field)
		{
			if (!FilterString(field, field.Name) && field.Class != null)
			{
				if (!FilterString(field, field.Class.Name) && field.Value != null)
					FilterString(field, field.Value.Name);
			}
		}

		public void VisitVector(VectorData field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.X))
				{
					if (!FilterNumber(field, field.Y))
						FilterNumber(field, field.Z);
				}
			}
		}

		public void VisitDegree(DegreeData field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.Degree))
				{
					FilterNumber(field, field.Degree);
				}
			}
		}

		public void VisitShaderRef(ShaderRef field)
		{
			if (!FilterString(field, field.Name))
				FilterString(field, field.DatabasePath);
		}

		public void FilterFields(IEnumerable<TagDataField> fields, string filter)
		{
			_filter = filter.ToLower();

			float numberValue;
			if (float.TryParse(filter, out numberValue))
				_numberFilter = numberValue;

			foreach (TagDataField field in fields)
			{
				_topLevelField = field;
				field.Accept(this);
			}
		}

		// Passed as the callback to TagBlockFlattener.EnumWrappers in VisitReflexive
		private void TagBlockFlattener_HandleWrapper(WrappedTagBlockEntry wrapper)
		{
			_topLevelField = _flattener.GetTopLevelWrapper(_currentTagBlocks, wrapper);
			_highlighter(wrapper, _highlightLevel > 0);
			wrapper.WrappedField.Accept(this);
		}

		private bool FilterString(TagDataField field, string fieldName)
		{
			if (fieldName.ToLower().Contains(_filter))
			{
				AcceptField(field);
				return true;
			}
			RejectField(field);
			return false;
		}

		private bool FilterNumber(TagDataField field, float value)
		{
			if (_numberFilter.HasValue && value == _numberFilter.Value)
			{
				AcceptField(field);
				return true;
			}
			RejectField(field);
			return false;
		}

		private void AcceptField(TagDataField field)
		{
			_highlighter(field, true);
			_resultCollector(field, _topLevelField, _currentTagBlocks);
		}

		private void RejectField(TagDataField field)
		{
			_highlighter(field, _highlightLevel > 0);
		}
	}
}