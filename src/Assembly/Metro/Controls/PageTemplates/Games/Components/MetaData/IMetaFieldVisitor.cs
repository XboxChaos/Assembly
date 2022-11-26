namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public interface IMetaFieldVisitor
	{
		void VisitFlags(FlagData field);
		void VisitComment(CommentData field);
		void VisitEnum(EnumData field);
		void VisitUint8(Uint8Data field);
		void VisitInt8(Int8Data field);
		void VisitUint16(Uint16Data field);
		void VisitInt16(Int16Data field);
		void VisitUint32(Uint32Data field);
		void VisitInt32(Int32Data field);
		void VisitUint64(Uint64Data field);
		void VisitInt64(Int64Data field);
		void VisitFloat32(Float32Data field);
		void VisitTagBlock(TagBlockData field);
		void VisitTagBlockEntry(WrappedTagBlockEntry field);
		void VisitString(StringData field);
		void VisitStringID(StringIDData field);
		void VisitOldStringID(OldStringIDData field);
		void VisitRawData(RawData field);
		void VisitDataRef(DataRef field);
		void VisitTagRef(TagRefData field);
		void VisitPoint2(Point2Data field);
		void VisitPoint3(Point3Data field);
		void VisitVector2(Vector2Data field);
		void VisitVector3(Vector3Data field);
		void VisitVector4(Vector4Data field);
		void VisitDegree(DegreeData field);
		void VisitDegree2(Degree2Data field);
		void VisitDegree3(Degree3Data field);
		void VisitPlane2(Plane2Data field);
		void VisitPlane3(Plane3Data field);
		void VisitRect16(RectangleData field);
		void VisitQuat16(Quaternion16Data field);
		void VisitPoint16(Point16Data field);
		void VisitColourInt(ColorData field);
		void VisitColourFloat(ColorData field);
		void VisitShaderRef(ShaderRef field);
		void VisitRangeInt16(RangeInt16Data field);
		void VisitRangeFloat32(RangeFloat32Data field);
		void VisitRangeDegree(RangeDegreeData field);
		void VisitDatum(DatumData field);
	}
}