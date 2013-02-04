namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public interface IMetaFieldVisitor
    {
        void VisitBitfield(BitfieldData field);
        void VisitComment(CommentData field);
        void VisitEnum(EnumData field);
        void VisitUint8(Uint8Data field);
        void VisitInt8(Int8Data field);
        void VisitUint16(Uint16Data field);
        void VisitInt16(Int16Data field);
        void VisitUint32(Uint32Data field);
        void VisitInt32(Int32Data field);
        void VisitFloat32(Float32Data field);
        void VisitReflexive(ReflexiveData field);
        void VisitReflexiveEntry(WrappedReflexiveEntry field);
        void VisitString(StringData field);
        void VisitStringID(StringIDData field);
        void VisitRawData(RawData field);
        void VisitDataRef(DataRef field);
        void VisitTagRef(TagRefData field);
		void VisitVector(VectorData field);
		void VisitDegree(DegreeData field);
    }
}
