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

namespace Blamite.Flexibility
{
	/// <summary>
	///     Represents a collection of values read from a structure.
	/// </summary>
	public class StructureValueCollection
	{
		private readonly Dictionary<string, StructureValueCollection[]> _arrayValues =
			new Dictionary<string, StructureValueCollection[]>();

		private readonly Dictionary<string, float> _floatValues = new Dictionary<string, float>();
		private readonly Dictionary<string, uint> _intValues = new Dictionary<string, uint>();

		private readonly Dictionary<string, byte[]> _rawValues = new Dictionary<string, byte[]>();
		private readonly Dictionary<string, string> _stringValues = new Dictionary<string, string>();

		/// <summary>
		///     Checks if an integer field is present in the value collection.
		/// </summary>
		/// <param name="name">The name of the integer field to search for.</param>
		/// <returns>true if the field is present.</returns>
		public bool HasInteger(string name)
		{
			return _intValues.ContainsKey(name);
		}

		/// <summary>
		///     Checks if a float field is present in the value collection.
		/// </summary>
		/// <param name="name">The name of the float field to search for.</param>
		/// <returns>true if the field is present.</returns>
		public bool HasFloat(string name)
		{
			return _floatValues.ContainsKey(name);
		}

		/// <summary>
		///     Checks if a string field is present in the value collection.
		/// </summary>
		/// <param name="name">The name of the string field to search for.</param>
		/// <returns>true if the field is present.</returns>
		public bool HasString(string name)
		{
			return _stringValues.ContainsKey(name);
		}

		/// <summary>
		///     Checks if an array field is present in the value collection.
		/// </summary>
		/// <param name="name">The name of the array field to search for.</param>
		/// <returns>true if the field is present.</returns>
		public bool HasArray(string name)
		{
			return _arrayValues.ContainsKey(name);
		}

		/// <summary>
		///     Checks if a raw byte array field is present in the value collection.
		/// </summary>
		/// <param name="name">The name of the raw byte array field to search for.</param>
		/// <returns>true if the field is present.</returns>
		public bool HasRaw(string name)
		{
			return _rawValues.ContainsKey(name);
		}

		/// <summary>
		///     Sets the value of a named integer field in a structure.
		///     If the field does not exist, it will be created.
		/// </summary>
		/// <param name="name">The name of the integer field to set.</param>
		/// <param name="value">The value to assign to the field.</param>
		public void SetInteger(string name, uint value)
		{
			_intValues[name] = value;
		}

		/// <summary>
		///     Sets the value of a named float field in a structure.
		///     If the field does not exist, it will be created.
		/// </summary>
		/// <param name="name">The name of the float field to set.</param>
		/// <param name="value">The value to assign to the field.</param>
		public void SetFloat(string name, float value)
		{
			_floatValues[name] = value;
		}

		/// <summary>
		///     Sets the value of a named string field in a structure.
		///     If the field does not exist, it will be created.
		/// </summary>
		/// <param name="name">The name of the string field to set.</param>
		/// <param name="value">The value to assign to the field.</param>
		public void SetString(string name, string value)
		{
			_stringValues[name] = value;
		}

		/// <summary>
		///     Sets the value of a named array field in a structure.
		///     If the field does not exist, it will be created.
		/// </summary>
		/// <param name="name">The name of the array field to set.</param>
		/// <param name="value">The array to assign to the field.</param>
		public void SetArray(string name, StructureValueCollection[] value)
		{
			_arrayValues[name] = value;
		}

		/// <summary>
		///     Sets the value of a named raw byte array field in a structure.
		/// </summary>
		/// <param name="name">The name of the raw field to set.</param>
		/// <param name="value">The byte array to assign to the field.</param>
		public void SetRaw(string name, byte[] value)
		{
			_rawValues[name] = value;
		}

		/// <summary>
		///     Finds an integer field with a given name and retrieves its value if it is found.
		/// </summary>
		/// <param name="name">The name of the integer field to find.</param>
		/// <param name="value">The variable to store the field's value to (if the field exists).</param>
		/// <returns>true if the field was found.</returns>
		public bool FindInteger(string name, out uint value)
		{
			return _intValues.TryGetValue(name, out value);
		}

		/// <summary>
		///     Finds a float field with a given name and retrieves its value if it is found.
		/// </summary>
		/// <param name="name">The name of the float field to find.</param>
		/// <param name="value">The variable to store the field's value to (if the field exists).</param>
		/// <returns>true if the field was found.</returns>
		public bool FindFloat(string name, out float value)
		{
			return _floatValues.TryGetValue(name, out value);
		}

		/// <summary>
		///     Finds a string field with a given name and retrieves its value if it is found.
		/// </summary>
		/// <param name="name">The name of the string field to find.</param>
		/// <param name="value">The variable to store the field's value to (if the field exists).</param>
		/// <returns>true if the field was found.</returns>
		public bool FindString(string name, out string value)
		{
			return _stringValues.TryGetValue(name, out value);
		}

