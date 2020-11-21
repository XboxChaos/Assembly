using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace Blamite.Serialization
{
	/// <summary>
	///     Defines the layout of a structure.
	/// </summary>
	public class StructureLayout
	{
		private readonly List<ILayoutField> _fields = new List<ILayoutField>();
		private readonly Dictionary<string, ILayoutField> _fieldsByName = new Dictionary<string, ILayoutField>();

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
			AddField(new BasicField(name, type, offset));
		}

		/// <summary>
		///		Adds a StringID field to the structure layout.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		public void AddStringIDField(string name, int offset)
        {
			AddField(new StringIDField(name, offset));
        }

		/// <summary>
		///		Adds a tag reference field to the structure layout.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		public void AddTagReferenceField(string name, int offset, bool withGroup)
		{
			AddField(new TagReferenceField(name, offset, withGroup));
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
			AddField(new ArrayField(name, offset, count, entryLayout));
		}

		/// <summary>
		///     Adds a raw byte array field to the structure layout.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		/// <param name="size">The size of the raw data to read.</param>
		public void AddRawField(string name, int offset, int size)
		{
			AddField(new RawField(name, offset, size));
		}

		/// <summary>
		///		Adds a structure field to the structure layout.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
		/// <param name="layout">The layout of the data in the structure.</param>
		public void AddStructField(string name, int offset, StructureLayout layout)
		{
			AddField(new StructField(name, offset, layout));
		}

		public void AddTagBlockField(string name, int offset, StructureLayout entryLayout)
		{
			AddField(new TagBlockField(name, offset, entryLayout));
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
			foreach (ILayoutField field in _fields)
            {
				if(field is PrimitiveLayoutField primitive)
                {
					primitive.Accept(visitor);
				}
			}
		}

		/// <summary>
		///     Traverses the structure layout in order of field definition and
		///     calls a method defined in an ICacheStructureLayoutVisitor for each field
		///     in the structure.
		/// </summary>
		/// <param name="visitor">The ICacheStructureLayoutVisitor that should visit each structure field.</param>
		public void Accept(ICacheStructureLayoutVisitor visitor)
		{
			foreach (ILayoutField field in _fields)
			{
				if(field is PrimitiveLayoutField primitive)
                {
					primitive.Accept(visitor);
                }
				else if(field is CacheLayoutField cache)
                {
					cache.Accept(visitor);
                }
				else
                {
					throw new NotImplementedException($"Unhandled field type {field.GetType().Name}.");
                }
			}
		}

		private void AddField(ILayoutField field)
		{
			_fields.Add(field);
			_fieldsByName[field.Name] = field;
		}

		private interface ILayoutField
        {
			/// <summary>
			///     Gets the name of the field.
			/// </summary>
			public string Name { get;}

			/// <summary>
			///     Gets the offset of the field.
			/// </summary>
			public int Offset { get;}
		}

        #region Primitive
        /// <summary>
        ///     Base class for a primitve field in a structure.
        /// </summary>
        private abstract class PrimitiveLayoutField : ILayoutField
		{

			/// <summary>
			/// Initializes a new instance of the <see cref="PrimitiveLayoutField"/> class.
			/// </summary>
			/// <param name="name">The field's name.</param>
			/// <param name="offset">The field's offset.</param>
			public PrimitiveLayoutField(string name, int offset)
			{
				Name = name;
				Offset = offset;
			}

			/// <summary>
			///     Gets the name of the field.
			/// </summary>
			public string Name { get; private set; }

			/// <summary>
			///     Gets the offset of the field.
			/// </summary>
			public int Offset { get; private set; }

			/// <summary>
			///     Depending on the type of the field, calls a corresponding method defined in the visitor object.
			/// </summary>
			/// <param name="visitor">The IStructureLayoutVisitor to accept.</param>
			public abstract void Accept(IStructureLayoutVisitor visitor);
		}

		/// <summary>
		///     Represents an array field in a structure.
		/// </summary>
		private class ArrayField : PrimitiveLayoutField
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
				: base(name, offset)
			{
				_count = count;
				_subLayout = entryLayout;
			}

			/// <summary>
			///     Accepts an IStructureLayoutVisitor, calling the VisitArrayField method on it.
			/// </summary>
			/// <param name="visitor">The IStructureLayoutVisitor to accept.</param>
			public override void Accept(IStructureLayoutVisitor visitor)
			{
				visitor.VisitArrayField(Name, Offset, _count, _subLayout);
			}
		}

		/// <summary>
		///     Represents a basic field in a structure.
		/// </summary>
		private class BasicField : PrimitiveLayoutField
		{
			private readonly StructureValueType _type;

			/// <summary>
			///     Constructs a new basic field.
			/// </summary>
			/// <param name="name">The name of the field.</param>
			/// <param name="type">The type of the field's value.</param>
			/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
			public BasicField(string name, StructureValueType type, int offset)
				: base(name, offset)
			{
				_type = type;
			}

			/// <summary>
			///     Accepts an IStructureLayoutVisitor, calling the VisitBasicField method on it.
			/// </summary>
			/// <param name="visitor">The IStructureLayoutVisitor to accept.</param>
			public override void Accept(IStructureLayoutVisitor visitor)
			{
				visitor.VisitBasicField(Name, _type, Offset);
			}
		}

		/// <summary>
		///     Represents a byte array field in a structure.
		/// </summary>
		private class RawField : PrimitiveLayoutField
		{
			private readonly int _size;

			/// <summary>
			///     Constructs a new raw field.
			/// </summary>
			/// <param name="name">The name of the field.</param>
			/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
			/// <param name="size">The size of the raw data to read.</param>
			public RawField(string name, int offset, int size)
				: base(name, offset)
			{
				_size = size;
			}

			/// <summary>
			///     Accepts an IStructureLayoutVisitor, calling the VisitRawField method on it.
			/// </summary>
			/// <param name="visitor">The IStructureLayoutVisitor to accept.</param>
			public override void Accept(IStructureLayoutVisitor visitor)
			{
				visitor.VisitRawField(Name, Offset, _size);
			}
		}

		/// <summary>
		///		Represents an embedded structure in a structure.
		/// </summary>
		private class StructField : PrimitiveLayoutField
		{
			private readonly StructureLayout _layout;

			/// <summary>
			/// Initializes a new instance of the <see cref="StructField"/> class.
			/// </summary>
			/// <param name="name">The name of the field.</param>
			/// <param name="offset">The offset (in bytes) of the field from the beginning of the structure.</param>
			/// <param name="layout">The layout of the data in the structure.</param>
			public StructField(string name, int offset, StructureLayout layout)
				: base(name, offset)
			{
				_layout = layout;
			}

			/// <summary>
			///     Accepts an IStructureLayoutVisitor, calling the VisitStructField method on it.
			/// </summary>
			/// <param name="visitor">The IStructureLayoutVisitor to accept.</param>
			public override void Accept(IStructureLayoutVisitor visitor)
			{
				visitor.VisitStructField(Name, Offset, _layout);
			}
		}
        #endregion

        #region Cache
        /// <summary>
        ///		Base class for a cache layout field in a structure.
        /// </summary>
        private abstract class CacheLayoutField : ILayoutField
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="PrimitiveLayoutField"/> class.
			/// </summary>
			/// <param name="name">The field's name.</param>
			/// <param name="offset">The field's offset.</param>
			public CacheLayoutField(string name, int offset)
			{
				Name = name;
				Offset = offset;
			}

			/// <summary>
			///     Gets the name of the field.
			/// </summary>
			public string Name { get; private set; }

			/// <summary>
			///     Gets the offset of the field.
			/// </summary>
			public int Offset { get; private set; }

			/// <summary>
			///     Depending on the type of the field, calls a corresponding method defined in the visitor object.
			/// </summary>
			/// <param name="visitor">The ICacheStructureLayoutVisitor to accept.</param>
			public abstract void Accept(ICacheStructureLayoutVisitor visitor);
		}

		/// <summary>
		///		Represents a StringID in a structure.
		/// </summary>
		private class StringIDField : CacheLayoutField
		{
			public StringIDField(string name, int offset) : base(name, offset)
			{
			}

			/// <summary>
			///     Accepts an ICacheStructureLayoutVisitor, calling the VisitStringID method on it.
			/// </summary>
			/// <param name="visitor">The ICacheStructureLayoutVisitor to accept.</param>
			public override void Accept(ICacheStructureLayoutVisitor visitor)
			{
				visitor.VisitStringIDField(Name, Offset);
			}
		}

		/// <summary>
		///		Represents a tag reference in a structure.
		/// </summary>
		private class TagReferenceField : CacheLayoutField
		{
			private readonly bool _withGroup;

			public TagReferenceField(string name, int offset, bool withGroup) : base(name, offset)
			{
				_withGroup = withGroup;
			}

			/// <summary>
			///     Accepts an ICacheStructureLayoutVisitor, calling the VisitTagReference method on it.
			/// </summary>
			/// <param name="visitor">The ICacheStructureLayoutVisitor to accept.</param>
			public override void Accept(ICacheStructureLayoutVisitor visitor)
			{
				visitor.VisitTagReferenceField(Name, Offset, _withGroup);
			}
		}

		/// <summary>
		///		Represents a tag block in a structure.
		/// </summary>
		private class TagBlockField : CacheLayoutField
		{
			private readonly StructureLayout _layout;
			public TagBlockField (string name, int offset, StructureLayout entryLayout) : base (name, offset)
            {
				_layout = entryLayout;
            }

			/// <summary>
			///     Accepts an ICacheStructureLayoutVisitor, calling the VisitTagReference method on it.
			/// </summary>
			/// <param name="visitor">The ICacheStructureLayoutVisitor to accept.</param>
			public override void Accept(ICacheStructureLayoutVisitor visitor)
            {
				visitor.VisitTagBlockField(Name, Offset, _layout);
            }
        }
        #endregion

    }
}
