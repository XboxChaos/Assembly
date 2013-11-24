using System.Linq;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.LanguagePack
{
	/// <summary>
	///     A range of strings in a string table.
	/// </summary>
	public class StringRange
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="StringRange" /> class.
		/// </summary>
		/// <param name="startIndex">The starting index of the range.</param>
		/// <param name="size">The number of strings in the range.</param>
		public StringRange(int startIndex, int size)
		{
			StartIndex = startIndex;
			Size = size;
		}

		/// <summary>
		///     Gets or sets the starting index of the range.
		/// </summary>
		public int StartIndex { get; set; }

		/// <summary>
		///     Gets or sets the number of strings in the range.
		/// </summary>
		public int Size { get; set; }

		/// <summary>
		///     Serializes this instance.
		/// </summary>
		/// <returns>The serialized structure values.</returns>
		public StructureValueCollection Serialize()
		{
			var values = new StructureValueCollection();
			values.SetInteger("range start index", (uint) StartIndex);
			values.SetInteger("range size", (uint) Size);
			return values;
		}

		/// <summary>
		///     Deserializes a string range from a structure.
		/// </summary>
		/// <param name="values">The values in the structure.</param>
		/// <returns>The deserialized range.</returns>
		public static StringRange Deserialize(StructureValueCollection values)
		{
			var startIndex = (int) values.GetInteger("range start index");
			var size = (int) values.GetInteger("range size");
			return new StringRange(startIndex, size);
		}
	}

	/// <summary>
	///     A multilingual_unicode_string_list ('unic') tag.
	/// </summary>
	public class ThirdGenMultilingualStringList
	{
		private readonly StructureLayout _layout;

		public ThirdGenMultilingualStringList(IReader reader, ITag tag, EngineDescription buildInfo)
		{
			Tag = tag;
			_layout = buildInfo.Layouts.GetLayout("unic");

			Load(reader);
		}

		/// <summary>
		///     The tag that the locale list was loaded from.
		/// </summary>
		public ITag Tag { get; private set; }

		/// <summary>
		///     The range of strings that this group occupies in each table, in order by language.
		/// </summary>
		public StringRange[] Ranges { get; private set; }

		/// <summary>
		///     Saves changes made to the string list.
		/// </summary>
		/// <param name="writer">The stream to write to.</param>
		public void SaveChanges(IWriter writer)
		{
			var values = new StructureValueCollection();
			StructureValueCollection[] rangeValues = Ranges.Select(r => r.Serialize()).ToArray();
			values.SetArray("language ranges", rangeValues);

			writer.SeekTo(Tag.MetaLocation.AsOffset());
			StructureWriter.WriteStructure(values, _layout, writer);
		}

		/// <summary>
		///     Loads the string list.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		private void Load(IReader reader)
		{
			reader.SeekTo(Tag.MetaLocation.AsOffset());
			StructureValueCollection values = StructureReader.ReadStructure(reader, _layout);

			StructureValueCollection[] rangeValues = values.GetArray("language ranges");
			Ranges = rangeValues.Select(v => StringRange.Deserialize(v)).ToArray();
		}
	}
}