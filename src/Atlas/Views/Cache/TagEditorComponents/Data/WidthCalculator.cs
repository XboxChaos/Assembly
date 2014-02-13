using System;
using System.Collections.Generic;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	/// <summary>
	///     Calculates meta control widths.
	/// </summary>
	public class WidthCalculator : ITagDataFieldVisitor
	{
		private const double TagBlockSubEntryLeftPadding = 20;
		private const double TagBlockSubEntryRightPadding = 20;
		private const double TagBlockSubEntryBorderWidth = 1;

		private const double TagBlockSubEntryExtraWidth =
			TagBlockSubEntryLeftPadding + TagBlockSubEntryRightPadding + TagBlockSubEntryBorderWidth*2;

		private static readonly AsciiValue _asciiControl = new AsciiValue();
		private static readonly Bitfield _bitfieldControl = new Bitfield();
		private static readonly Comment _commentControl = new Comment();
		private static readonly Enumeration _enumControl = new Enumeration();
		private static readonly IntValue _intControl = new IntValue();
		private static readonly StringIDValue _stringIDControl = new StringIDValue();
		private static readonly rawBlock _rawBlock = new rawBlock();
		//private static MetaChunk _chunkControl = new MetaChunk();
		private static readonly TagValue _tagControl = new TagValue();
		private static readonly VectorValue _vectorControl = new VectorValue();
		private static readonly DegreeValue _degreeControl = new DegreeValue();
		private static readonly ColourValue _colourValue = new ColourValue();
		private static readonly Shader _shader = new Shader();
		private double _totalWidth;

		public double TotalWidth
		{
			get { return _totalWidth; }
		}

		public void VisitBitfield(BitfieldData field)
		{
			AddWidth(_bitfieldControl.Width);
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

		public void VisitFloat32(Float32Data field)
		{
			AddWidth(_intControl.Width);
		}

		public void VisitReflexive(TagBlockData field)
		{
			AddWidth(field.Width);
		}

		public void VisitReflexiveEntry(WrappedTagBlockEntry field)
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

		public void VisitVector(VectorData field)
		{
			AddWidth(_vectorControl.Width);
		}

		public void VisitDegree(DegreeData field)
		{
			AddWidth(_degreeControl.Width);
		}

		public void VisitColourInt(ColourData field)
		{
			AddWidth(_colourValue.Width);
		}

		public void VisitColourFloat(ColourData field)
		{
			AddWidth(_colourValue.Width);
		}

		public void VisitShaderRef(ShaderRef field)
		{
			AddWidth(_shader.Width);
		}

		public void Add(TagDataField field)
		{
			field.Accept(this);
		}

		public void Add(IEnumerable<TagDataField> fields)
		{
			foreach (TagDataField field in fields)
				Add(field);
		}


		private void AddWidth(double width)
		{
			_totalWidth = Math.Max(_totalWidth, width);
		}
	}
}