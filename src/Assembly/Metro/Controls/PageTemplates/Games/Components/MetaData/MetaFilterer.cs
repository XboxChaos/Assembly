using System.Collections.Generic;
using Assembly.Helpers;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class MetaFilterer : IMetaFieldVisitor
	{
		public delegate void FieldHighlighter(MetaField field, bool highlight);

		public delegate void ResultCollector(MetaField foundField, MetaField listField, ReflexiveData parent);

		private readonly ReflexiveFlattener _flattener;
		private readonly FieldHighlighter _highlighter;
		private readonly ResultCollector _resultCollector;

		private ReflexiveData _currentReflexive;
		private string _filter;
		private int _highlightLevel; // If greater than zero, then always highlight fields
		private float? _numberFilter;
		private MetaField _topLevelField;

		public MetaFilterer(ReflexiveFlattener flattener, ResultCollector resultCollector, FieldHighlighter highlighter)
		{
			_flattener = flattener;
			_resultCollector = resultCollector;
			_highlighter = highlighter;
		}

		public void VisitBitfield(BitfieldData field)
		{
			FilterString(field, field.Name);
			foreach (var bit in field.Bits)
			{
				if (FilterString(field, bit.Name))
					return;
			}
		}

		public void VisitComment(CommentData field)
		{
			if (!FilterString(field, field.Name))
				FilterString(field, field.Text);
		}

		public void VisitEnum(EnumData field)
		{
			if (FilterString(field, field.Name))
				return;
			foreach (var val in field.Values)
			{
				if (FilterString(field, val.Name))
					return;
			}
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

		public void VisitReflexive(ReflexiveData field)
		{
			// Don't enter empty reflexives
			ReflexiveData oldReflexive = _currentReflexive;
			_currentReflexive = field;

			if (FilterString(field, field.Name) && field.Length > 0)
			{
				// Forcibly highlight everything inside it
				_highlightLevel++;
				_flattener.EnumWrappers(field, ReflexiveFlattener_HandleWrapper);
				_highlightLevel--;
			}
			else if (field.Length > 0)
			{
				_flattener.EnumWrappers(field, ReflexiveFlattener_HandleWrapper);
			}

			_currentReflexive = oldReflexive;
		}

		public void VisitReflexiveEntry(WrappedReflexiveEntry field)
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
			if (!FilterString(field, field.Name))
				FilterString(field, field.Value);
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
				if (!FilterString(field, field.Class.TagClassMagic) && field.Value != null)
					FilterString(field, field.Value.TagFileName);
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

		public void FilterFields(IEnumerable<MetaField> fields, string filter)
		{
			_filter = filter.ToLower();

			float numberValue;
			if (float.TryParse(filter, out numberValue))
				_numberFilter = numberValue;

			foreach (MetaField field in fields)
			{
				_topLevelField = field;
				field.Accept(this);
			}
		}

		// Passed as the callback to ReflexiveFlattener.EnumWrappers in VisitReflexive
		private void ReflexiveFlattener_HandleWrapper(WrappedReflexiveEntry wrapper)
		{
			_topLevelField = _flattener.GetTopLevelWrapper(_currentReflexive, wrapper);
			_highlighter(wrapper, _highlightLevel > 0);
			wrapper.WrappedField.Accept(this);
		}

		private bool FilterString(MetaField field, string fieldName)
		{
			if (fieldName != null && fieldName.ToLower().Contains(_filter))
			{
				AcceptField(field);
				return true;
			}
			RejectField(field);
			return false;
		}

		private bool FilterNumber(MetaField field, float value)
		{
			if (_numberFilter.HasValue && value == _numberFilter.Value)
			{
				AcceptField(field);
				return true;
			}
			RejectField(field);
			return false;
		}

		private void AcceptField(MetaField field)
		{
			_highlighter(field, true);
			_resultCollector(field, _topLevelField, _currentReflexive);
		}

		private void RejectField(MetaField field)
		{
			_highlighter(field, _highlightLevel > 0);
		}
	}
}