using System;
using System.Collections.Generic;

namespace Blamite.RTE.Console
{
	/// <summary>
	/// Parses and indexes a specific type of response recieved from a xbox/360 console.
	/// </summary>
	public class FormattedResponse
	{
		private Dictionary<string, uint> _numberValues;
		private Dictionary<string, string> _stringValues;

		public FormattedResponse()
		{
			_numberValues = new Dictionary<string, uint>();
			_stringValues = new Dictionary<string, string>();
		}

		/// <summary>
		/// Parses comma-separated numbers in both hex and decimal formats.
		/// </summary>
		/// <param name="text">String containing the values to parse.</param>
		public void ParseNumberValues(string text)
		{
			string cleaned = text.Replace(",", "").Replace("\r\n", "");
			string[] split = cleaned.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			for (int i = 0; i < split.Length; i++)
			{
				string[] moreSplit = split[i].Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

				string key = moreSplit[0];
				string value = moreSplit[1];
				uint number;
				if (value.Contains("0x"))
					number = uint.Parse(value.Substring(2), System.Globalization.NumberStyles.HexNumber);
				else
					number = uint.Parse(value);

				_numberValues[key] = number;
			}
		}

		/// <summary>
		/// Parses a single string value.
		/// </summary>
		/// <param name="text">String containing the value to parse.</param>
		public void ParseStringValue(string text)
		{
			string cleaned = text.Replace("\r\n", "");

			int start = cleaned.IndexOf("=\"");

			string key = cleaned.Substring(0, start);
			string value = cleaned.Substring(start + 2).Replace("\"", "");

			_stringValues[key] = value;
		}

		/// <summary>
		/// Tries to retrieve the string value stored under a specific key.
		/// </summary>
		/// <param name="key">Key to find.</param>
		/// <returns>The string associated wuth the key, otherwise null.</returns>
		public string FindStringValue(string key)
		{
			if (!_stringValues.TryGetValue(key, out string result))
				return null;

			return result;
		}

		/// <summary>
		/// Tries to retrieve the number value stored under a specific key.
		/// </summary>
		/// <param name="key">Key to find.</param>
		/// <returns>The number value associated wuth the key, otherwise null.</returns>
		public uint? FindNumberValue(string key)
		{
			if (!_numberValues.TryGetValue(key, out uint result))
				return null;

			return result;
		}

		/// <summary>
		/// Writes all values to a list.
		/// </summary>
		/// <returns>All values as a list.</returns>
		public List<string> DumpValues()
		{
			var result = new List<string>();

			foreach (KeyValuePair<string, uint> pair in _numberValues)
			{
				result.Add($"{pair.Key} = 0x{pair.Value:X}");
			}
			foreach (KeyValuePair<string, string> pair in _stringValues)
			{
				result.Add($"{pair.Key} = {pair.Value}");
			}

			return result;
		}
	}
}
