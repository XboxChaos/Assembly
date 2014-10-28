using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.TagEditor.Fields
{
	public interface ITagFieldVisitor
	{
		void VisitUInt8(UInt8Field field);

		void VisitUInt16(UInt16Field field);

		void VisitUInt32(UInt32Field field);

		void VisitInt8(Int8Field field);

		void VisitInt16(Int16Field field);

		void VisitInt32(Int32Field field);

		void VisitFloat32(Float32Field field);
	}
}
