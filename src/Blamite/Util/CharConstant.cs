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

using System.Text;

namespace Blamite.Util
{
	/// <summary>
	///     Provides methods for converting C-style character constants into managed
	///     strings.
	/// </summary>
	public static class CharConstant
	{
		/// <summary>
		///     Converts a numeric character constant into a string.
		/// </summary>
		/// <param name="constant">The numeric character constant to convert.</param>
		/// <returns>The corresponding string that the constant looks like.</returns>
		public static string ToString(int constant)
		{
			int index = 4;
			var chars = new char[4];

			// Fill the chars array from back-to-front
			// Each loop, we take the low byte of constant, put it in the array,
			// and then shift constant over 8 bits to get the next byte
			while (constant > 0)
			{
				index--;
				chars[index] = (char) (constant & 0xFF);
				constant >>= 8;
			}
			if (index == 4)
				return "";
			return new string(chars, index, chars.Length - index);
		}

		/// <summary>
		///     Converts a string into a numeric character constant.
		///     This has the same effect as single-quoting a string in C.
		/// </summary>
		/// <param name="str">The string to convert.</param>
		/// <returns>The numeric character constant.</returns>
		public static int FromString(string str)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(str);
			int result = 0;
			foreach (byte b in bytes)
			{
				result <<= 8;
				result |= b;
			}
			return result;
		}
	}
}