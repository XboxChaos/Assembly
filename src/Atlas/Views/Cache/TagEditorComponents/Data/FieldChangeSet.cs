using System.Collections;
using System.Collections.Generic;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	/// <summary>
	///     A set of fields that have been changed.
	/// </summary>
	/// <seealso cref="FieldChangeTracker" />
	public class FieldChangeSet : IEnumerable<TagDataField>
	{
		private readonly HashSet<TagDataField> _fields = new HashSet<TagDataField>();

		/// <summary>
		///     Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<TagDataField> GetEnumerator()
		{
			return _fields.GetEnumerator();
		}

		/// <summary>
		///     Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _fields.GetEnumerator();
		}

		/// <summary>
		///     Marks a field as changed.
		/// </summary>
		/// <param name="field">The field to mark as changed.</param>
		public void MarkChanged(TagDataField field)
		{
			_fields.Add(field);
		}

		/// <summary>
		///     Marks a field as unchanged.
		/// </summary>
		/// <param name="field">The field to mark as unchanged.</param>
		public void MarkUnchanged(TagDataField field)
		{
			_fields.Remove(field);
		}

		/// <summary>
		///     Returns whether or not a field is marked as changed.
		/// </summary>
		/// <param name="field">The field to check.</param>
		/// <returns>true if the field has been marked as changed.</returns>
		public bool HasChanged(TagDataField field)
		{
			return _fields.Contains(field);
		}

		/// <summary>
		///     Marks all fields as unchanged.
		/// </summary>
		public void MarkAllUnchanged()
		{
			_fields.Clear();
		}
	}
}