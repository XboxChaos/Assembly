/* Copyright 2012 Aaron Dierking, TJ Tunnell, Jordan Mueller, Alex Reed
 * 
 * This file is part of ExtryzeDLL.
 * 
 * Extryze is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Extryze is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ExtryzeDLL.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Util
{
	/// <summary>
	/// Provides methods for locating patterns among byte arrays
	/// strings.
	/// </summary>
    public static class ByteListArray
    {
        static readonly int Empty = -1;

		/// <summary>
		/// Locates position of candidate in byte array and returns int based position
		/// </summary>
		/// <param name="self">The byte array to search.</param>
		/// <param name="candidate">The byte array pattern to find.</param>
		/// <param name="max">The max depth the function, until failing. </param>
		/// <returns>The corresponding position value of candidate, or -1 in failure.</returns>
        public static int Locate(this byte[] self, byte[] candidate, int max = 100)
        {
            if (IsEmptyLocate(self, candidate))
                return Empty;

            var list = new List<int>();

            for (int i = 0; i < self.Length; i++)
            {
				if (i > max)
					return Empty;

                if (IsMatch(self, i, candidate))
                {
                    return i;
                }
            }
            return Empty;
        }

        static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
                return false;

            for (int i = 0; i < candidate.Length; i++)
                if (array[position + i] != candidate[i])
                    return false;

            return true;
        }

        static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                || candidate == null
                || array.Length == 0
                || candidate.Length == 0
                || candidate.Length > array.Length;
        }
    }
}
