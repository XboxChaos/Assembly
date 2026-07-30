using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Assembly.Avalonia.Services;
using Blamite.IO;

namespace Assembly.Avalonia.ViewModels
{
	/// <summary>How a row's value should be edited. Drives which editor the value sidebar shows.</summary>
	public enum EditorKind
	{
		None, Integer, Float, Vector2, Vector3, Vector4, RangeFloat, RangeInt16,
		Enum, Flags, Color, Ascii, Utf16, StringId, Block, ReadOnly
	}

	/// <summary>One selectable option for an enum editor (wraps a (Name, Value) schema choice).</summary>
	public sealed class ChoiceOption
	{
		public ChoiceOption(string name, long value) { Name = name; Value = value; }
		public string Name { get; }
		public long Value { get; }
		public override string ToString() => Name;
	}

	/// <summary>One row in a tag document's field table: a scalar field, a comment, or a tag
	/// block header (which can be expanded to show its elements' fields as nested rows).</summary>
	public sealed class MetaRowViewModel : ObservableObject
	{
		public MetaRowViewModel(MetaFieldDef def, int schemaIndex)
		{
			Def = def;
			SchemaIndex = schemaIndex;
			Editor = ClassifyEditor(def);
		}

		public MetaFieldDef Def { get; }
		public int SchemaIndex { get; }
		public EditorKind Editor { get; }

		private int _depth;
		public int Depth { get => _depth; set => Set(ref _depth, value); }

		private long _absoluteOffset;
		public long AbsoluteOffset { get => _absoluteOffset; set { if (Set(ref _absoluteOffset, value)) Raise(nameof(OffsetLabel)); } }

		public string OffsetLabel => $"0x{AbsoluteOffset:X6}";
		public string Name => string.IsNullOrEmpty(Def.Name) ? "-" : Def.Name;
		public string KindLabel => Def.Kind == MetaFieldKind.TagBlock ? $"TagBlock[{Def.EntrySize:X}]" : Def.KindLabel;

		private string _displayValue = "";
		public string DisplayValue { get => _displayValue; set => Set(ref _displayValue, value); }

		public bool IsComment => Def.Kind == MetaFieldKind.Comment;
		public bool IsBlock => Def.Kind == MetaFieldKind.TagBlock;

		public IReadOnlyList<ChoiceOption> Choices =>
			Def.Choices?.Select(c => new ChoiceOption(c.Name, c.Value)).ToList() ?? new List<ChoiceOption>();

		// ---- tag block navigation state ----
		private bool _isExpanded;
		public bool IsExpanded { get => _isExpanded; set => Set(ref _isExpanded, value); }

		private int _elementCount;
		public int ElementCount { get => _elementCount; set { if (Set(ref _elementCount, value)) Raise(nameof(ElementSummary)); } }

		private int _elementIndex;
		public int ElementIndex
		{
			get => _elementIndex;
			set { if (Set(ref _elementIndex, Math.Max(0, value))) Raise(nameof(ElementSummary)); }
		}

		public long ElementsBaseFileOffset { get; set; } = -1;
		public string ElementSummary => ElementCount == 0 ? "0 entries" : $"element {ElementIndex + 1} of {ElementCount}  (index {ElementIndex})";

		// ---- edit state ----
		public FieldEditState? Original { get; set; }
		public FieldEditState? Current { get; set; }
		private long? _lastSeededOffset;
		public long? LastSeededOffset { get => _lastSeededOffset; set => _lastSeededOffset = value; }

		public bool IsDirty => Original != null && Current != null && !Original.ValueEquals(Current);

		public void NotifyEdited()
		{
			Raise(nameof(IsDirty));
			Raise(nameof(DisplayValue));
		}

		public void Revert()
		{
			if (Original == null) return;
			Current = Original.Clone();
			NotifyEdited();
		}

		private static EditorKind ClassifyEditor(MetaFieldDef d) => d.Kind switch
		{
			MetaFieldKind.TagBlock => EditorKind.Block,
			MetaFieldKind.UInt8 or MetaFieldKind.Int8 or MetaFieldKind.UInt16 or MetaFieldKind.Int16 or
			MetaFieldKind.UInt32 or MetaFieldKind.Int32 or MetaFieldKind.UInt64 or MetaFieldKind.Int64 => EditorKind.Integer,
			MetaFieldKind.Float32 or MetaFieldKind.Degree => EditorKind.Float,
			MetaFieldKind.Point2 or MetaFieldKind.Vector2 or MetaFieldKind.Degree2 => EditorKind.Vector2,
			MetaFieldKind.Point3 or MetaFieldKind.Vector3 or MetaFieldKind.Degree3 => EditorKind.Vector3,
			MetaFieldKind.Vector4 => EditorKind.Vector4,
			MetaFieldKind.RangeFloat32 or MetaFieldKind.RangeDegree => EditorKind.RangeFloat,
			MetaFieldKind.RangeInt16 => EditorKind.RangeInt16,
			MetaFieldKind.Enum => EditorKind.Enum,
			MetaFieldKind.Flags => EditorKind.Flags,
			MetaFieldKind.ColorInt => EditorKind.Color,
			MetaFieldKind.Ascii => EditorKind.Ascii,
			MetaFieldKind.Utf16 => EditorKind.Utf16,
			MetaFieldKind.StringId or MetaFieldKind.OldStringId => EditorKind.StringId,
			_ => EditorKind.ReadOnly
		};
	}

