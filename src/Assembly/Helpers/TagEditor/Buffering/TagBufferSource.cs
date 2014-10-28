using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.TagEditor.Buffering
{
	/// <summary>
	/// Base for a class which maintains tag buffers and sub-sources.
	/// </summary>
	public abstract class TagBufferSource
	{
		/// <summary>
		/// Gets the currently-active tag buffer.
		/// </summary>
		/// <returns>The currently-active tag buffer, or <c>null</c> if none.</returns>
		public abstract TagBuffer GetActiveBuffer();

		/// <summary>
		/// Occurs when the active tag buffer changes.
		/// </summary>
		public event EventHandler<ActiveBufferChangedEventArgs> ActiveBufferChanged;

		protected void OnActiveBufferChanged()
		{
			OnActiveBufferChanged(new ActiveBufferChangedEventArgs(this));
		}

		protected void OnActiveBufferChanged(ActiveBufferChangedEventArgs args)
		{
			if (ActiveBufferChanged != null)
				ActiveBufferChanged(this, args);
		}
	}

	/// <summary>
	/// Provides additional information about active tag buffer change events.
	/// </summary>
	public class ActiveBufferChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ActiveBufferChangedEventArgs"/> class.
		/// </summary>
		/// <param name="source">The tag buffer source that triggered the event.</param>
		public ActiveBufferChangedEventArgs(TagBufferSource source)
		{
			Source = source;
		}

		/// <summary>
		/// Gets the tag buffer source that triggered the event.
		/// </summary>
		public TagBufferSource Source { get; private set; }
	}
}
