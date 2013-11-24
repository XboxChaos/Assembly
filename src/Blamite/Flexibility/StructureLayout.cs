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

using System.Collections.Generic;

namespace Blamite.Flexibility
{
	/// <summary>
	///     Defines the layout of a structure.
	/// </summary>
	public class StructureLayout
	{
		private readonly List<IStructField> _fields = new List<IStructField>();
		private readonly Dictionary<string, IStructField> _fieldsByName = new Dictionary<string, IStructField>();

		/// <summary>
		///     Constructs a new StructureLayout with a structure size of 0.
		/// </summary>
		public StructureLayout()
		{
		}

		/// <summary>
		///     Constructs a new StructureLayout.
		/// </summary>
		/// <param name="size">The size of the structure in bytes.</param>
		public StructureLayout(int size)
		{
			Size = size;
		}

		/// <summary>
		///     Gets the size of the structure.
		///     Defaults to 0 if not specified at construction time.
		/// </summary>
		public int Size { get; private set; }

		/// <summary>
		///     Adds a basic field to the structure layout.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="type">The type of the field's value.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		public void AddBasicField(string name, StructureValueType type, int offset)
		{
			var field = new BasicField(name, type, offset);
			_fields.Add(field);
			_fieldsByName[name] = field;
		}

		/// <summary>
		///     Adds an array field to the structure layout.
		/// </summary>
		/// <param name="name">The name of the array.</param>
		/// <param name="offset">The offset (in bytes) of the array from the beginning of the structure.</param>
		/// <param name="count">The number of entries in the array.</param>
		/// <param name="entryLayout">The layout of each entry in the array.</param>
		public void AddArrayField(string name, int offset, int count, StructureLayout entryLayout)
		{
			var field = new ArrayField(name, offset, count, entryLayout);
			_fields.Add(field);
			_fieldsByName[name] = field;
		}

		/// <summary>
		///     Adds a raw byte array field to the structure layout.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		/// <param name="size">The size of the raw data to read.</param>
		public void AddRawField(string name, int offset, int size)
		{
			var field = new RawField(name, offset, size);
			_fields.Add(field);
			_fieldsByName[name] = field;
		}

		/// <summary>
		///     Returns whether or not a field is defined in the layout.
		/// </summary>
		/// <param name="name">The name of the field to search for.</param>
		/// <returns><c>true</c> if the field is defined, or <c>false</c> otherwise.</returns>
		public bool HasField(string name)
		{
			return _fieldsByName.ContainsKey(name);
		}

		/// <summary>
		///     Gets the offset of the field with a given name.
		/// </summary>
		/// <param name="name">The name of the field to get the offset of.</param>
		/// <returns>The offset of the field from the start of the structure.</returns>
		public int GetFieldOffset(string name)
		{
			return _fieldsByName[name].Offset;
		}

		/// <summary>
		///     Traverses the structure layout in order of field definition and
		///     calls a method defined in an IStructureLayoutVisitor for each field
		///     in the structure.
		/// </summary>
		/// <param name="visitor">The IStructureLayoutVisitor that should visit each structure field.</param>
		public void Accept(IStructureLayoutVisitor visitor)
		{
			foreach (IStructField field in _fields)
				field.Accept(visitor);
		}

		/// <summary>
		///     Represents an array field in a structure.
		/// </summary>
		private class ArrayField : IStructField
		{
			private readonly int _count;
			private readonly StructureLayout _subLayout;

			/// <summary>
			///     Constructs a new array field.
			/// </summary>
			/// <param name="name">The name of the array.</param>
			/// <param name="offset">The offset (in bytes) of the array from the beginning of the structure.</param>
			/// <param name="count">The number of entries in the array.</param>
			/// <param name="entryLayout">The layout of each entry in the array.</param>
			public ArrayField(string name, int offset, int count, StructureLayout entryLayout)
			{
				Name = name;
				Offset = offset;
				_count = count;
				_subLayout = entryLayout;
			}

			public string Name { get; private set; }
			public int Offset { get; private set; }

			/// <summary>
			///     Accepts an IStructureLayoutVisitor, calling the VisitArrayField method on it.
			/// </summary>
			/// <param name="visitor">The IStructureLayoutVisitor to accept.</param>
			public void Accept(IStructureLayoutVisitor visitor)
			{
				visitor.VisitArrayField(Name, Offset, _count, _subLayout);
			}
		}

		/// <summary>
		///     Represents a basic field in a structure.
		/// </summary>
		private class BasicField : IStructField
		{
			private readonly StructureValueType _type;

			/// <summary>
			///     Constructs a new basic field.
			/// </summary>
			/// <param name="name">The name of the field.</param>
			/// <param name="type">The type of the field's value.</param>
			/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
			public BasicField(string name, StructureValueType type, int offset)
			{
				Name = name;
				_type = type;
				Offset = offset;
			}

			public string Name { get; private set; }
			public int Offset { get; private set; }

			/// <summary>
			///     Accepts an IStructureLayoutVisitor, calling the VisitBasicField method on it.
			/// </summary>
			/// <param name="visitor">The IStructureLayoutVisitor to accept.</param>
			public void Accept(IStructureLayoutVisitor visitor)
			{
				visitor.VisitBasicField(Name, _type, Offset);
			}
		}

		/// <summary>
		///     Interface for a field in a structure.
		/// </summary>
		private interface IStructField
		{
			/// <summary>
			///     Gets the name of the field.
			/// </summary>
			string Name { get; }

			/// <summary>
			///     Gets the offset of the field.
			/// </summary>
			int Offset { get; }

			/// <summary>
			///     Depending on the type of the field, calls a corresponding method defined in the visitor object.
			/// </summary>
			/// <param name="visitor">The IStructureLayoutVisitor to accept.</param>
			void Accept(IStructureLayoutVisitor visitor);
		}

		/// <summary>
		///     Represents a byte array field in a structure.
		/// </summary>
		private class RawField : IStructField
		{
			private readonly int _size;

			/// <summary>
			///     Constructs a new raw field.
			/// </summary>
			/// <param name="name">The name of the field.</param>
			/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
			/// <param name="size">The size of the raw data to read.</param>
			public RawField(string name, int offset, int size)
			{
				Name = name;
				Offset = offset;
				_size = size;
			}

			public string Name { get; private set; }
			public int Offset { get; private set; }

			/// <summary>
			///     Accepts an IStructureLayoutVisitor, calling the VisitRawField method on it.
			/// </summary>
			/// <param name="visitor">The IStructureLayoutVisitor to accept.</param>
			public void Accept(IStructureLayoutVisitor visitor)
			{
				visitor.VisitRawField(Name, Offset, _size);
			}
		}
	}
}