	/// <summary>
	///     One open tag tab. Owns the flattened, navigable row list for a single tag's meta:
	///     scalar fields at the top level, and tag blocks that expand in place to show their
	///     elements' fields (with a real element-index spinner backed by
	///     <see cref="MetaValueReader.ReadBlockHeader" />'s live pointer conversion — not a
	///     canned list). Tracks per-row dirty state and writes edits back through
	///     <see cref="MetaValueWriter" /> on Save.
	/// </summary>
	public sealed class TagDocumentViewModel : ObservableObject
	{
		private readonly CacheSession _session;
		private readonly IReadOnlyList<MetaFieldDef> _schema;
		private readonly Dictionary<int, MetaRowViewModel> _rowCache = new();
		private readonly LogService? _log;

		public TagDocumentViewModel(TagInfo tag, LogService? log = null)
		{
			Tag = tag;
			_log = log;
			_session = tag.Owner ?? throw new InvalidOperationException("tag has no owning session");
			TagBaseOffset = tag.Raw.MetaLocation?.AsOffset() ?? 0;
			_schema = _session.GetSchema(tag, out var schemaStatus);
			SchemaStatus = schemaStatus;
			Rebuild();
		}

		public TagInfo Tag { get; }
		public long TagBaseOffset { get; }
		public string SchemaStatus { get; }
		public string TagKey => $"{Tag.SourceName}::{Tag.Group}::{Tag.Name}";

		public string HeaderTitle => $"{Tag.Name}.{Tag.Group}";
		public string TabTitle => (IsDirty ? "* " : "") + ShortName(Tag.Name);
		public string SourceLabel => Tag.SourceName;

		public ObservableCollection<MetaRowViewModel> Rows { get; } = new();

		private MetaRowViewModel? _selectedRow;
		public MetaRowViewModel? SelectedRow { get => _selectedRow; set => Set(ref _selectedRow, value); }

		private bool _isDirty;
		public bool IsDirty
		{
			get => _isDirty;
			private set { if (Set(ref _isDirty, value)) Raise(nameof(TabTitle)); }
		}

		public void RecomputeDirty()
		{
			IsDirty = _rowCache.Values.Any(r => r.IsDirty);
		}

		public void ToggleExpand(MetaRowViewModel row)
		{
			if (!row.IsBlock) return;
			row.IsExpanded = !row.IsExpanded;
			Rebuild();
		}

		public void SetElementIndex(MetaRowViewModel row, int index)
		{
			if (!row.IsBlock) return;
			row.ElementIndex = Math.Max(0, Math.Min(index, Math.Max(0, row.ElementCount - 1)));
			Rebuild();
		}

		public void Rebuild()
		{
			var selectedIndex = SelectedRow?.SchemaIndex;
			Rows.Clear();
			if (_schema.Count == 0) return;

			using var reader = _session.Streams.OpenRead();
			Walk(reader, 0, _schema.Count, TagBaseOffset, 0);

			if (selectedIndex.HasValue && _rowCache.TryGetValue(selectedIndex.Value, out var reselect) && Rows.Contains(reselect))
				SelectedRow = reselect;

			RecomputeDirty();
		}

