using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assembly.Helpers.TagEditor.Buffering;
using Assembly.Helpers.TagEditor.Fields;
using Blamite.IO;
using Blamite.Util;

namespace Assembly.Helpers.TagEditor
{
	public class TagDataWriter : ITagFieldVisitor
	{
		/// <summary>
		/// Occurs when a tag buffer is written to.
		/// </summary>
		public event EventHandler<TagDataUpdatedEventArgs> TagDataUpdated;

		public void WriteField(TagField field)
		{
			field.Accept(this);
		}

		public void WriteFields(IEnumerable<TagField> fields)
		{
			foreach (var field in fields)
				WriteField(field);
		}

		void ITagFieldVisitor.VisitUInt8(UInt8Field field)
		{
			var stream = field.GetStream();
			stream.WriteByte(field.Value);
			OnTagDataUpdated(field, stream, 1);
		}

		void ITagFieldVisitor.VisitUInt16(UInt16Field field)
		{
			var stream = field.GetStream();
			stream.WriteUInt16(field.Value);
			OnTagDataUpdated(field, stream, 2);
		}

		void ITagFieldVisitor.VisitUInt32(UInt32Field field)
		{
			var stream = field.GetStream();
			stream.WriteUInt32(field.Value);
			OnTagDataUpdated(field, stream, 4);
		}

		void ITagFieldVisitor.VisitInt8(Int8Field field)
		{
			var stream = field.GetStream();
			stream.WriteSByte(field.Value);
			OnTagDataUpdated(field, stream, 1);
		}

		void ITagFieldVisitor.VisitInt16(Int16Field field)
		{
			var stream = field.GetStream();
			stream.WriteInt16(field.Value);
			OnTagDataUpdated(field, stream, 2);
		}

		void ITagFieldVisitor.VisitInt32(Int32Field field)
		{
			var stream = field.GetStream();
			stream.WriteInt32(field.Value);
			OnTagDataUpdated(field, stream, 4);
		}

		void ITagFieldVisitor.VisitFloat32(Float32Field field)
		{
			var stream = field.GetStream();
			stream.WriteFloat(field.Value);
			OnTagDataUpdated(field, stream, 4);
		}

		/// <summary>
		/// Raises the <see cref="E:TagDataUpdated" /> event.
		/// </summary>
		/// <param name="e">The <see cref="TagDataUpdatedEventArgs"/> instance containing the event data.</param>
		protected void OnTagDataUpdated(TagDataUpdatedEventArgs e)
		{
			if (TagDataUpdated != null)
				TagDataUpdated(this, e);
		}

		/// <summary>
		/// Raises the <see cref="E:TagDataUpdated" /> event after a field's data has been updated.
		/// The <paramref name="size"/> bytes before the stream position will be reported as the changed area.
		/// </summary>
		/// <param name="field">The field that was updated.</param>
		/// <param name="stream">The stream that was written to.</param>
		/// <param name="size">The size of the data that was written.</param>
		protected void OnTagDataUpdated(ValueTagField field, IStream stream, long size)
		{
			var pos = stream.Position;
			OnTagDataUpdated(new TagDataUpdatedEventArgs(field.Source, field, new Range<long>(pos - size, pos)));
		}
	}

	/// <summary>
	/// Provides additional information for tag data update events.
	/// </summary>
	public class TagDataUpdatedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TagDataUpdatedEventArgs"/> class.
		/// </summary>
		/// <param name="bufferSource">The buffer source that changes occurred in.</param>
		/// <param name="field">The tag field that triggered the update. Can be <c>null</c>.</param>
		/// <param name="changes">The section of the buffer that was changed.</param>
		public TagDataUpdatedEventArgs(TagBufferSource bufferSource, ValueTagField field, Range<long> changes)
		{
			BufferSource = bufferSource;
		    Field = field;
			Changes = changes;
		}

		/// <summary>
		/// Gets the buffer source that the changes occurred in.
		/// </summary>
		public TagBufferSource BufferSource { get; private set; }

        /// <summary>
        /// Gets the tag field that triggered the update. Can be <c>null</c>.
        /// </summary>
        public ValueTagField Field { get; private set; }

		/// <summary>
		/// Gets the section of the buffer that was changed.
		/// </summary>
		public Range<long> Changes { get; private set; } 
	}
}
