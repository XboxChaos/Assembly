using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.TagEditor
{
    /// <summary>
    /// Updates tag buffers when fields bound to them are edited by the user.
    /// </summary>
    public class TagDataUpdater
    {
        private readonly TagDataWriter _writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TagDataUpdater"/> class.
        /// The given <see cref="TagFieldMonitor"/> will be used to monitor fields for changes.
        /// </summary>
        /// <param name="writer">The writer to use to update tag buffers.</param>
        public TagDataUpdater(TagDataWriter writer)
        {
            _writer = writer;
        }

		/// <summary>
		/// Attaches to a tag data monitor so that field changes will be written back to the field's tag buffer.
		/// </summary>
		/// <param name="monitor">The monitor to attach to.</param>
	    public void AttachTo(TagFieldMonitor monitor)
	    {
		    monitor.TagFieldChanged += MonitorOnTagFieldChanged;
	    }

        private void MonitorOnTagFieldChanged(object sender, TagFieldChangedEventArgs e)
        {
            _writer.WriteField(e.Field);
        }
    }
}
