using System.Collections.Generic;
using Assembly.Helpers;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class MetaFilterer : IMetaFieldVisitor
	{
		public delegate void FieldHighlighter(MetaField field, bool highlight);

		public delegate void ResultCollector(MetaField foundField, MetaField listField, TagBlockData parent);

		private readonly TagBlockFlattener _flattener;
		private readonly FieldHighlighter _highlighter;
		private readonly ResultCollector _resultCollector;

		private TagBlockData _currentTagBlock;
		private string _filter;
		private int _highlightLevel; // If greater than zero, then always highlight fields
		private float? _numberFilter;
		private MetaField _topLevelField;

		public MetaFilterer(TagBlockFlattener flattener, ResultCollector resultCollector, FieldHighlighter highlighter)
		{
			_flattener = flattener;
			_resultCollector = resultCollector;
			_highlighter = highlighter;
		}

		public void VisitFlags(FlagData field)
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
			if (FilterString(field, field.SelectedValue.Name))
				return;
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

		public void VisitUint64(Uint64Data field)
		{
			if (!FilterString(field, field.Name))
				FilterNumber(field, field.Value);
		}

		public void VisitInt64(Int64Data field)
		{
			if (!FilterString(field, field.Name))
				FilterNumber(field, field.Value);
		}

		public void VisitFloat32(Float32Data field)
		{
			if (!FilterString(field, field.Name))
				FilterNumber(field, field.Value);
		}

		public void VisitColourInt(ColorData field)
		{
			FilterString(field, field.Name);
		}

		public void VisitColourFloat(ColorData field)
		{
			FilterString(field, field.Name);
		}

		public void VisitTagBlock(TagBlockData field)
		{
			// Don't enter empty blocks
			TagBlockData oldTagBlock = _currentTagBlock;
			_currentTagBlock = field;

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

			_currentTagBlock = oldTagBlock;
		}

		public void VisitTagBlockEntry(WrappedTagBlockEntry field)
		{
			// Ignore - wrapper handling is done inside VisitTagBlock/HandleWrapper to ensure that
			// closed blocks aren't skipped over
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
			if (!FilterString(field, field.Name) && field.Group != null)
			{
				if (!FilterString(field, field.Group.TagGroupMagic) && field.Value != null)
					FilterString(field, field.Value.TagFileName);
			}
		}

		public void VisitPoint2(Vector2Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					FilterNumber(field, field.B);
				}
			}
		}

		public void VisitPoint3(Vector3Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					if (!FilterNumber(field, field.B))
						FilterNumber(field, field.C);
				}
			}
		}

		public void VisitVector2(Vector2Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					FilterNumber(field, field.B);
				}
			}
		}

		public void VisitVector3(Vector3Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					if (!FilterNumber(field, field.B))
						FilterNumber(field, field.C);
				}
			}
		}

		public void VisitVector4(Vector4Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					if (!FilterNumber(field, field.B))
						if (!FilterNumber(field, field.C))
							FilterNumber(field, field.D);
				}
			}
		}

		public void VisitPoint2(Point2Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					FilterNumber(field, field.B);
				}
			}
		}

		public void VisitPoint3(Point3Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					if (!FilterNumber(field, field.B))
						FilterNumber(field, field.C);
				}
			}
		}



		public void VisitPlane2(Plane2Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					if (!FilterNumber(field, field.B))
						FilterNumber(field, field.C);
				}
			}
		}

		public void VisitPlane3(Plane3Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					if (!FilterNumber(field, field.B))
						if (!FilterNumber(field, field.C))
							FilterNumber(field, field.D);
				}
			}
		}



		public void VisitDegree(DegreeData field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.Value))
				{
					FilterNumber(field, field.Value);
				}
			}
		}

		public void VisitDegree2(Degree2Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					FilterNumber(field, field.B);
				}
			}
		}

		public void VisitDegree3(Degree3Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					if (!FilterNumber(field, field.B))
						FilterNumber(field, field.C);
				}
			}
		}

		public void VisitPlane2(Vector3Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					if (!FilterNumber(field, field.B))
						FilterNumber(field, field.C);
				}
			}
		}

		public void VisitPlane3(Vector4Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					if (!FilterNumber(field, field.B))
						if (!FilterNumber(field, field.C))
							FilterNumber(field, field.D);
				}
			}
		}

		public void VisitRect16(RectangleData field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					if (!FilterNumber(field, field.B))
						if (!FilterNumber(field, field.C))
							FilterNumber(field, field.D);
				}
			}
		}

		public void VisitQuat16(Quaternion16Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					if (!FilterNumber(field, field.B))
						if (!FilterNumber(field, field.C))
							FilterNumber(field, field.D);
				}
			}
		}

		public void VisitPoint16(Point16Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.A))
				{
					FilterNumber(field, field.B);
				}
			}
		}

		public void VisitShaderRef(ShaderRef field)
		{
			if (!FilterString(field, field.Name))
				FilterString(field, field.DatabasePath);
		}

		public void VisitRangeInt16(RangeInt16Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.Min))
				{
					FilterNumber(field, field.Max);
				}
			}
		}

		public void VisitRangeFloat32(RangeFloat32Data field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.Min))
				{
					FilterNumber(field, field.Max);
				}
			}
		}

		public void VisitRangeDegree(RangeDegreeData field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.Min))
				{
					FilterNumber(field, field.Max);
				}
			}
		}

		public void VisitDatum(DatumData field)
		{
			if (!FilterString(field, field.Name))
			{
				if (!FilterNumber(field, field.Salt))
				{
					FilterNumber(field, field.Index);
				}
			}
		}

		public void VisitOldStringID(OldStringIDData field)
		{
			if (!FilterString(field, field.Name))
				FilterString(field, field.Value);
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

		// Passed as the callback to TagBlockFlattener.EnumWrappers in VisitTagBlock
		private void TagBlockFlattener_HandleWrapper(WrappedTagBlockEntry wrapper)
		{
			_topLevelField = _flattener.GetTopLevelWrapper(_currentTagBlock, wrapper);
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
			_resultCollector(field, _topLevelField, _currentTagBlock);
		}

		private void RejectField(MetaField field)
		{
			_highlighter(field, _highlightLevel > 0);
		}
	}
}