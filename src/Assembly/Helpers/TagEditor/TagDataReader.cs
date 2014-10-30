using Assembly.Helpers.TagEditor.Fields;
using Blamite.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.TagEditor
{
	/// <summary>
	/// Handles deserializing tag data and updating field values.
	/// </summary>
	public class TagDataReader : ITagFieldVisitor
	{
		public void ReadField(TagField field)
		{
			field.Accept(this);
		}

		public void ReadFields(IEnumerable<TagField> fields)
		{
			foreach (var field in fields)
				ReadField(field);
		}

		void ITagFieldVisitor.VisitUInt8(UInt8Field field)
		{
			var stream = field.GetStream();
			field.Value = stream.ReadByte();
		}

		void ITagFieldVisitor.VisitUInt16(UInt16Field field)
		{
			var stream = field.GetStream();
			field.Value = stream.ReadUInt16();
		}

		void ITagFieldVisitor.VisitUInt32(UInt32Field field)
		{
			var stream = field.GetStream();
			field.Value = stream.ReadUInt32();
		}

		void ITagFieldVisitor.VisitInt8(Int8Field field)
		{
			var stream = field.GetStream();
			field.Value = stream.ReadSByte();
		}

		void ITagFieldVisitor.VisitInt16(Int16Field field)
		{
			var stream = field.GetStream();
			field.Value = stream.ReadInt16();
		}

		void ITagFieldVisitor.VisitInt32(Int32Field field)
		{
			var stream = field.GetStream();
			field.Value = stream.ReadInt32();
		}

		void ITagFieldVisitor.VisitFloat32(Float32Field field)
		{
			var stream = field.GetStream();
			field.Value = stream.ReadFloat();
		}
	}
}
