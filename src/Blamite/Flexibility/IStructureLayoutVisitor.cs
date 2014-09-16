namespace Blamite.Flexibility
{
	/// <summary>
	///     Defines the interface for a class which acts as a visitor for fields in
	///     a structure.
	/// </summary>
	public interface IStructureLayoutVisitor
	{
		/// <summary>
		///     Called when a basic layout field is visited.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="type">The type of the field's value.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		void VisitBasicField(string name, StructureValueType type, int offset);

		/// <summary>
		///     Called when an array layout field is visited.
		/// </summary>
		/// <param name="name">The name of the array field.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		/// <param name="count">The number of elements in the array.</param>
		/// <param name="entryLayout">The layout of each element in the array.</param>
		void VisitArrayField(string name, int offset, int count, StructureLayout entryLayout);

		/// <summary>
		///     Called when a raw byte array layout field is visited.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		/// <param name="size">The size of the raw data to read.</param>
		void VisitRawField(string name, int offset, int size);

		/// <summary>
		///		Called when a structure field is visited.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		/// <param name="layout">The layout of the data in the structure.</param>
		void VisitStructField(string name, int offset, StructureLayout layout);
	}
}