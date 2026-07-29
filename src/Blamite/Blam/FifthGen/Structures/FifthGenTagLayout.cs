using System.Collections.Generic;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     The tag definition schema carried inside a fifth-generation tag payload's <c>blay</c> chunk.
	/// </summary>
	/// <remarks>
	///     <para>
	///         Every other engine in Blamite is told what its structures look like from the outside, by Layout XML or by a
	///         plugin. This one is not: a tag payload describes its own schema, and two payloads of the same group from
	///         different engine builds can legitimately disagree. Trying to express these tables as Layout XML would mean
	///         maintaining a copy of something the file already states authoritatively, and it would be wrong the first time the
	///         engine changed. Only the twelve fixed-width record shapes below are stated in code; everything they mean - names,
	///         types, widths, struct membership - is read from the file. That is why the schema is modelled as objects here
	///         rather than as a layout, and it is the one place in the codebase where the Layout XML convention does not apply.
	///     </para>
	///     <para>
	///         The preamble carries a manifest of twelve record counts, one per table. It agrees with the tables it counts in
	///         every payload anyone has measured, which makes it the single best correctness check the format offers, so a
	///         disagreement is treated as fatal rather than as a warning.
	///     </para>
	/// </remarks>
	public class FifthGenTagLayout
	{
		/// <summary>
		///     The <c>blay</c> chunk version this parser was written against.
		/// </summary>
		public const uint ExpectedVersion = 2;

		/// <summary>
		///     The number of definition tables a layout always contains. Every one is present even when it is empty.
		/// </summary>
		public const int TableCount = 12;

		/// <summary>
		///     The offset, relative to the start of the <c>blay</c> chunk header, at which the record count manifest begins.
		/// </summary>
		public const int ManifestOffset = 0x28;

		/// <summary>
		///     The offset, relative to the start of the <c>blay</c> chunk header, at which the first child chunk begins.
		/// </summary>
		public const int PreambleSize = 0x58;

		/// <summary>
		///     The order the record count manifest lists the tables in, paired with each table's record width. A width of zero
		///     means the manifest counts the table's bytes rather than its records, which is how the name blob is counted.
		/// </summary>
		/// <remarks>
		///     This order is not the same as the order the table chunks appear in, so tables are matched to their counts by magic
		///     rather than by position.
		/// </remarks>
		private static readonly TableShape[] _manifestOrder =
		{
			new TableShape("str*", 0),
			new TableShape("sz+x", 4),
			new TableShape("sz[]", 12),
			new TableShape("csbn", 4),
			new TableShape("dtnm", 4),
			new TableShape("arr!", 12),
			new TableShape("tgft", 12),
			new TableShape("gras", 12),
			new TableShape("stv4", 28),
			new TableShape("blv2", 12),
			new TableShape("rcv2", 12),
			new TableShape("]==[", 24)
		};

		/// <summary>
		///     The order the table chunks are laid out in. Only used to warn when a payload disagrees.
		/// </summary>
		private static readonly string[] _chunkOrder =
		{
			"str*", "sz+x", "sz[]", "csbn", "dtnm", "arr!", "tgft", "gras", "blv2", "rcv2", "]==[", "stv4"
		};

		private readonly ICollection<string> _warnings;
		private readonly Dictionary<int, FifthGenChunk> _tableChunks = new Dictionary<int, FifthGenChunk>();
		private readonly List<uint> _recordCounts = new List<uint>(TableCount);
		private readonly List<string> _options = new List<string>();
		private readonly List<FifthGenEnumDefinition> _enums = new List<FifthGenEnumDefinition>();
		private readonly List<FifthGenArrayDefinition> _arrays = new List<FifthGenArrayDefinition>();
		private readonly List<FifthGenTypeDefinition> _types = new List<FifthGenTypeDefinition>();
		private readonly List<FifthGenFieldDefinition> _fields = new List<FifthGenFieldDefinition>();
		private readonly List<FifthGenBlockDefinition> _blocks = new List<FifthGenBlockDefinition>();
		private readonly List<FifthGenStructDefinition> _structs = new List<FifthGenStructDefinition>();

		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenTagLayout" /> class by reading a <c>blay</c> chunk.
		/// </summary>
		/// <param name="reader">The stream to read from, with its endianness set to the payload's.</param>
		/// <param name="chunk">The payload's <c>blay</c> chunk.</param>
		/// <param name="warnings">A collection to record non-fatal problems in. Can be null.</param>
		public FifthGenTagLayout(IReader reader, FifthGenChunk chunk, ICollection<string> warnings)
		{
			_warnings = warnings ?? new List<string>();
			Load(reader, chunk);
		}

		/// <summary>
		///     Gets the leading sentinel dword of the preamble, which reads 0xFFFFFFFF.
		/// </summary>
		public uint Sentinel { get; private set; }

		/// <summary>
		///     Gets the three fixed ASCII marker dwords of the preamble, which read <c>4444</c>, <c>CCCC</c> and <c>wwww</c>.
		/// </summary>
		public int[] Markers { get; private set; }

		/// <summary>
		///     Gets the per-group dword at preamble offset 0x1C. It is constant for a given group but nothing is known about what
		///     it encodes.
		/// </summary>
		public uint GroupDword { get; private set; }

		/// <summary>
		///     Gets the eight preamble bytes at offset 0x20, for which no interpretation holds across tag groups. Preserved as
		///     read.
		/// </summary>
		public byte[] UndecodedPreamble { get; private set; }

		/// <summary>
		///     Gets the twelve record counts from the preamble manifest, in manifest order.
		/// </summary>
		public IList<uint> RecordCounts
		{
			get { return _recordCounts; }
		}

		/// <summary>
		///     Gets the name blob every name in the layout is a byte offset into.
		/// </summary>
		public FifthGenStringBlob Names { get; private set; }

		/// <summary>
		///     Gets the flat list of enumeration option names, in declaration order. Enumerations tile this list.
		/// </summary>
		public IList<string> Options
		{
			get { return _options; }
		}

		/// <summary>
		///     Gets the enumeration and flags definitions.
		/// </summary>
		public IList<FifthGenEnumDefinition> Enums
		{
			get { return _enums; }
		}

		/// <summary>
		///     Gets the inline array definitions.
		/// </summary>
		public IList<FifthGenArrayDefinition> Arrays
		{
			get { return _arrays; }
		}

		/// <summary>
		///     Gets the field type definitions, which state each type's on-disk width.
		/// </summary>
		public IList<FifthGenTypeDefinition> Types
		{
			get { return _types; }
		}

		/// <summary>
		///     Gets the flat field table. A struct's fields are a contiguous run within it.
		/// </summary>
		public IList<FifthGenFieldDefinition> Fields
		{
			get { return _fields; }
		}

		/// <summary>
		///     Gets the block definitions.
		/// </summary>
		public IList<FifthGenBlockDefinition> Blocks
		{
			get { return _blocks; }
		}

		/// <summary>
		///     Gets the struct definitions.
		/// </summary>
		public IList<FifthGenStructDefinition> Structs
		{
			get { return _structs; }
		}

		/// <summary>
		///     Gets the <c>csbn</c> table, whose record contents are undecoded.
		/// </summary>
		public FifthGenOpaqueTable Csbn { get; private set; }

		/// <summary>
		///     Gets the <c>dtnm</c> table, whose record contents are undecoded.
		/// </summary>
		public FifthGenOpaqueTable Dtnm { get; private set; }

		/// <summary>
		///     Gets the <c>rcv2</c> table, whose record contents are undecoded.
		/// </summary>
		public FifthGenOpaqueTable Rcv2 { get; private set; }

		/// <summary>
		///     Gets the <c>]==[</c> table, whose record contents are undecoded.
		/// </summary>
		public FifthGenOpaqueTable Brackets { get; private set; }

		/// <summary>
		///     Gets a struct definition by index.
		/// </summary>
		/// <param name="index">The index of the struct.</param>
		/// <returns>The struct definition.</returns>
		public FifthGenStructDefinition GetStruct(int index)
		{
			if (index < 0 || index >= _structs.Count)
			{
				throw new FifthGenFormatException(
					$"Struct index {index} is out of range; the layout declares {_structs.Count} struct(s).");
			}
			return _structs[index];
		}

		/// <summary>
		///     Gets an array definition by index.
		/// </summary>
		/// <param name="index">The index of the array.</param>
		/// <returns>The array definition.</returns>
		public FifthGenArrayDefinition GetArray(int index)
		{
			if (index < 0 || index >= _arrays.Count)
			{
				throw new FifthGenFormatException(
					$"Array index {index} is out of range; the layout declares {_arrays.Count} array(s).");
			}
			return _arrays[index];
		}

		/// <summary>
		///     Gets a block definition by index.
		/// </summary>
		/// <param name="index">The index of the block.</param>
		/// <returns>The block definition.</returns>
		public FifthGenBlockDefinition GetBlock(int index)
		{
			if (index < 0 || index >= _blocks.Count)
			{
				throw new FifthGenFormatException(
					$"Block index {index} is out of range; the layout declares {_blocks.Count} block(s).");
			}
			return _blocks[index];
		}

		private void Load(IReader reader, FifthGenChunk chunk)
		{
			if (chunk.Version != ExpectedVersion)
				_warnings.Add($"Tag layout is version {chunk.Version}; this parser was written against version {ExpectedVersion}.");

			FifthGenChunk tables = LoadPreamble(reader, chunk);
			LoadTableChunks(reader, tables);
			ValidateRecordCounts();

			LoadNames(reader);
			LoadTypes(reader);
			LoadOptions(reader);
			LoadEnums(reader);
			LoadArrays(reader);
			LoadFields(reader);
			LoadBlocks(reader);
			LoadStructs(reader);
			LoadOpaqueTables(reader);

			LinkFieldTypes();
			LinkStructFields();
			LinkFieldTargets();
			ComputeInlineSizes();

			reader.SeekTo(chunk.ContentEnd);
		}

		/// <summary>
		///     Reads the <c>blay</c> preamble and the child chunk that holds the definition tables.
		/// </summary>
		private FifthGenChunk LoadPreamble(IReader reader, FifthGenChunk chunk)
		{
			reader.SeekTo(chunk.ContentOffset);
			Sentinel = reader.ReadUInt32();
			if (Sentinel != 0xFFFFFFFF)
				_warnings.Add($"Tag layout preamble sentinel is 0x{Sentinel:X8}, expected 0xFFFFFFFF.");

			Markers = new[] {FifthGenChunk.ReadMagic(reader), FifthGenChunk.ReadMagic(reader), FifthGenChunk.ReadMagic(reader)};
			ValidateMarker(0, "4444");
			ValidateMarker(1, "CCCC");
			ValidateMarker(2, "wwww");

			GroupDword = reader.ReadUInt32();
			UndecodedPreamble = reader.ReadBlock(8);

			for (var i = 0; i < TableCount; i++)
				_recordCounts.Add(reader.ReadUInt32());

			long expected = chunk.HeaderOffset + PreambleSize;
			if (reader.Position != expected)
			{
				throw new FifthGenFormatException(
					$"Tag layout preamble ended at 0x{reader.Position:X}, expected 0x{expected:X}.");
			}

			return FifthGenChunk.ReadExpecting(reader, chunk.ContentEnd, CharConstant.FromString("tgly"),
				"the tag layout's definition tables");
		}

		private void ValidateMarker(int index, string expected)
		{
			int packed = CharConstant.FromString(expected);
			if (Markers[index] != packed)
			{
				_warnings.Add(
					$"Tag layout preamble marker {index} is '{FifthGenChunk.MagicToString(Markers[index])}', expected '{expected}'.");
			}
		}

		/// <summary>
		///     Walks the definition table chunks and indexes them by magic.
		/// </summary>
		private void LoadTableChunks(IReader reader, FifthGenChunk tables)
		{
			reader.SeekTo(tables.ContentOffset);
			var order = new List<string>(TableCount);
			while (reader.Position < tables.ContentEnd)
			{
				FifthGenChunk table = FifthGenChunk.Read(reader, tables.ContentEnd);
				if (_tableChunks.ContainsKey(table.Magic))
				{
					throw new FifthGenFormatException(
						$"Tag layout declares '{table.MagicString}' twice; the second is at 0x{table.HeaderOffset:X}.");
				}
				_tableChunks.Add(table.Magic, table);
				order.Add(table.MagicString);

				// The tables are byte-packed: the name blob's size is rarely a multiple of four and the next chunk
				// starts on the very next byte, so nothing is rounded up here.
				reader.SeekTo(table.ContentEnd);
			}
			tables.EnsureConsumed(reader);

			foreach (TableShape shape in _manifestOrder)
			{
				if (!_tableChunks.ContainsKey(shape.Magic))
				{
					throw new FifthGenFormatException(
						$"Tag layout is missing its '{shape.Name}' table; all {TableCount} are expected even when empty.");
				}
			}

			if (order.Count != TableCount)
			{
				throw new FifthGenFormatException(
					$"Tag layout declares {order.Count} table(s), expected {TableCount}.");
			}

			for (var i = 0; i < TableCount; i++)
			{
				if (order[i] != _chunkOrder[i])
				{
					_warnings.Add(
						$"Tag layout table {i} is '{order[i]}', expected '{_chunkOrder[i]}'. Tables are matched by magic, so the parse continues.");
					break;
				}
			}
		}

		/// <summary>
		///     Checks every table against the record count the preamble manifest claims for it.
		/// </summary>
		private void ValidateRecordCounts()
		{
			for (var i = 0; i < TableCount; i++)
			{
				TableShape shape = _manifestOrder[i];
				FifthGenChunk table = _tableChunks[shape.Magic];
				uint declared = _recordCounts[i];

				if (shape.RecordSize == 0)
				{
					// The name blob is counted in bytes rather than in records.
					if (table.Size != declared)
					{
						throw new FifthGenFormatException(
							$"Tag layout manifest claims {declared} byte(s) for '{shape.Name}' but its chunk at 0x{table.HeaderOffset:X} holds {table.Size}.");
					}
					continue;
				}

				if (table.Size%shape.RecordSize != 0)
				{
					throw new FifthGenFormatException(
						$"'{shape.Name}' chunk at 0x{table.HeaderOffset:X} holds {table.Size} byte(s), which is not a whole number of {shape.RecordSize} byte record(s).");
				}

				uint actual = table.Size/(uint) shape.RecordSize;
				if (actual != declared)
				{
					throw new FifthGenFormatException(
						$"Tag layout manifest claims {declared} record(s) for '{shape.Name}' but its chunk at 0x{table.HeaderOffset:X} holds {actual}.");
				}
			}
		}

		private void LoadNames(IReader reader)
		{
			Names = new FifthGenStringBlob(reader, _tableChunks[CharConstant.FromString("str*")]);
		}

		private void LoadTypes(IReader reader)
		{
			foreach (long offset in EnumerateRecords(reader, "tgft", 12))
			{
				reader.SeekTo(offset);
				uint nameOffset = reader.ReadUInt32();
				uint size = reader.ReadUInt32();
				uint flags = reader.ReadUInt32();
				_types.Add(new FifthGenTypeDefinition(nameOffset, size, flags));
			}

			foreach (FifthGenTypeDefinition type in _types)
			{
				type.Name = Names.GetString(reader, type.NameOffset, _warnings);
				type.Type = FifthGenFieldTypes.Parse(type.Name);

				if (type.Type == FifthGenFieldType.Unknown)
				{
					_warnings.Add(
						$"Field type '{type.Name}' is not in the known vocabulary; its {type.Size} byte(s) will be read as opaque data.");
					continue;
				}

				int known = FifthGenFieldTypes.GetKnownSize(type.Type);
				if (known >= 0 && known != (int) type.Size)
				{
					_warnings.Add(
						$"Field type '{type.Name}' declares a width of {type.Size} byte(s) but is expected to be {known}. The declared width is used.");
				}
			}
		}

		private void LoadOptions(IReader reader)
		{
			var nameOffsets = new List<uint>();
			foreach (long offset in EnumerateRecords(reader, "sz+x", 4))
			{
				reader.SeekTo(offset);
				nameOffsets.Add(reader.ReadUInt32());
			}

			foreach (uint nameOffset in nameOffsets)
				_options.Add(Names.GetString(reader, nameOffset, _warnings));
		}

		private void LoadEnums(IReader reader)
		{
			var index = 0;
			foreach (long offset in EnumerateRecords(reader, "sz[]", 12))
			{
				reader.SeekTo(offset);
				uint nameOffset = reader.ReadUInt32();
				uint optionCount = reader.ReadUInt32();
				uint firstOption = reader.ReadUInt32();
				_enums.Add(new FifthGenEnumDefinition(index++, nameOffset, optionCount, firstOption));
			}

			foreach (FifthGenEnumDefinition definition in _enums)
			{
				definition.Name = Names.GetString(reader, definition.NameOffset, _warnings);

				long last = (long) definition.FirstOption + definition.OptionCount;
				if (last > _options.Count)
				{
					throw new FifthGenFormatException(
						$"Enumeration '{definition.Name}' claims option(s) {definition.FirstOption} to {last - 1} but the layout only declares {_options.Count}.");
				}

				var options = new List<string>((int) definition.OptionCount);
				for (var i = 0; i < definition.OptionCount; i++)
					options.Add(_options[(int) definition.FirstOption + i]);
				definition.Options = options;
			}
		}

		private void LoadArrays(IReader reader)
		{
			var index = 0;
			foreach (long offset in EnumerateRecords(reader, "arr!", 12))
			{
				reader.SeekTo(offset);
				uint nameOffset = reader.ReadUInt32();
				uint count = reader.ReadUInt32();
				uint structIndex = reader.ReadUInt32();
				_arrays.Add(new FifthGenArrayDefinition(index++, nameOffset, count, structIndex));
			}

			foreach (FifthGenArrayDefinition definition in _arrays)
				definition.Name = Names.GetString(reader, definition.NameOffset, _warnings);
		}

		private void LoadFields(IReader reader)
		{
			var index = 0;
			foreach (long offset in EnumerateRecords(reader, "gras", 12))
			{
				reader.SeekTo(offset);
				uint nameOffset = reader.ReadUInt32();
				uint typeIndex = reader.ReadUInt32();
				uint aux = reader.ReadUInt32();
				_fields.Add(new FifthGenFieldDefinition(index++, nameOffset, typeIndex, aux));
			}

			foreach (FifthGenFieldDefinition field in _fields)
				field.Name = Names.GetString(reader, field.NameOffset, _warnings);
		}

		private void LoadBlocks(IReader reader)
		{
			var index = 0;
			foreach (long offset in EnumerateRecords(reader, "blv2", 12))
			{
				reader.SeekTo(offset);
				uint nameOffset = reader.ReadUInt32();
				uint maxCount = reader.ReadUInt32();
				uint structIndex = reader.ReadUInt32();
				_blocks.Add(new FifthGenBlockDefinition(index++, nameOffset, maxCount, structIndex));
			}

			foreach (FifthGenBlockDefinition definition in _blocks)
				definition.Name = Names.GetString(reader, definition.NameOffset, _warnings);
		}

		private void LoadStructs(IReader reader)
		{
			var index = 0;
			foreach (long offset in EnumerateRecords(reader, "stv4", 28))
			{
				reader.SeekTo(offset);
				byte[] guid = reader.ReadBlock(16);
				uint nameOffset = reader.ReadUInt32();
				int firstField = reader.ReadInt32();
				uint aux = reader.ReadUInt32();
				_structs.Add(new FifthGenStructDefinition(index++, guid, nameOffset, firstField, aux));
			}

			foreach (FifthGenStructDefinition definition in _structs)
				definition.Name = Names.GetString(reader, definition.NameOffset, _warnings);
		}

		private void LoadOpaqueTables(IReader reader)
		{
			Csbn = LoadOpaqueTable(reader, "csbn", 4);
			Dtnm = LoadOpaqueTable(reader, "dtnm", 4);
			Rcv2 = LoadOpaqueTable(reader, "rcv2", 12);
			Brackets = LoadOpaqueTable(reader, "]==[", 24);
		}

		private FifthGenOpaqueTable LoadOpaqueTable(IReader reader, string name, int recordSize)
		{
			int magic = CharConstant.FromString(name);
			FifthGenChunk chunk = _tableChunks[magic];
			byte[] raw = chunk.ReadContent(reader);
			var count = (int) (chunk.Size/(uint) recordSize);
			if (count > 0)
			{
				_warnings.Add(
					$"'{name}' holds {count} record(s) of {recordSize} byte(s) whose contents are undecoded. They were skipped by width and preserved verbatim.");
			}
			return new FifthGenOpaqueTable(magic, recordSize, count, raw);
		}

		private void LinkFieldTypes()
		{
			foreach (FifthGenFieldDefinition field in _fields)
			{
				if (field.TypeIndex >= (uint) _types.Count)
				{
					throw new FifthGenFormatException(
						$"Field {field.Index} names type index {field.TypeIndex} but the layout only declares {_types.Count} type(s).");
				}
				field.TypeDefinition = _types[(int) field.TypeIndex];
				field.AuxKind = FifthGenFieldTypes.GetAuxKind(field.Type, field.TypeName);
			}
		}

		/// <summary>
		///     Assigns each struct the run of fields it owns.
		/// </summary>
		/// <remarks>
		///     The run is located from the struct's own first-field index, never inferred from where the previous struct's run
		///     ended. Working backwards from the terminators looks like it should give the same answer and does not: some tag
		///     groups lay their runs out in an order that does not follow the struct table, and inferring the mapping fails on the
		///     large majority of payloads where reading the index succeeds on essentially all of them.
		/// </remarks>
		private void LinkStructFields()
		{
			foreach (FifthGenStructDefinition definition in _structs)
			{
				if (definition.FirstField < 0 || definition.FirstField > _fields.Count)
				{
					throw new FifthGenFormatException(
						$"Struct '{definition.Name}' (index {definition.Index}) starts at field {definition.FirstField}, which is outside the {_fields.Count} field(s) the layout declares.");
				}

				var fields = new List<FifthGenFieldDefinition>();
				var terminated = false;
				for (int i = definition.FirstField; i < _fields.Count; i++)
				{
					FifthGenFieldDefinition field = _fields[i];
					if (field.Type == FifthGenFieldType.TerminatorX)
					{
						definition.Terminator = field;
						terminated = true;
						break;
					}
					fields.Add(field);
				}

				if (!terminated)
				{
					throw new FifthGenFormatException(
						$"Struct '{definition.Name}' (index {definition.Index}) starts at field {definition.FirstField} and runs off the end of the field table without a terminator.");
				}

				definition.Fields = fields;
			}
		}

		private void LinkFieldTargets()
		{
			foreach (FifthGenFieldDefinition field in _fields)
			{
				switch (field.AuxKind)
				{
					case FifthGenAuxKind.StructIndex:
						field.Struct = GetStruct((int) field.Aux);
						break;
					case FifthGenAuxKind.ArrayIndex:
						field.Array = GetArray((int) field.Aux);
						break;
					case FifthGenAuxKind.BlockIndex:
						if (field.Type == FifthGenFieldType.Block)
						{
							field.Block = GetBlock((int) field.Aux);
						}
						else if (field.Aux < (uint) _blocks.Count)
						{
							// A block index field only names the block its value counts against; the walk does not
							// need it, so an index that does not resolve is a warning rather than a failure.
							field.Block = _blocks[(int) field.Aux];
						}
						else
						{
							_warnings.Add(
								$"Field '{field.Name}' ({field.TypeName}) names block {field.Aux} but the layout only declares {_blocks.Count}. Its value is still readable.");
						}
						break;
					case FifthGenAuxKind.EnumIndex:
						if (field.Aux < (uint) _enums.Count)
						{
							field.Enum = _enums[(int) field.Aux];
						}
						else
						{
							_warnings.Add(
								$"Field '{field.Name}' ({field.TypeName}) names enumeration {field.Aux} but the layout only declares {_enums.Count}. Its value is still readable.");
						}
						break;
				}
			}

			foreach (FifthGenBlockDefinition definition in _blocks)
				definition.Struct = GetStruct((int) definition.StructIndex);
			foreach (FifthGenArrayDefinition definition in _arrays)
				definition.Struct = GetStruct((int) definition.StructIndex);
		}

		/// <summary>
		///     Works out how many bytes one instance of each struct occupies in packed element data.
		/// </summary>
		private void ComputeInlineSizes()
		{
			var state = new SizeState[_structs.Count];
			for (var i = 0; i < _structs.Count; i++)
				ResolveStructSize(i, state);
		}

		private int ResolveStructSize(int index, SizeState[] state)
		{
			FifthGenStructDefinition definition = GetStruct(index);
			if (state[index] == SizeState.Resolved)
				return definition.InlineSize;
			if (state[index] == SizeState.InProgress)
			{
				throw new FifthGenFormatException(
					$"Struct '{definition.Name}' (index {index}) contains itself, so its inline size cannot be computed.");
			}

			state[index] = SizeState.InProgress;
			var total = 0;
			var emits = false;
			foreach (FifthGenFieldDefinition field in definition.Fields)
			{
				field.InlineSize = ResolveFieldSize(field, state);
				total += field.InlineSize;
				emits |= FieldEmitsSections(field);
			}
			definition.InlineSize = total;
			definition.EmitsSections = emits;
			state[index] = SizeState.Resolved;
			return total;
		}

		private static bool FieldEmitsSections(FifthGenFieldDefinition field)
		{
			switch (field.Type)
			{
				case FifthGenFieldType.Block:
				case FifthGenFieldType.Struct:
				case FifthGenFieldType.StringId:
				case FifthGenFieldType.Data:
				case FifthGenFieldType.TagReference:
				case FifthGenFieldType.PageableResource:
					return true;
				case FifthGenFieldType.Array:
					return field.Array.Struct.EmitsSections;
				default:
					return false;
			}
		}

		private int ResolveFieldSize(FifthGenFieldDefinition field, SizeState[] state)
		{
			switch (field.Type)
			{
				case FifthGenFieldType.Pad:
					// A padding field's width is its auxiliary word, not its type's declared width, which is zero.
					return (int) field.Aux;
				case FifthGenFieldType.Struct:
					return ResolveStructSize((int) field.Aux, state);
				case FifthGenFieldType.Array:
					FifthGenArrayDefinition array = GetArray((int) field.Aux);
					return (int) array.Count*ResolveStructSize((int) array.StructIndex, state);
				case FifthGenFieldType.TerminatorX:
				case FifthGenFieldType.Custom:
					return 0;
				default:
					return (int) field.TypeDefinition.Size;
			}
		}

		private IEnumerable<long> EnumerateRecords(IReader reader, string name, int recordSize)
		{
			FifthGenChunk chunk = _tableChunks[CharConstant.FromString(name)];
			var count = (int) (chunk.Size/(uint) recordSize);
			for (var i = 0; i < count; i++)
				yield return chunk.ContentOffset + i*(long) recordSize;
		}

		private enum SizeState
		{
			Unresolved,
			InProgress,
			Resolved
		}

		private struct TableShape
		{
			public TableShape(string name, int recordSize)
			{
				Name = name;
				Magic = CharConstant.FromString(name);
				RecordSize = recordSize;
			}

			public string Name { get; }
			public int Magic { get; }
			public int RecordSize { get; }
		}
	}
}
