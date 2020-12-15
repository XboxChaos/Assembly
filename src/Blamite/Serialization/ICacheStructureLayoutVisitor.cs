using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Serialization
{
	/// <summary>
	///     Extends the interface for a class which acts as a visitor for fields in
	///     a structure which is read from a cache file.
	/// </summary>
	public interface ICacheStructureLayoutVisitor : IStructureLayoutVisitor
    {
		/// <summary>
		///     Called when a StringID layout field is visited.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		void VisitStringIDField(string name, int offset);

		/// <summary>
		///     Called when a tag reference layout field is visited.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		void VisitTagReferenceField(string name, int offset, bool withGroup);

		/// <summary>
		///		Called when a tag block field is visited.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		/// <param name="layout">The layout of the tag block's elements.</param>
		void VisitTagBlockField(string name, int offset, StructureLayout layout);
	}
}
