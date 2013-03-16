using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Util
{
    public static class ListSearching
    {
        // Binary search methods adapted from http://stackoverflow.com/questions/967047/how-to-perform-a-binary-search-on-ilistt

        public static int BinarySearch<T>(IList<T> values, T value)
        {
            return BinarySearch<T, T>(values, value, DefaultSelector<T>);
        }

        public static int BinarySearch<T>(IList<T> values, T value, IComparer<T> comparer)
        {
            return BinarySearch<T, T>(values, value, comparer, DefaultSelector<T>);
        }

        public static int BinarySearch<TSource, TKey>(IList<TSource> values, TKey value, Func<TSource, TKey> selector)
        {
            return BinarySearch<TSource, TKey>(values, value, Comparer<TKey>.Default, selector);
        }

        public static int BinarySearch<TSource, TKey>(IList<TSource> values, TKey value, IComparer<TKey> comparer, Func<TSource, TKey> selector)
        {
            int lower = 0;
            int upper = values.Count - 1;

            while (lower <= upper)
            {
                int middle = (lower + upper) / 2;
                TKey testValue = selector(values[middle]);

                int comparison = comparer.Compare(value, testValue);
                if (comparison == 0)
                    return middle;
                else if (comparison < 0)
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
