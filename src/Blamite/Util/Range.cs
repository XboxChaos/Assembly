using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Util
{
    /// <summary>
    /// A range of two comparable values. The starting value is inclusive, and the ending value is exclusive.
    /// </summary>
    public struct Range<T> where T : IComparable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Range&lt;T&gt;"/> struct.
        /// The range will be set to have a given starting value (inclusive) and a given ending value (exclusive).
        /// </summary>
        /// <param name="start">The starting value (inclusive).</param>
        /// <param name="end">The ending value (exclusive).</param>
        public Range(T start, T end)
        {
            if (end.CompareTo(start) < 0)
                throw new ArgumentException("end is less than start");
            Start = start;
            End = end;
        }

        /// <summary>
        /// The starting value.
        /// </summary>
        public readonly T Start;

        /// <summary>
        /// The ending value (exclusive).
        /// </summary>
        public readonly T End;

        /// <summary>
        /// Determines whether or not a value is included in this range.
        /// </summary>
        /// <param name="val">The value to test.</param>
        /// <returns><c>true</c> if the value is included in this range.</returns>
        public bool Includes(T val)
        {
            return val.CompareTo(Start) >= 0 && val.CompareTo(End) < 0;
        }

        /// <summary>
        /// Determines whether or not another range is entirely included in this range.
        /// </summary>
        /// <param name="other">The other range.</param>
        /// <returns><c>true</c> if the other range is entirely contained inside this range.</returns>
        public bool Includes(Range<T> other)
        {
            return other.Start.CompareTo(Start) >= 0 && other.End.CompareTo(End) <= 0;
        }

        /// <summary>
        /// Determines whether or not this range intersects another range.
        /// </summary>
        /// <param name="other">The other range.</param>
        /// <returns><c>true</c> if this range shares at least one value with the other range.</returns>
        public bool Intersects(Range<T> other)
        {
            return other.Start.CompareTo(other.End) != 0 && Start.CompareTo(End) != 0
                && ((other.Start.CompareTo(Start) >= 0 && other.Start.CompareTo(End) < 0)
                || (other.End.CompareTo(Start) > 0 && other.End.CompareTo(End) <= 0)
                || (other.Start.CompareTo(Start) <= 0 && other.End.CompareTo(End) >= 0));
        }

        /// <summary>
        /// Determines whether or not a union can be successfully made between this range and another range.
        /// That is, whether or not this range is adjacent to or intersects the other range.
        /// </summary>
        /// <param name="other">The other range.</param>
        /// <returns><c>true</c> if a union can be successfully made between this range and the other range.</returns>
        public bool CanUnionWith(Range<T> other)
        {
            return other.Start.CompareTo(End) <= 0 && other.End.CompareTo(Start) >= 0;
        }

        /// <summary>
        /// Returns a range with the same ending value as this range but a different starting value.
        /// </summary>
        /// <param name="newStart">The starting value of the new range.</param>
        /// <returns>The new range.</returns>
        public Range<T> ResizeFront(T newStart)
        {
            return new Range<T>(newStart, End);
        }

        /// <summary>
        /// Returns a range with the same starting value as this range but a different ending value.
        /// </summary>
        /// <param name="newEnd">The ending value of the new range.</param>
        /// <returns>The new range.</returns>
        public Range<T> ResizeBack(T newEnd)
        {
            return new Range<T>(Start, newEnd);
        }

        /// <summary>
        /// Returns a range which is the union of this range with another range.
        /// The ranges must intersect.
        /// </summary>
        /// <param name="other">The other range.</param>
        /// <returns>A range which contains all values in both ranges.</returns>
        public Range<T> UnionWith(Range<T> other)
        {
            if (!CanUnionWith(other))
                throw new ArgumentException("The ranges do not intersect and are not adjacent");

            var start = Start;
            var end = End;
            if (other.Start.CompareTo(Start) < 0)
                start = other.Start;
            if (other.End.CompareTo(End) > 0)
                end = other.End;

            return new Range<T>(start, end);
        }
    }
}