		private void Walk(IReader r, int start, int end, long baseOffset, int depth)
		{
			int i = start;
			while (i < end)
			{
				var def = _schema[i];
				if (def.Kind == MetaFieldKind.TagBlockEnd) { i++; continue; }

				var row = GetOrCreateRow(i, def);
				row.Depth = depth;
				row.AbsoluteOffset = baseOffset + def.Offset;

				if (def.Kind == MetaFieldKind.Comment)
				{
					row.DisplayValue = def.Note ?? "";
					Rows.Add(row);
					i++;
					continue;
				}

				if (def.Kind == MetaFieldKind.TagBlock)
				{
					BlockHeader header = default;
					try { header = MetaValueReader.ReadBlockHeader(r, row.AbsoluteOffset, _session.Cache); }
					catch { /* leave default: Count 0 */ }

					row.ElementCount = header.Count;
					row.ElementsBaseFileOffset = header.BaseFileOffset;
					if (row.ElementIndex >= header.Count) row.ElementIndex = Math.Max(0, header.Count - 1);
					row.DisplayValue = header.Count == 0
						? "empty"
						: header.BaseFileOffset < 0
							? $"{header.Count} entries (pointer unresolved: 0x{header.PointerRaw:X8})"
							: $"{header.Count} entries @ file 0x{header.BaseFileOffset:X6}";
					Rows.Add(row);

					if (row.IsExpanded && def.ChildStart >= 0 && header.Count > 0 && header.BaseFileOffset >= 0)
					{
						long elemBase = header.BaseFileOffset + (long)row.ElementIndex * def.EntrySize;
						Walk(r, def.ChildStart, def.ChildEnd, elemBase, depth + 1);
					}

					i = def.ChildEnd >= 0 ? def.ChildEnd + 1 : i + 1;
					continue;
				}

				// scalar-ish field
				try { row.DisplayValue = MetaValueReader.ReadValue(r, baseOffset, def, _session.Cache); }
				catch (Exception ex) { row.DisplayValue = $"<read error: {ex.GetType().Name}>"; }

				if (def.IsEditable)
					SeedEditStateIfNeeded(row, r, baseOffset, def);

				Rows.Add(row);
				i++;
			}
		}

		private MetaRowViewModel GetOrCreateRow(int schemaIndex, MetaFieldDef def)
		{
			if (_rowCache.TryGetValue(schemaIndex, out var existing)) return existing;
			var row = new MetaRowViewModel(def, schemaIndex);
			_rowCache[schemaIndex] = row;
			return row;
		}

		private void SeedEditStateIfNeeded(MetaRowViewModel row, IReader r, long baseOffset, MetaFieldDef def)
		{
			long abs = baseOffset + def.Offset;
			if (row.Original != null && row.LastSeededOffset == abs)
				return; // same element context, and we're not overwriting live in-flight edits

			var state = MetaValueReader.ReadEditState(r, baseOffset, def);
			row.Original = state;
			row.Current = state.Clone();
			row.LastSeededOffset = abs;
		}

		/// <summary>Writes every dirty, editable row back to the cache file and reloads to confirm the round-trip.</summary>
		public (bool Ok, string Message) Save()
		{
			var dirty = Rows.Where(rr => rr.IsDirty && rr.Def.IsEditable).ToList();
			// Dirty rows outside the currently-visible (expanded) set still live in _rowCache.
			dirty = _rowCache.Values.Where(rr => rr.IsDirty && rr.Def.IsEditable).ToList();

			if (dirty.Count == 0)
				return (true, "Nothing to save.");

			try
			{
				using (var stream = _session.Streams.OpenReadWrite())
				{
					foreach (var row in dirty)
					{
						long contextBase = row.AbsoluteOffset - row.Def.Offset;
						MetaValueWriter.Write(stream, contextBase, row.Def, row.Current!);
					}
				}

				_log?.Info($"saved {dirty.Count} field(s) to \"{Tag.SourceName}\" ({Tag.Name}.{Tag.Group})");
				Rebuild();
				return (true, $"Saved {dirty.Count} field(s).");
			}
			catch (Exception ex)
			{
				_log?.Error($"save failed for {Tag.Name}.{Tag.Group}: {ex.Message}");
				return (false, ex.Message);
			}
		}

		public void RevertAll()
		{
			foreach (var row in _rowCache.Values.Where(rr => rr.IsDirty))
				row.Revert();
			RecomputeDirty();
		}

		/// <summary>Searches this document's cache for existing string IDs containing <paramref name="query"/>.
		/// Real editing only ever offers strings that already exist in the table - adding brand-new
		/// strings would need the table to grow, which is out of scope for this pass.</summary>
		public IEnumerable<string> SearchStringIds(string query, int max)
		{
			var table = _session.Cache.StringIDs;
			if (table == null) yield break;

			int n = 0;
			foreach (var s in table)
			{
				if (n >= max) yield break;
				if (string.IsNullOrWhiteSpace(query) || s.Contains(query, StringComparison.OrdinalIgnoreCase))
				{
					yield return s;
					n++;
				}
			}
		}

		public uint? FindStringId(string text)
		{
			var table = _session.Cache.StringIDs;
			if (table == null) return null;
			var sid = table.FindStringID(text);
			return sid == Blamite.Blam.StringID.Null ? null : sid.Value;
		}

		private static string ShortName(string name)
		{
			var slash = name.LastIndexOf('/');
			return slash < 0 ? name : name[(slash + 1)..];
		}
	}
}
