using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.TagEditor
{
	/// <summary>
	/// A <see cref="TagBufferSource"/> which always has the same buffer active.
	/// </summary>
	public class SingleTagBufferSource : TagBufferSource
	{
		private TagBuffer _buffer;

		/// <summary>
		/// Initializes a new instance of the <see cref="SingleTagBufferSource"/> class.
		/// </summary>
		/// <param name="buffer">The buffer to use as the active buffer.</param>
		public SingleTagBufferSource(TagBuffer buffer)
		{
			_buffer = buffer;
		}

		/// <summary>
		/// Gets the currently-active tag buffer.
		/// </summary>
		/// <returns>
		/// The currently-active tag buffer, or <c>null</c> if none.
		/// </returns>
		public override TagBuffer GetActiveBuffer()
		{
			return _buffer;
		}
	}
}
