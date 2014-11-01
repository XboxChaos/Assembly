using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assembly.Helpers.TagEditor.Buffering;
using Assembly.Helpers.TagEditor.Fields;
using Blamite.Util;

namespace Assembly.Helpers.TagEditor
{
    /// <summary>
    /// Synchronizes tag fields with overlapping data so that they automatically update when overlapping data is changed.
    /// </summary>
    public class TagFieldSynchronizer : ITagFieldVisitor
    {
        private readonly TagDataReader _reader;
        private readonly TagFieldMonitor _monitor;
        private readonly Dictionary<TagBufferSource, List<SynchronizedField>> _fieldsBySource = new Dictionary<TagBufferSource, List<SynchronizedField>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="TagFieldSynchronizer"/> class.
		/// </summary>
		/// <param name="reader">The reader to use to read tag data.</param>
		/// <param name="monitor">The monitor to disable when tag data is being synchronized. Can be <c>null</c>.</param>
        public TagFieldSynchronizer(TagDataReader reader, TagFieldMonitor monitor)
        {
            _reader = reader;
            _monitor = monitor;
        }

		/// <summary>
		/// Monitors a writer for changes made to tag data.
		/// </summary>
		/// <param name="writer">The writer to monitor.</param>
	    public void AttachTo(TagDataWriter writer)
	    {
			writer.TagDataUpdated += WriterOnTagDataUpdated;
	    }

		/// <summary>
		/// Registers a tag field with the synchronizer, ensuring that it will be synchronized whenever the value of an overlapping field changes.
		/// </summary>
		/// <param name="field">The field.</param>
        public void RegisterField(TagField field)
        {
            field.Accept(this);
        }

		/// <summary>
		/// Registers a group of tag fields with the synchronizer, ensuring that all of them will be synchronized whenever the value of an overlapping field changes.
		/// </summary>
		/// <param name="fields">The fields.</param>
        public void RegisterFields(IEnumerable<TagField> fields)
        {
            foreach (var field in fields)
                RegisterField(field);
        }

        void ITagFieldVisitor.VisitUInt8(UInt8Field field)
        {
            RegisterField(field, 1);
        }

        void ITagFieldVisitor.VisitUInt16(UInt16Field field)
        {
            RegisterField(field, 2);
        }

        void ITagFieldVisitor.VisitUInt32(UInt32Field field)
        {
            RegisterField(field, 4);
        }

        void ITagFieldVisitor.VisitInt8(Int8Field field)
        {
            RegisterField(field, 1);
        }

        void ITagFieldVisitor.VisitInt16(Int16Field field)
        {
            RegisterField(field, 2);
        }

        void ITagFieldVisitor.VisitInt32(Int32Field field)
        {
            RegisterField(field, 4);
        }

        void ITagFieldVisitor.VisitFloat32(Float32Field field)
        {
            RegisterField(field, 4);
        }

		/// <summary>
		/// Registers a field to be synchronized.
		/// </summary>
		/// <param name="field">The field.</param>
		/// <param name="size">The size of the field's data in bytes.</param>
        private void RegisterField(ValueTagField field, long size)
        {
            if (field.Source == null)
                return;
            var fieldList = GetOrCreateFieldList(field.Source);
            var range = new Range<long>(field.Offset, field.Offset + size);
            var syncField = new SynchronizedField(field, range);
            fieldList.Add(syncField);
        }

        private void WriterOnTagDataUpdated(object sender, TagDataUpdatedEventArgs e)
        {
            var fieldList = GetFieldList(e.BufferSource);
            if (fieldList == null)
                return;

            // Disable monitoring so that we don't get stuck in an endless loop
	        var oldMonitorEnabled = true;
	        if (_monitor != null)
	        {
		        oldMonitorEnabled = _monitor.Enabled;
		        _monitor.Enabled = false;
	        }

	        // TODO: This always runs in O(n) time. Determine if this needs to be optimized or not.
	        foreach (var field in fieldList)
	        {
		        if (field.Field != e.Field && field.Location.Intersects(e.Changes))
					UpdateField(field.Field);
	        }
            
            // Re-enable monitoring
			if (_monitor != null)
				_monitor.Enabled = oldMonitorEnabled;
        }

		/// <summary>
		/// Gets the list of synchronized fields for a source.
		/// </summary>
		/// <param name="source">The source to get the field list for.</param>
		/// <returns>The field list for the source, or <c>null</c> if none</returns>
		/// <exception cref="System.ArgumentNullException">Thrown if source is null</exception>
        private List<SynchronizedField> GetFieldList(TagBufferSource source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            List<SynchronizedField> fieldList;
            _fieldsBySource.TryGetValue(source, out fieldList);
            return fieldList;
        }

		/// <summary>
		/// Gets the list of synchronized fields for a source, creating one if it doesn't exist.
		/// </summary>
		/// <param name="source">The source to get the field list for.</param>
		/// <returns>The field list for the source.</returns>
        private List<SynchronizedField> GetOrCreateFieldList(TagBufferSource source)
        {
	        var result = GetFieldList(source);
            if (result != null)
                return result;
            result = new List<SynchronizedField>();
            _fieldsBySource[source] = result;
            return result;
        }

		/// <summary>
		/// Updates a field's value.
		/// </summary>
		/// <param name="field">The field.</param>
        private void UpdateField(TagField field)
        {
            _reader.ReadField(field);
        }

		/// <summary>
		/// Contains information about a synchronized field.
		/// </summary>
        private class SynchronizedField : IComparable<SynchronizedField>
        {
			/// <summary>
			/// Initializes a new instance of the <see cref="SynchronizedField"/> class.
			/// </summary>
			/// <param name="field">The field.</param>
			/// <param name="location">The location of the field's data.</param>
            public SynchronizedField(ValueTagField field, Range<long> location)
            {
                Field = field;
                Location = location;
            }

			/// <summary>
			/// Gets the field to be synchronized.
			/// </summary>
            public ValueTagField Field { get; private set; }

			/// <summary>
			/// Gets the location of the field's data.
			/// </summary>
            public Range<long> Location { get; private set; }

            public int CompareTo(SynchronizedField other)
            {
                return Location.Start.CompareTo(other.Location.Start);
            }
        }
    }
}
