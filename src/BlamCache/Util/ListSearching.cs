using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Util
{
    public static class ListSearching
    {
        // Binary search methods adapted from http://stackoverflow.com/questions/967047/how-to-perform-a-binary-search-on-ilistt

        public static int BinarySearch<T>(IList<T> values, T value)
        {
            return BinarySearch<T>(values, value, Comparer<T>.Default);
        }

        public static int BinarySearch<T>(IList<T> values, T value, IComparer<T> comparer)
        {
            int lower = 0;
            int upper = values.Count - 1;

            while (lower <= upper)
            {
                int middle = (lower + upper) / 2;
                T testValue = values[middle];
                
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
    }
}
