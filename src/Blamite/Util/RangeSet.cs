using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Util
{
	/// <summary>
	/// A set of ranges. Adjacent ranges will be combined with each other.
	/// </summary>
	/// <typeparam name="T">The type of value used to set the range bounds.</typeparam>
	public class RangeSet<T> where T : IComparable
	{
		private static readonly RangeStartComparer StartComparer = new RangeStartComparer();
		private readonly List<Range<T>> _ranges = new List<Range<T>>();

		/// <summary>
		/// Gets the ranges contained in the set, in sorted order by starting index.
		/// </summary>
		public IEnumerable<Range<T>> Ranges
		{
			get { return _ranges; }
		}

		/// <summary>
		/// Inserts the specified range into the set.
		/// It will be combined with other ranges as appropriate.
		/// </summary>
		/// <param name="range">The range to insert.</param>
		public void Insert(Range<T> range)
		{
			var index = _ranges.BinarySearch(range, StartComparer);
			if (index < 0)
			{
				// Start index not found
				// Try to merge with the range before it
				index = ~index;
				if (index > 0 && TryMergeAndSimplify(range, index - 1))
					return;
			}

			// Try to merge with the next range >= starting index
			if (index < _ranges.Count && TryMergeAndSimplify(range, index))
				return;

			// Can't merge with anything - insert it
			_ranges.Insert(index, range);
		}

		/// <summary>
		/// Imports all of the ranges from another set into this one.
		/// </summary>
		/// <param name="other">The other range set to import from.</param>
		public void Import(RangeSet<T> other)
		{
			foreach (var range in other.Ranges)
				Insert(range);
		}

		/// <summary>
		/// Determines whether the set contains a range of values.
		/// </summary>
		/// <param name="range">The range of values to check.</param>
		/// <returns><c>true</c> if the range is contained within any of the ranges in the set.</returns>
		public bool ContainsRange(Range<T> range)
		{
			var index = _ranges.BinarySearch(range, StartComparer);
			if (index < 0)
			{
				// Start index not found
				// Flip it to get the index of the next highest range, and check if the one before it includes it
				index = ~index;
				return (index > 0 && _ranges[index - 1].Includes(range));
			}
			return _ranges[index].Includes(range);
		}

		/// <summary>
		/// Removes all ranges from the set.
		/// </summary>
		public void Clear()
		{
			_ranges.Clear();
		}

		/// <summary>
		/// Tries the merge.
		/// </summary>
		/// <param name="first">The first range to merge.</param>
		/// <param name="secondIndex">Index of the second range to merge.</param>
		/// <param name="resultIndex">Index to store the result to.</param>
		/// <param name="deleteSecond">If set to <c>true</c> and a merge occurs, the range at secondIndex will be removed.</param>
		/// <returns><c>true</c> if a merge took place.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">Thrown if either index is invalid.</exception>
		private bool TryMerge(Range<T> first, int secondIndex, int resultIndex, bool deleteSecond)
		{
			if (secondIndex < 0 || secondIndex >= _ranges.Count)
				throw new ArgumentOutOfRangeException("secondIndex");
			if (resultIndex < 0 || resultIndex >= _ranges.Count)
				throw new ArgumentOutOfRangeException("secondIndex");

			var second = _ranges[secondIndex];
			if (first.CanUnionWith(second))
			{
				_ranges[resultIndex] = first.UnionWith(second);
				if (deleteSecond)
					_ranges.RemoveAt(secondIndex);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Starting at an index, tries to merge the range at that index with as many subsequent ranges as possible.
		/// </summary>
		/// <param name="index">The index to start at.</param>
		/// <exception cref="System.ArgumentOutOfRangeException">Thrown if the index is invalid.</exception>
		private void Simplify(int index)
		{
			if (index < 0 || index >= _ranges.Count)
				throw new ArgumentOutOfRangeException("index");

			// Keep trying to merge, deleting the second range each time
			var range = _ranges[index];
			while (index < _ranges.Count - 1 && TryMerge(range, index + 1, index, true))
				range = _ranges[index];
		}

		/// <summary>
		/// Attempts to merge a range with the range at an index.
		/// If the merge is successful, a simplify operation will be run starting from the index.
		/// </summary>
		/// <param name="range">The range to merge.</param>
		/// <param name="index">The index to merge at.</param>
		/// <returns><c>true</c> if at least one merge took place.</returns>
		private bool TryMergeAndSimplify(Range<T> range, int index)
		{
			if (TryMerge(range, index, index, false))
			{
				Simplify(index);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Comparison function for ranges which compares their start indexes.
		/// </summary>
		private class RangeStartComparer : IComparer<Range<T>>
		{
			public int Compare(Range<T> x, Range<T> y)
			{
				return x.Start.CompareTo(y.Start);
			}
		}
	}
}
