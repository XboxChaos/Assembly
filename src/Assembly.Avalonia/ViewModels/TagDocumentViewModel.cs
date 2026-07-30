using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Assembly.Avalonia.Services;
using Blamite.Blam;
using Blamite.Blam.FifthGen;
using Blamite.Blam.FifthGen.Structures;
using Blamite.IO;
using Blamite.Util;

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

	/// <summary>
	///     What a tag-reference field points at, as read straight out of the field's own bytes -
	///     never resolved eagerly against the mounted namespace, since resolving means searching
	///     every mounted source and the field itself is read far more often than it is followed.
	///     <see cref="MainViewModel.NavigateToTagReference" /> does that search only when the row
	///     is actually activated (double-click, the field table's "Open" glyph, or Enter).
	/// </summary>
	public sealed class TagRefTarget
	{
		public TagRefTarget(string group, string? name, string? sameSourceHint)
		{
			Group = group;
			Name = name;
			SameSourceHint = sameSourceHint;
		}

		/// <summary>The target's tag group four-CC, e.g. "weap". Falls back to "????" when even
		/// the group could not be determined (a classic reference with no inline group magic
		/// pointing at a datum index the owning cache doesn't recognize).</summary>
		public string Group { get; }

		/// <summary>
		///     The target's name/path, or <c>null</c> when the field carries no resolvable name:
		///     an explicit null reference, or (classic only) a datum index that doesn't resolve
		///     against this tag's own cache. <see cref="IsFollowable" /> is exactly this being set.
		/// </summary>
		public string? Name { get; }

		/// <summary>
		///     For a classic reference: the mounted source (file name) the target is expected in,
		///     since a classic datum index only ever resolves within the cache it came from. Tried
		///     first by navigation, before falling back to a namespace-wide search, so two mounted
		///     sources that happen to share a tag name/group can't be confused. Null for a
		///     fifth-generation reference, whose path can legitimately name a tag mounted from a
		///     different sibling container than the one carrying the reference.
		/// </summary>
		public string? SameSourceHint { get; }

		public bool IsFollowable => !string.IsNullOrEmpty(Name);
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

		/// <summary>
		///     Row identity for a fifth-generation document, where there is no fixed flat schema
		///     index to key a row cache by (a struct's field list is only walked as its block/array
		///     is expanded - see <see cref="TagDocumentViewModel.WalkFifthGenStruct"/>). A dotted
		///     chain of field indices from the tag's root struct down to this field. Null for a
		///     classic plugin-XML row, which uses <see cref="SchemaIndex"/> instead.
		/// </summary>
		public string? FifthGenPath { get; init; }

		private int _depth;
		public int Depth { get => _depth; set => Set(ref _depth, value); }

		private long _absoluteOffset;
		public long AbsoluteOffset { get => _absoluteOffset; set { if (Set(ref _absoluteOffset, value)) Raise(nameof(OffsetLabel)); } }

		public string OffsetLabel => $"0x{AbsoluteOffset:X6}";
		public string Name => string.IsNullOrEmpty(Def.Name) ? "-" : Def.Name;

		public string KindLabel => Def.KindLabelOverride
			?? (Def.Kind == MetaFieldKind.TagBlock ? $"TagBlock[{Def.EntrySize:X}]" : Def.KindLabel);

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

		/// <summary>
		///     For a fifth-generation block/array/(singular) struct row: the already-resolved child
		///     element list (see <see cref="TagDocumentViewModel.WalkFifthGenStruct" />), cached on
		///     the row itself so <see cref="TagDocumentViewModel.ToggleExpand" /> and
		///     <see cref="TagDocumentViewModel.SetElementIndex" /> can splice just this row's
		///     subtree back in without re-walking from the document root. Safe to cache indefinitely
		///     because a Campaign Evolved tag's parsed tree never mutates (the format is read-only
		///     in this build) - the same field reached at the same tree position always yields the
		///     same element list. Null for a classic row, or a fifth-generation scalar row.
		/// </summary>
		public IList<FifthGenTagStruct>? FifthGenElements { get; set; }

		/// <summary>
		///     What this row's field points at, when it is a tag reference (classic
		///     <see cref="MetaFieldKind.TagReference" />, or a fifth-generation field of type
		///     <c>TagReference</c>) and the reference isn't explicitly null. Null for every other
		///     row, including a null reference - see <see cref="TagRefTarget.IsFollowable" />.
		/// </summary>
		public TagRefTarget? RefTarget { get; set; }

		public bool IsReference => RefTarget != null;

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

		/// <summary>
		///     Warnings raised while parsing (fifth-generation only - see the constructor) are
		///     buffered here instead of written straight through <see cref="_log" />, because the
		///     constructor itself now commonly runs on a background thread
		///     (<see cref="MainViewModel.OpenTagAsync" />) and <see cref="LogService.Entries" /> is
		///     bound to the console's ListBox - mutating it off the UI thread is exactly the kind of
		///     bug this pass exists to remove, not add one of its own. The caller replays these into
		///     the real log once construction hands back control on its own thread (the UI thread,
		///     for every real caller).
		/// </summary>
		private readonly List<(LogLevel Level, string Message)> _pendingLog = new();
		public IReadOnlyList<(LogLevel Level, string Message)> PendingLogMessages => _pendingLog;

		// ---- fifth-generation (Campaign Evolved) support ----
		//
		// A fifth-generation tag carries its own schema (see FifthGenTagFile.Layout) instead of
		// having one looked up by group magic, and its data is a tree already fully parsed into
		// memory rather than bytes reachable by seeking a stream at baseOffset + Def.Offset. Both
		// of those break the assumptions _schema/_rowCache/Walk are built on, so this tag kind
		// gets its own parallel state and its own walker (WalkFifthGenStruct) below, feeding the
		// exact same Rows/MetaRowViewModel the classic path does so the table, tabs and properties
		// sidebar need no changes to render it.
		private readonly bool _isFifthGen;
		private readonly FifthGenTagFile? _fifthGenFile;
		private readonly Dictionary<(int StructIndex, int FieldIndex), MetaFieldDef> _fifthGenDefCache = new();
		private readonly Dictionary<int, MetaFieldDef> _fifthGenTrailingDefCache = new();
		private readonly Dictionary<string, MetaRowViewModel> _fifthGenRowCache = new();
		private int _fifthGenRowCounter;

		/// <summary>Whether this document is a self-describing fifth-generation tag rather than a classic plugin-XML one.</summary>
		public bool IsFifthGen => _isFifthGen;

		public TagDocumentViewModel(TagInfo tag, LogService? log = null)
		{
			Tag = tag;
			_log = log;
			_session = tag.Owner ?? throw new InvalidOperationException("tag has no owning session");

			if (tag.Raw is FifthGenTag fgTag)
			{
				_isFifthGen = true;
				TagBaseOffset = 0; // no cache-relative address space; RawPayload is the tag's whole file
				_schema = Array.Empty<MetaFieldDef>();

				if (!FifthGenTagFile.IsTagPayload(fgTag.RawPayload))
				{
					SchemaStatus = "This does not look like a Campaign Evolved tag payload.";
				}
				else
				{
					try
					{
						_fifthGenFile = new FifthGenTagFile(fgTag.RawPayload);
						FifthGenTagLayout layout = _fifthGenFile.Layout;
						SchemaStatus = $"{layout.Fields.Count} fields / {layout.Structs.Count} structs / " +
						               $"{layout.Enums.Count} enums / {layout.Blocks.Count} blocks  (self-describing, Campaign Evolved)";

						// The payload's own parser is the authority on what it could and could not
						// make sense of - surface that honestly rather than silently dropping it.
						foreach (string w in _fifthGenFile.Warnings)
							_pendingLog.Add((LogLevel.Warn, $"{Tag.Name}.{Tag.Group}: {w}"));

						int rootCount = _fifthGenFile.Data.Elements.Count;
						if (rootCount != 1)
						{
							_pendingLog.Add((LogLevel.Warn, $"{Tag.Name}.{Tag.Group}: tag root block has {rootCount} element(s) (expected 1); " +
							           (rootCount == 0 ? "the field table will be empty." : "showing element 0.")));
						}
					}
					catch (Exception ex)
					{
						SchemaStatus = $"Failed to parse Campaign Evolved tag: {ex.Message}";
						_pendingLog.Add((LogLevel.Error, $"{Tag.Name}.{Tag.Group}: {ex.Message}"));
					}
				}
			}
			else
			{
				TagBaseOffset = tag.Raw.MetaLocation?.AsOffset() ?? 0;
				_schema = _session.GetSchema(tag, out var schemaStatus);
				SchemaStatus = schemaStatus;
			}

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

		// ================= field search / filter =================
		//
		// Decision: the filter searches unexpanded subtrees too, not just what's currently on
		// screen. On a 1806-field scenario tag, most fields live inside a block nobody has expanded
		// yet, so a filter that only matched visible rows would be close to useless for exactly the
		// tag this feature exists for. That is affordable here because a Campaign Evolved tag's
		// whole tree is already parsed into memory (FifthGenTagFile.Data) - descending into every
		// field costs CPU only, no disk I/O, however deep the tag goes. For a classic (plugin-XML)
		// tag, the same "always descend" approach still costs one MetaValueReader read per field in
		// the flattened schema (bounded by the schema's total size, not multiplied by block element
		// counts, since - like the rest of this UI - only one representative element per block is
		// ever inspected). There is no classic (.map) fixture in this repo big enough to measure
		// that cost against, so this is the one place in this file whose performance is reasoned
		// about rather than proven; if a future large classic mount makes it visibly slow, it should
		// get the same debounce/async treatment tag-opening got rather than being trusted blind.

		private string _filterQuery = "";
		public string FilterQuery
		{
			get => _filterQuery;
			set
			{
				value ??= "";
				if (_filterQuery == value) return;
				_filterQuery = value;
				Raise();
				Raise(nameof(IsFiltering));
				if (IsFiltering) RebuildFilteredNow();
				else Rebuild();
			}
		}

		public bool IsFiltering => _filterQuery.Trim().Length > 0;

		private int _filterMatchCount;
		public int FilterMatchCount { get => _filterMatchCount; private set => Set(ref _filterMatchCount, value); }

		/// <summary>Accumulates leaf-field matches during a filtered walk (see <see cref="Matches"/>
		/// call sites in <see cref="WalkFiltered"/>/<see cref="WalkFifthGenFiltered"/>) - reset at
		/// the start of <see cref="RebuildFilteredNow"/> and read back into <see cref="FilterMatchCount"/>
		/// once the walk completes. A field accumulator rather than a returned count because both
		/// walkers are recursive and every recursive call needs to add to the same running total.</summary>
		private int _filterMatchAccumulator;

		private void RebuildFilteredNow()
		{
			string ql = _filterQuery.Trim();
			var selectedIndex = SelectedRow?.SchemaIndex;
			string? selectedPath = SelectedRow?.FifthGenPath;

			Rows.Clear();
			_filterMatchAccumulator = 0;
			var target = new List<MetaRowViewModel>();

			if (_isFifthGen)
			{
				if (_fifthGenFile != null && _fifthGenFile.Data.Elements.Count > 0)
					WalkFifthGenFiltered(_fifthGenFile.Data.Elements[0], 0, "", ql, target);
			}
			else if (_schema.Count > 0)
			{
				using var reader = _session.Streams.OpenRead();
				WalkFiltered(reader, 0, _schema.Count, TagBaseOffset, 0, ql, target);
			}

			foreach (var row in target) Rows.Add(row);
			FilterMatchCount = _filterMatchAccumulator;

			if (_isFifthGen)
			{
				if (selectedPath != null && _fifthGenRowCache.TryGetValue(selectedPath, out var reselect) && Rows.Contains(reselect))
					SelectedRow = reselect;
			}
			else if (selectedIndex.HasValue && _rowCache.TryGetValue(selectedIndex.Value, out var reselectClassic) && Rows.Contains(reselectClassic))
			{
				SelectedRow = reselectClassic;
			}
		}

		private static bool Matches(string ql, params string?[] fields)
		{
			foreach (var f in fields)
				if (f != null && f.Contains(ql, StringComparison.OrdinalIgnoreCase))
					return true;
			return false;
		}

		// ================= expand / collapse / element navigation =================
		//
		// These used to call Rebuild() - Rows.Clear() plus a full re-walk of the document - for
		// literally any change, which is a full teardown/rebuild of the ListBox's visual tree on
		// every click and throws away scroll position and selection (see the reselect-by-identity
		// hack in Rebuild()/RebuildFifthGen()). What actually changed is always one row's own
		// subtree, so the fix is to compute just that subtree and splice it into Rows in place:
		// ObservableCollection.Insert/RemoveAt raise Add/Remove, which Avalonia's virtualizing
		// panel and the ListBox's selection both handle incrementally, unlike a Reset (which is
		// what Clear() raises and is exactly as disruptive as it sounds).

		public void ToggleExpand(MetaRowViewModel row)
		{
			if (!row.IsBlock) return;

			// A filtered view's shape is driven entirely by which rows currently match, not by
			// IsExpanded (see WalkFiltered/WalkFifthGenFiltered) - splicing a subtree into it here
			// would assume positions the filtered walk never guaranteed. Flip the flag so it takes
			// effect the moment the filter is cleared, but leave Rows alone until then.
			if (IsFiltering) { row.IsExpanded = !row.IsExpanded; return; }

			int pos = Rows.IndexOf(row);
			if (pos < 0) { row.IsExpanded = !row.IsExpanded; return; } // defensive: row not currently visible

			if (row.IsExpanded)
			{
				RemoveSubtreeAfter(pos, row.Depth);
				row.IsExpanded = false;
				return;
			}

			row.IsExpanded = true;
			ExpandChildrenInPlace(row, pos);
		}

		public void SetElementIndex(MetaRowViewModel row, int index)
		{
			if (!row.IsBlock) return;
			int clamped = Math.Max(0, Math.Min(index, Math.Max(0, row.ElementCount - 1)));
			if (clamped == row.ElementIndex) return;
			row.ElementIndex = clamped; // updates ElementSummary via its own PropertyChanged regardless of visibility

			if (IsFiltering) { RebuildFilteredNow(); return; } // the representative element the filtered view descends into just changed
			if (!row.IsExpanded) return; // nothing visible depends on which element is selected while collapsed

			int pos = Rows.IndexOf(row);
			if (pos < 0) return;
			RemoveSubtreeAfter(pos, row.Depth);
			ExpandChildrenInPlace(row, pos);
		}

		/// <summary>Removes every row after <paramref name="pos" /> whose depth is greater than
		/// <paramref name="parentDepth" /> - i.e. exactly the subtree rooted at <paramref name="pos" />,
		/// however deeply nested (a previously-expanded grandchild included), and nothing else.</summary>
		private void RemoveSubtreeAfter(int pos, int parentDepth)
		{
			while (pos + 1 < Rows.Count && Rows[pos + 1].Depth > parentDepth)
				Rows.RemoveAt(pos + 1);
		}

		private void InsertSubtreeAt(int pos, IReadOnlyList<MetaRowViewModel> children)
		{
			for (int i = 0; i < children.Count; i++)
				Rows.Insert(pos + 1 + i, children[i]);
		}

		private void ExpandChildrenInPlace(MetaRowViewModel row, int pos)
		{
			if (_isFifthGen) ExpandFifthGenChildrenInPlace(row, pos);
			else ExpandClassicChildrenInPlace(row, pos);
		}

		private void ExpandClassicChildrenInPlace(MetaRowViewModel row, int pos)
		{
			if (row.Def.ChildStart < 0 || row.ElementCount == 0 || row.ElementsBaseFileOffset < 0) return;

			using var reader = _session.Streams.OpenRead();
			long elemBase = row.ElementsBaseFileOffset + (long)row.ElementIndex * row.Def.EntrySize;
			var children = new List<MetaRowViewModel>();
			Walk(reader, row.Def.ChildStart, row.Def.ChildEnd, elemBase, row.Depth + 1, children);
			InsertSubtreeAt(pos, children);
		}

		public void Rebuild()
		{
			if (_isFifthGen) { RebuildFifthGen(); return; }
			if (IsFiltering) { RebuildFilteredNow(); return; }

			// Only reachable from the constructor and from Save()'s post-write reload now that
			// expand/collapse/element-index no longer call this - both are full-document, low
			// frequency operations where re-seeding the selection by identity is still doing real
			// work (unlike the incremental paths above, where the selected row object never leaves
			// Rows in the first place unless its own ancestor was just collapsed).
			var selectedIndex = SelectedRow?.SchemaIndex;
			Rows.Clear();
			if (_schema.Count == 0) return;

			using var reader = _session.Streams.OpenRead();
			Walk(reader, 0, _schema.Count, TagBaseOffset, 0, Rows);

			if (selectedIndex.HasValue && _rowCache.TryGetValue(selectedIndex.Value, out var reselect) && Rows.Contains(reselect))
				SelectedRow = reselect;

			RecomputeDirty();
		}

		private void Walk(IReader r, int start, int end, long baseOffset, int depth, IList<MetaRowViewModel> target)
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
					target.Add(row);
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
					target.Add(row);

					if (row.IsExpanded && def.ChildStart >= 0 && header.Count > 0 && header.BaseFileOffset >= 0)
					{
						long elemBase = header.BaseFileOffset + (long)row.ElementIndex * def.EntrySize;
						Walk(r, def.ChildStart, def.ChildEnd, elemBase, depth + 1, target);
					}

					i = def.ChildEnd >= 0 ? def.ChildEnd + 1 : i + 1;
					continue;
				}

				// scalar-ish field
				try { row.DisplayValue = MetaValueReader.ReadValue(r, baseOffset, def, _session.Cache); }
				catch (Exception ex) { row.DisplayValue = $"<read error: {ex.GetType().Name}>"; }

				row.RefTarget = def.Kind == MetaFieldKind.TagReference ? TryResolveClassicTagRef(r, baseOffset, def) : null;

				if (def.IsEditable)
					SeedEditStateIfNeeded(row, r, baseOffset, def);

				target.Add(row);
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

		/// <summary>
		///     Resolves a classic tag-reference field's target straight off the field's own bytes,
		///     independent of and in addition to <see cref="MetaValueReader.ReadValue" />'s display
		///     string (both seek to the same offset, so calling this after is safe - see
		///     <see cref="IReader.SeekTo" />). Mirrors the 16-byte "four-CC + two runtime words +
		///     datum index" / 4-byte "datum index only" layouts <c>MetaValueReader</c>'s own
		///     TagReference case reads, because the byte layout is the one thing here that isn't a
		///     choice; what's added is preferring the *referenced tag's own* group over the inline
		///     four-CC when the datum index resolves, since a stale inline four-CC (the runtime
		///     patches it in, not the cache author) would otherwise send navigation looking in the
		///     wrong tag group.
		/// </summary>
		private TagRefTarget? TryResolveClassicTagRef(IReader r, long baseOffset, MetaFieldDef def)
		{
			long at = baseOffset + def.Offset;
			if (at < 0 || at >= r.Length) return null;
			r.SeekTo(at);

			int inlineGroupMagic = 0;
			if (def.Size == 16)
			{
				inlineGroupMagic = r.ReadInt32();
				r.Skip(8);
			}

			DatumIndex di = DatumIndex.ReadFrom(r);
			if (!di.IsValid) return null; // an explicit null reference - nothing to follow, not a broken one

			ICacheFile cache = _session.Cache;
			string group = inlineGroupMagic != 0 ? CharConstant.ToString(inlineGroupMagic) : "";
			string? name = null;

			try
			{
				if (cache.Tags.IsValidIndex(di))
				{
					ITag target = cache.Tags[di];
					if (target?.Group != null) group = CharConstant.ToString(target.Group.Magic);
					if (cache.FileNames != null) name = cache.FileNames.GetTagName(di);
				}
			}
			catch { /* damaged/partial cache - same as the read-only value column, leave it unresolved rather than throw */ }

			// A classic datum index only ever resolves within the cache it came from.
			return new TagRefTarget(string.IsNullOrEmpty(group) ? "????" : group, name, Tag.SourceName);
		}

		/// <summary>
		///     Filtered counterpart to <see cref="Walk"/>: unlike the normal walk, this always
		///     descends into a tag block's one representative element (see the "field search /
		///     filter" remarks above for why that is an acceptable cost here) regardless of
		///     <see cref="MetaRowViewModel.IsExpanded"/>, and only adds a row when it - or something
		///     inside it - actually matches. Returns whether it added anything, so a block knows
		///     whether to include itself purely as match context for a descendant.
		/// </summary>
		private bool WalkFiltered(IReader r, int start, int end, long baseOffset, int depth, string ql, List<MetaRowViewModel> target)
		{
			int i = start;
			bool any = false;
			while (i < end)
			{
				var def = _schema[i];
				if (def.Kind == MetaFieldKind.TagBlockEnd) { i++; continue; }

				if (def.Kind == MetaFieldKind.Comment)
				{
					if (Matches(ql, def.Name, def.KindLabel, def.Note))
					{
						var crow = GetOrCreateRow(i, def);
						crow.Depth = depth;
						crow.AbsoluteOffset = baseOffset + def.Offset;
						crow.DisplayValue = def.Note ?? "";
						target.Add(crow);
						any = true;
						_filterMatchAccumulator++;
					}
					i++;
					continue;
				}

				if (def.Kind == MetaFieldKind.TagBlock)
				{
					var row = GetOrCreateRow(i, def);
					row.Depth = depth;
					row.AbsoluteOffset = baseOffset + def.Offset;

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

					bool selfMatch = Matches(ql, row.Name, row.KindLabel, row.DisplayValue);
					var childRows = new List<MetaRowViewModel>();
					bool childMatch = false;
					if (def.ChildStart >= 0 && header.Count > 0 && header.BaseFileOffset >= 0)
					{
						long elemBase = header.BaseFileOffset + (long)row.ElementIndex * def.EntrySize;
						childMatch = WalkFiltered(r, def.ChildStart, def.ChildEnd, elemBase, depth + 1, ql, childRows);
					}

					if (selfMatch || childMatch)
					{
						target.Add(row);
						target.AddRange(childRows);
						any = true;
						if (selfMatch) _filterMatchAccumulator++;
					}

					i = def.ChildEnd >= 0 ? def.ChildEnd + 1 : i + 1;
					continue;
				}

				// scalar-ish field
				{
					var row = GetOrCreateRow(i, def);
					row.Depth = depth;
					row.AbsoluteOffset = baseOffset + def.Offset;
					try { row.DisplayValue = MetaValueReader.ReadValue(r, baseOffset, def, _session.Cache); }
					catch (Exception ex) { row.DisplayValue = $"<read error: {ex.GetType().Name}>"; }

					if (Matches(ql, row.Name, row.KindLabel, row.DisplayValue))
					{
						row.RefTarget = def.Kind == MetaFieldKind.TagReference ? TryResolveClassicTagRef(r, baseOffset, def) : null;
						if (def.IsEditable) SeedEditStateIfNeeded(row, r, baseOffset, def);
						target.Add(row);
						any = true;
						_filterMatchAccumulator++;
					}
					i++;
				}
			}
			return any;
		}

		// ================= fifth-generation (Campaign Evolved) walker =================
		//
		// FifthGenTagFile parses a whole tag's schema *and* data eagerly: FifthGenTagFile.Data is
		// already a complete tree of FifthGenTagStruct/FifthGenTagValue objects by the time the
		// constructor returns - there is nothing left to read lazily at the Blamite layer, and
		// nothing here re-parses any bytes.
		//
		// What *is* kept lazy is the projection of that tree into MetaRowViewModel rows: this only
		// walks a struct's own field list, and only descends into a block/array/struct field's
		// element(s) when the corresponding row is expanded (row.IsExpanded), exactly mirroring how
		// the classic Walk() above only recurses into an expanded tag block. That matters here for
		// two reasons rather than one: it is what keeps the table responsive for a tag the size of
		// the scenario (1800+ fields across 270+ structs), and - unlike classic plugin XML, whose
		// block element schema is written inline in the XML and so is necessarily tree-shaped - a
		// fifth-generation struct is addressed by index into a shared table and a block's element
		// struct can legitimately be the struct that contains the block (Blamite's own
		// FifthGenTagLayout only rules this out for *inline* struct/array nesting, where it would
		// make a fixed byte size impossible to compute; a block's elements live in their own
		// variable-length section, so nothing stops one from nesting itself). Eagerly flattening
		// the whole schema up front, the way PluginSchemaVisitor can for classic plugins, would risk
		// an infinite walk for exactly that shape. Only ever descending on-demand, bounded by
		// row.IsExpanded, is what makes that safe.
		//
		// A row's identity therefore can't be a flat schema index either (see MetaRowViewModel.
		// FifthGenPath's remarks) - it is the dotted chain of field indices from the tag's root
		// struct down to this field, which is stable across re-expansion and across navigating a
		// block/array's ElementIndex (all elements of one block share the same struct, hence the
		// same field list and the same offsets), but distinct for the same struct reused at two
		// different positions in the tree.

		private void RebuildFifthGen()
		{
			if (IsFiltering) { RebuildFilteredNow(); return; }

			string? selectedPath = SelectedRow?.FifthGenPath;
			Rows.Clear();
			if (_fifthGenFile == null) return;

			FifthGenTagBlock root = _fifthGenFile.Data;
			if (root.Elements.Count == 0) return;

			WalkFifthGenStruct(root.Elements[0], 0, "", Rows);

			if (selectedPath != null && _fifthGenRowCache.TryGetValue(selectedPath, out var reselect) && Rows.Contains(reselect))
				SelectedRow = reselect;

			RecomputeDirty();
		}

		private void ExpandFifthGenChildrenInPlace(MetaRowViewModel row, int pos)
		{
			IList<FifthGenTagStruct>? elements = row.FifthGenElements;
			if (elements == null || elements.Count == 0 || row.FifthGenPath == null) return;

			int idx = Math.Min(row.ElementIndex, elements.Count - 1);
			var children = new List<MetaRowViewModel>();
			WalkFifthGenStruct(elements[idx], row.Depth + 1, row.FifthGenPath, children);
			InsertSubtreeAt(pos, children);
		}

		/// <summary>
		///     Projects one struct instance's fields into rows: a leaf row per scalar field, and an
		///     expandable <see cref="MetaFieldKind.TagBlock"/> row per block/array/(singular) struct
		///     field, recursing into whichever element <see cref="MetaRowViewModel.ElementIndex"/>
		///     currently selects only when that row is expanded.
		/// </summary>
		private void WalkFifthGenStruct(FifthGenTagStruct instance, int depth, string pathPrefix, IList<MetaRowViewModel> target)
		{
			IList<FifthGenFieldDefinition> fields = instance.Definition.Fields;
			IList<FifthGenTagValue> values = instance.Values;
			int structIndex = instance.Definition.Index;
			uint offset = 0;

			for (var i = 0; i < fields.Count; i++)
			{
				FifthGenFieldDefinition field = fields[i];
				FifthGenTagValue value = values[i];
				uint fieldOffset = offset;
				offset += (uint)Math.Max(0, field.InlineSize);

				// Padding contributes no name and no meaning; classic plugins never surface it
				// as a row either (it is simply absent from the XML).
				if (field.Type == FifthGenFieldType.Pad)
					continue;

				string path = $"{pathPrefix}.{i}";

				if (field.Type == FifthGenFieldType.Block || field.Type == FifthGenFieldType.Array ||
				    field.Type == FifthGenFieldType.Struct)
				{
					MetaFieldDef cdef = GetFifthGenContainerDef(structIndex, i, field, fieldOffset);
					MetaRowViewModel row = GetOrCreateFifthGenRow(path, cdef);
					row.Depth = depth;
					row.AbsoluteOffset = fieldOffset;

					IList<FifthGenTagStruct> elements = GetFifthGenElements(value);
					row.FifthGenElements = elements;
					row.ElementCount = elements.Count;
					row.ElementsBaseFileOffset = -1; // no file-relative address space for this engine
					if (row.ElementIndex >= elements.Count) row.ElementIndex = Math.Max(0, elements.Count - 1);
					row.DisplayValue = DescribeFifthGenContainer(field, elements.Count, value);
					target.Add(row);

					if (row.IsExpanded && elements.Count > 0)
						WalkFifthGenStruct(elements[row.ElementIndex], depth + 1, path, target);

					continue;
				}

				MetaFieldDef sdef = GetFifthGenScalarDef(structIndex, i, field, fieldOffset);
				MetaRowViewModel srow = GetOrCreateFifthGenRow(path, sdef);
				srow.Depth = depth;
				srow.AbsoluteOffset = fieldOffset;
				try { srow.DisplayValue = FifthGenValueFormatter.Format(value); }
				catch (Exception ex) { srow.DisplayValue = $"<format error: {ex.GetType().Name}>"; }
				srow.RefTarget = ResolveFifthGenTagRef(value);
				target.Add(srow);
			}

			// A struct instance can carry more sections than its own field list accounts for -
			// see FifthGenTagStruct.TrailingSections' remarks. That is worth a visible line, not
			// just a warning that scrolled off the console two tags ago.
			if (instance.TrailingSections.Count > 0)
			{
				MetaFieldDef tdef = GetFifthGenTrailingDef(structIndex);
				MetaRowViewModel trow = GetOrCreateFifthGenRow(pathPrefix + ".$trailing", tdef);
				trow.Depth = depth;
				trow.AbsoluteOffset = offset;
				trow.DisplayValue = $"{instance.TrailingSections.Count} undecoded trailing section(s) - see console";
				target.Add(trow);
			}
		}

		/// <summary>A fifth-generation reference field's target travels with the field itself (see
		/// <see cref="FifthGenTagReferenceValue"/>'s remarks) - no read, seek or cache lookup
		/// needed, unlike the classic path.</summary>
		private static TagRefTarget? ResolveFifthGenTagRef(FifthGenTagValue value) =>
			value is FifthGenTagReferenceValue tr && !tr.IsNull ? new TagRefTarget(tr.GroupTag, tr.Path, null) : null;

		/// <summary>
		///     Filtered counterpart to <see cref="WalkFifthGenStruct"/>: always descends into every
		///     block/array/struct field's currently-selected representative element - there is no
		///     I/O cost to doing so here, the whole tree is already in memory - and only adds a row
		///     when it, or something inside it, matches. Returns whether it added anything, exactly
		///     like <see cref="WalkFiltered"/> does for the classic path.
		/// </summary>
		private bool WalkFifthGenFiltered(FifthGenTagStruct instance, int depth, string pathPrefix, string ql, List<MetaRowViewModel> target)
		{
			IList<FifthGenFieldDefinition> fields = instance.Definition.Fields;
			IList<FifthGenTagValue> values = instance.Values;
			int structIndex = instance.Definition.Index;
			uint offset = 0;
			bool any = false;

			for (var i = 0; i < fields.Count; i++)
			{
				FifthGenFieldDefinition field = fields[i];
				FifthGenTagValue value = values[i];
				uint fieldOffset = offset;
				offset += (uint)Math.Max(0, field.InlineSize);

				if (field.Type == FifthGenFieldType.Pad)
					continue;

				string path = $"{pathPrefix}.{i}";

				if (field.Type == FifthGenFieldType.Block || field.Type == FifthGenFieldType.Array ||
				    field.Type == FifthGenFieldType.Struct)
				{
					MetaFieldDef cdef = GetFifthGenContainerDef(structIndex, i, field, fieldOffset);
					bool selfMatch = Matches(ql, field.Name, cdef.KindLabelOverride);

					IList<FifthGenTagStruct> elements = GetFifthGenElements(value);
					var childRows = new List<MetaRowViewModel>();
					bool childMatch = false;
					if (elements.Count > 0)
					{
						MetaRowViewModel probe = GetOrCreateFifthGenRow(path, cdef);
						int idx = Math.Min(probe.ElementIndex, elements.Count - 1);
						childMatch = WalkFifthGenFiltered(elements[idx], depth + 1, path, ql, childRows);
					}

					if (selfMatch || childMatch)
					{
						MetaRowViewModel row = GetOrCreateFifthGenRow(path, cdef);
						row.Depth = depth;
						row.AbsoluteOffset = fieldOffset;
						row.FifthGenElements = elements;
						row.ElementCount = elements.Count;
						row.ElementsBaseFileOffset = -1;
						if (row.ElementIndex >= elements.Count) row.ElementIndex = Math.Max(0, elements.Count - 1);
						row.DisplayValue = DescribeFifthGenContainer(field, elements.Count, value);
						target.Add(row);
						target.AddRange(childRows);
						any = true;
						if (selfMatch) _filterMatchAccumulator++;
					}

					continue;
				}

				MetaFieldDef sdef = GetFifthGenScalarDef(structIndex, i, field, fieldOffset);
				string display;
				try { display = FifthGenValueFormatter.Format(value); }
				catch (Exception ex) { display = $"<format error: {ex.GetType().Name}>"; }

				if (Matches(ql, field.Name, sdef.KindLabelOverride, display))
				{
					MetaRowViewModel srow = GetOrCreateFifthGenRow(path, sdef);
					srow.Depth = depth;
					srow.AbsoluteOffset = fieldOffset;
					srow.DisplayValue = display;
					srow.RefTarget = ResolveFifthGenTagRef(value);
					target.Add(srow);
					any = true;
					_filterMatchAccumulator++;
				}
			}

			if (instance.TrailingSections.Count > 0 && Matches(ql, "(undecoded)"))
			{
				MetaFieldDef tdef = GetFifthGenTrailingDef(structIndex);
				MetaRowViewModel trow = GetOrCreateFifthGenRow(pathPrefix + ".$trailing", tdef);
				trow.Depth = depth;
				trow.AbsoluteOffset = offset;
				trow.DisplayValue = $"{instance.TrailingSections.Count} undecoded trailing section(s) - see console";
				target.Add(trow);
				any = true;
				_filterMatchAccumulator++;
			}

			return any;
		}

		private static IList<FifthGenTagStruct> GetFifthGenElements(FifthGenTagValue value) => value switch
		{
			FifthGenBlockValue b => b.Value?.Elements ?? Array.Empty<FifthGenTagStruct>(),
			FifthGenArrayValue a => a.Elements,
			FifthGenStructValue s => new[] { s.Value },
			_ => Array.Empty<FifthGenTagStruct>()
		};

		private static string DescribeFifthGenContainer(FifthGenFieldDefinition field, int count, FifthGenTagValue value)
		{
			switch (field.Type)
			{
				case FifthGenFieldType.Block:
					uint declared = (value as FifthGenBlockValue)?.Value?.DeclaredCount ?? (uint)count;
					return count == 0
						? "empty"
						: declared == count ? $"{count} entries" : $"{count} entries (block declares {declared})";
				case FifthGenFieldType.Array:
					return count == 0 ? "empty" : $"{count} entries (fixed-size)";
				default: // Struct: always exactly one, inlined instance
					return "(struct)";
			}
		}

		/// <summary>Builds (and caches) the row schema for a block/array/struct field. Cached by
		/// (struct, field) rather than by tree position: every instance of one struct shares the
		/// same field list, same types and same offsets, so the definition itself never varies by
		/// where in the tree it is reached from - only the row (see <see cref="GetOrCreateFifthGenRow"/>) does.</summary>
		private MetaFieldDef GetFifthGenContainerDef(int structIndex, int fieldIndex, FifthGenFieldDefinition field, uint offset)
		{
			var key = (structIndex, fieldIndex);
			if (_fifthGenDefCache.TryGetValue(key, out var cached)) return cached;

			string shapeLabel = field.Type switch
			{
				FifthGenFieldType.Block => "block",
				FifthGenFieldType.Array => $"array[{field.Array?.Count ?? 0}]",
				_ => "struct"
			};

			int elementSize = field.Type switch
			{
				FifthGenFieldType.Block => field.Block?.Struct.InlineSize ?? 0,
				FifthGenFieldType.Array => field.Array?.Struct.InlineSize ?? 0,
				_ => field.Struct?.InlineSize ?? 0
			};

			var def = new MetaFieldDef
			{
				Kind = MetaFieldKind.TagBlock,
				Name = field.Name,
				Offset = offset,
				Size = Math.Max(0, field.InlineSize),
				KindLabelOverride = shapeLabel,
				EntrySize = (uint)Math.Max(0, elementSize)
			};
			_fifthGenDefCache[key] = def;
			return def;
		}

		private MetaFieldDef GetFifthGenScalarDef(int structIndex, int fieldIndex, FifthGenFieldDefinition field, uint offset)
		{
			var key = (structIndex, fieldIndex);
			if (_fifthGenDefCache.TryGetValue(key, out var cached)) return cached;

			var def = new MetaFieldDef
			{
				Kind = MetaFieldKind.FifthGenValue,
				Name = field.Name,
				Offset = offset,
				Size = Math.Max(0, field.InlineSize),
				KindLabelOverride = field.TypeName ?? field.Type.ToString(),
				Tooltip = field.Enum != null ? $"Options: {string.Join(", ", field.Enum.Options)}" : null
			};
			_fifthGenDefCache[key] = def;
			return def;
		}

		private MetaFieldDef GetFifthGenTrailingDef(int structIndex)
		{
			if (_fifthGenTrailingDefCache.TryGetValue(structIndex, out var cached)) return cached;
			var def = new MetaFieldDef { Kind = MetaFieldKind.Comment, Name = "(undecoded)" };
			_fifthGenTrailingDefCache[structIndex] = def;
			return def;
		}

		private MetaRowViewModel GetOrCreateFifthGenRow(string path, MetaFieldDef def)
		{
			if (_fifthGenRowCache.TryGetValue(path, out var existing)) return existing;
			var row = new MetaRowViewModel(def, _fifthGenRowCounter++) { FifthGenPath = path };
			_fifthGenRowCache[path] = row;
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
			// Original/Current are plain properties (not themselves observable), so nothing
			// else raises IsDirty's PropertyChanged when they're (re)seeded - e.g. right after a
			// Save() reloads from disk and every row should visually go clean again.
			row.NotifyEdited();
		}

		/// <summary>Writes every dirty, editable row back to the cache file and reloads to confirm the round-trip.</summary>
		public (bool Ok, string Message) Save()
		{
			// No FifthGenValue/TagBlock row for a Campaign Evolved tag is ever seeded with edit
			// state (see WalkFifthGenStruct), so _rowCache - which this method otherwise reads from
			// - stays empty for one and this would already fall through to "Nothing to save."
			// unaided. The explicit guard exists so that stays true on purpose rather than by
			// accident: FifthGenCacheFile.SaveChanges throws NotSupportedException by design (a
			// fifth-generation tag is a whole cooked IoStore chunk with no in-place write path this
			// codebase implements), and a future edit path added to WalkFifthGenStruct without
			// touching this guard would otherwise silently attempt - and fail - a save through
			// _session.Streams, which isn't even the right stream for this tag's bytes.
			if (_isFifthGen)
				return (false, "Campaign Evolved tags are read-only in this build: FifthGenCacheFile.SaveChanges does not support writing them back.");

			// Dirty rows outside the currently-expanded block set still live in _rowCache even
			// though they're not in the visible Rows collection right now.
			var dirty = _rowCache.Values.Where(rr => rr.IsDirty && rr.Def.IsEditable).ToList();

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

				// Promote what we just wrote to the new "clean" baseline directly. Rebuild()'s
				// reseed-from-disk is keyed on a row's context offset changing (see
				// SeedEditStateIfNeeded) - which intentionally does NOT happen for an ordinary
				// top-level field across a save, so it must not be the only thing clearing
				// dirty state, or every plain field would show "modified" forever after saving.
				foreach (var row in dirty)
				{
					row.Original = row.Current!.Clone();
					row.NotifyEdited();
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