		/// <summary>
		///     Finds an array field with a given name and retrieves its value if it is found.
		/// </summary>
		/// <param name="name">The name of the array field to find.</param>
		/// <param name="value">The variable to store the field's value to (if the field exists).</param>
		/// <returns>true if the field was found.</returns>
		public bool FindArray(string name, out StructureValueCollection[] value)
		{
			return _arrayValues.TryGetValue(name, out value);
		}

		/// <summary>
		///     Finds a raw byte array field with a given name and retrieves its value if it is found.
		/// </summary>
		/// <param name="name">The name of the raw field to find.</param>
		/// <param name="value">The variable to store the field's value to (if the field exists).</param>
		/// <returns>true if the field was found.</returns>
		public bool FindRaw(string name, out byte[] value)
		{
			return _rawValues.TryGetValue(name, out value);
		}

		/// <summary>
		///     Retrieves the value of an integer field,
		///     throwing an exception if the field does not exist.
		/// </summary>
		/// <param name="name">The name of the integer field to retrieve the value of.</param>
		/// <returns>The field's value.</returns>
		/// <exception cref="ArgumentException">Thrown if the field does not exist.</exception>
		public uint GetInteger(string name)
		{
			if (!HasInteger(name))
				throw new ArgumentException("The structure is missing the required \"" + name + "\" field.");
			return _intValues[name];
		}

		/// <summary>
		///     Retrieves the value of a float field,
		///     throwing an exception if the field does not exist.
		/// </summary>
		/// <param name="name">The name of the float field to retrieve the value of.</param>
		/// <returns>The field's value.</returns>
		/// <exception cref="ArgumentException">Thrown if the field does not exist.</exception>
		public float GetFloat(string name)
		{
			if (!HasFloat(name))
				throw new ArgumentException("The structure is missing the required \"" + name + "\" field.");
			return _floatValues[name];
		}

		/// <summary>
		///     Retrieves the value of a string field,
		///     throwing an exception if the field does not exist.
		/// </summary>
		/// <param name="name">The name of the string field to retrieve the value of.</param>
		/// <returns>The field's value.</returns>
		/// <exception cref="ArgumentException">Thrown if the field does not exist.</exception>
		public string GetString(string name)
		{
			if (!HasString(name))
				throw new ArgumentException("The structure is missing the required \"" + name + "\" field.");
			return _stringValues[name];
		}

		/// <summary>
		///     Retrieves the value of a array field,
		///     throwing an exception if the field does not exist.
		/// </summary>
		/// <param name="name">The name of the array field to retrieve the value of.</param>
		/// <returns>The field's value.</returns>
		/// <exception cref="ArgumentException">Thrown if the field does not exist.</exception>
		public StructureValueCollection[] GetArray(string name)
		{
			if (!HasArray(name))
				throw new ArgumentException("The structure is missing the required \"" + name + "\" field.");
			return _arrayValues[name];
		}

		/// <summary>
		///     Retrieves the value of a raw byte array field,
		///     throwing an exception if the field does not exist.
		/// </summary>
		/// <param name="name">The name of the raw byte array field to retrieve the value of.</param>
		/// <returns>The field's value.</returns>
		/// <exception cref="ArgumentException">Thrown if the field does not exist.</exception>
		public byte[] GetRaw(string name)
		{
			if (!HasRaw(name))
				throw new ArgumentException("The structure is missing the required \"" + name + "\" field.");
			return _rawValues[name];
		}

		/// <summary>
		///     Attempts to retrieve the value of an integer field,
		///     returning a specified default value if it does not exist.
		/// </summary>
		/// <param name="name">The name of the integer field to retrieve the value of.</param>
		/// <param name="defaultValue">The value to return if the field is not found.</param>
		/// <returns>The field's value if it was found, or <paramref name="defaultValue" /> otherwise.</returns>
		public uint GetIntegerOrDefault(string name, uint defaultValue)
		{
			uint value;
			if (FindInteger(name, out value))
				return value;
			return defaultValue;
		}

		/// <summary>
		///     Attempts to retrieve the value of a float field,
		///     returning a specified default value if it does not exist.
		/// </summary>
		/// <param name="name">The name of the float field to retrieve the value of.</param>
		/// <param name="defaultValue">The value to return if the field is not found.</param>
		/// <returns>The field's value if it was found, or <paramref name="defaultValue" /> otherwise.</returns>
		public float GetFloatOrDefault(string name, float defaultValue)
		{
			float value;
			if (FindFloat(name, out value))
				return value;
			return defaultValue;
		}

		/// <summary>
		///     Attempts to retrieve the value of a string field,
		///     returning a specified default value if it does not exist.
		/// </summary>
		/// <param name="name">The name of the string field to retrieve the value of.</param>
		/// <param name="defaultValue">The value to return if the field is not found.</param>
		/// <returns>The field's value if it was found, or <paramref name="defaultValue" /> otherwise.</returns>
		public string GetStringOrDefault(string name, string defaultValue)
		{
			string value;
			if (FindString(name, out value))
				return value;
			return defaultValue;
		}
	}
}