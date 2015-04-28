using System;

namespace Blamite.IO
{
	/// <summary>
	///     A dynamic pointer to an offset within a segment.
	/// </summary>
	public class SegmentOffset : SegmentPointer
	{
        private readonly int _offset;

        public SegmentOffset(int offset) : base(null,null,0)
		{
            _offset = offset;
		}

		/// <summary>
		///     Gets the file offset corresponding to the SegmentPointer.
		/// </summary>
		/// <returns>The file offset corresponding to the SegmentPointer.</returns>
		public override int AsOffset()
		{
            return _offset;
		}
	}
}