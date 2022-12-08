using System;
using System.Collections.Generic;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	/// <summary>
	///     Calculates meta control widths.
	/// </summary>
	public class WidthCalculator : IMetaFieldVisitor
	{
		private const double TagBlockSubEntryLeftPadding = 10;
		private const double TagBlockSubEntryRightPadding = 10;
		private const double TagBlockSubEntryBorderWidth = 1;

		private const double TagBlockSubEntryExtraWidth =
			TagBlockSubEntryLeftPadding + TagBlockSubEntryRightPadding + TagBlockSubEntryBorderWidth * 2;

		private static readonly AsciiValue _asciiControl = new AsciiValue();
		private static readonly FlagsValue _flagsControl = new FlagsValue();
		private static readonly Comment _commentControl = new Comment();
		private static readonly Enumeration _enumControl = new Enumeration();
		private static readonly intValue _intControl = new intValue();
		private static readonly StringIDValue _stringIDControl = new StringIDValue();
		private static readonly RawBlock _rawBlock = new RawBlock();
		private static readonly TagValue _tagControl = new TagValue();
		private static readonly ColorValue _colourValue = new ColorValue();
		private static readonly Shader _shader = new Shader();
		private static readonly Multi2Value _multi2Value = new Multi2Value();
		private static readonly Multi3Value _multi3Value = new Multi3Value();
		private static readonly Multi4Value _multi4Value = new Multi4Value();
		private static readonly Plane2Value _plane2Value = new Plane2Value();
		private static readonly Plane3Value _plane3Value = new Plane3Value();
		private static readonly RangeValue _rangeValue = new RangeValue();
		private static readonly DatumValue _datumValue = new DatumValue();
		private double _totalWidth;

		public double TotalWidth
		{
			get { return _totalWidth; }
		}

		public void VisitFlags(FlagData field)
		{
			AddWidth(_flagsControl.Width);
		}

		public void VisitComment(CommentData field)
		{
			AddWidth(_commentControl.Width);
		}

		public void VisitEnum(EnumData field)
		{
			AddWidth(_enumControl.Width);
		}

		public void VisitUint8(Uint8Data field)
		{
			AddWidth(_intControl.Width);
		}

		public void VisitInt8(Int8Data field)
		{
			AddWidth(_intControl.Width);
		}

		public void VisitUint16(Uint16Data field)
		{
			AddWidth(_intControl.Width);
		}

		public void VisitInt16(Int16Data field)
		{
			AddWidth(_intControl.Width);
		}

		public void VisitUint32(Uint32Data field)
		{
			AddWidth(_intControl.Width);
		}

		public void VisitInt32(Int32Data field)
		{
			AddWidth(_intControl.Width);
		}

		public void VisitUint64(Uint64Data field)
		{
			AddWidth(_intControl.Width);
		}

		public void VisitInt64(Int64Data field)
		{
			AddWidth(_intControl.Width);
		}

		public void VisitFloat32(Float32Data field)
		{
			AddWidth(_intControl.Width);
		}

		public void VisitTagBlock(TagBlockData field)
		{
			AddWidth(field.Width);
		}

		public void VisitTagBlockEntry(WrappedTagBlockEntry field)
		{
			// Save our state and recurse into it
			double oldTotal = _totalWidth;
			_totalWidth = 0;
			Add(field.WrappedField);

			double entryWidth = _totalWidth;
			_totalWidth = oldTotal;

			AddWidth(entryWidth + TagBlockSubEntryExtraWidth);
		}

		public void VisitString(StringData field)
		{
			AddWidth(_asciiControl.Width);
		}

		public void VisitStringID(StringIDData field)
		{
			AddWidth(_stringIDControl.Width);
		}

		public void VisitRawData(RawData field)
		{
			AddWidth(_rawBlock.Width);
		}

		public void VisitDataRef(DataRef field)
		{
			AddWidth(_rawBlock.Width);
		}

		public void VisitTagRef(TagRefData field)
		{
			AddWidth(_tagControl.Width);
		}

		public void VisitPoint2(Point2Data field)
		{
			AddWidth(_multi2Value.Width);
		}

		public void VisitPoint3(Point3Data field)
		{
			AddWidth(_multi3Value.Width);
		}

		public void VisitVector2(Vector2Data field)
		{
			AddWidth(_multi2Value.Width);
		}

		public void VisitVector3(Vector3Data field)
		{
			AddWidth(_multi3Value.Width);
		}

		public void VisitVector4(Vector4Data field)
		{
			AddWidth(_multi4Value.Width);
		}

		public void VisitDegree(DegreeData field)
		{
			AddWidth(_intControl.Width);
		}

		public void VisitDegree2(Degree2Data field)
		{
			AddWidth(_multi2Value.Width);
		}

		public void VisitDegree3(Degree3Data field)
		{
			AddWidth(_multi3Value.Width);
		}

		public void VisitPlane2(Plane2Data field)
		{
			AddWidth(_plane2Value.Width);
		}

		public void VisitPlane3(Plane3Data field)
		{
			AddWidth(_plane3Value.Width);
		}

		public void VisitRect16(RectangleData field)
		{
			AddWidth(_multi4Value.Width);
		}

		public void VisitQuat16(Quaternion16Data field)
		{
			AddWidth(_multi4Value.Width);
		}

		public void VisitPoint16(Point16Data field)
		{
			AddWidth(_multi2Value.Width);
		}

		public void VisitColourInt(ColorData field)
		{
			AddWidth(_colourValue.Width);
		}

		public void VisitColourFloat(ColorData field)
		{
			AddWidth(_colourValue.Width);
		}

		public void VisitShaderRef(ShaderRef field)
		{
			AddWidth(_shader.Width);
		}

		public void VisitRangeInt16(RangeInt16Data field)
		{
			AddWidth(_rangeValue.Width);
		}

		public void VisitRangeFloat32(RangeFloat32Data field)
		{
			AddWidth(_rangeValue.Width);
		}

		public void VisitRangeDegree(RangeDegreeData field)
		{
			AddWidth(_rangeValue.Width);
		}

		public void VisitDatum(DatumData field)
		{
			AddWidth(_datumValue.Width);
		}

		public void VisitOldStringID(OldStringIDData field)
		{
			AddWidth(_stringIDControl.Width);
		}

		public void Add(MetaField field)
		{
			field.Accept(this);
		}

		public void Add(IEnumerable<MetaField> fields)
		{
			foreach (MetaField field in fields)
				Add(field);
		}


		private void AddWidth(double width)
		{
			_totalWidth = Math.Max(_totalWidth, width);
		}
	}
}