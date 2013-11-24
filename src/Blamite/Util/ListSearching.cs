using System;
using System.Collections.Generic;

namespace Blamite.Util
{
	public static class ListSearching
	{
		// Binary search methods adapted from http://stackoverflow.com/questions/967047/how-to-perform-a-binary-search-on-ilistt

		public static int BinarySearch<T>(IList<T> values, T value)
		{
			return BinarySearch(values, value, DefaultSelector);
		}

		public static int BinarySearch<T>(IList<T> values, T value, IComparer<T> comparer)
		{
			return BinarySearch(values, value, comparer, DefaultSelector);
		}

		public static int BinarySearch<TSource, TKey>(IList<TSource> values, TKey value, Func<TSource, TKey> selector)
		{
			return BinarySearch(values, value, Comparer<TKey>.Default, selector);
		}

		public static int BinarySearch<TSource, TKey>(IList<TSource> values, TKey value, IComparer<TKey> comparer,
			Func<TSource, TKey> selector)
		{
			int lower = 0;
			int upper = values.Count - 1;

			while (lower <= upper)
			{
				int middle = (lower + upper)/2;
				TKey testValue = selector(values[middle]);

				int comparison = comparer.Compare(value, testValue);
				if (comparison == 0)
					return middle;
				if (comparison < 0)
					upper = middle - 1;
				else
					lower = middle + 1;
			}

			return ~lower;
		}

		private static TSource DefaultSelector<TSource>(TSource value)
		{
			return value;
		}
	}
}