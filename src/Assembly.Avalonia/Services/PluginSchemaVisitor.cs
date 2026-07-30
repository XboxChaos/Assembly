using System.Collections.Generic;
using Blamite.Blam.Shaders;
using Blamite.Plugins;

namespace Assembly.Avalonia.Services
{
	/// <summary>
	///     Implements Blamite's <see cref="IPluginVisitor" /> to turn a tag-definition XML
	///     into a flat, ordered list of field descriptors.
	///
	///     This is read-only schema extraction: it records what fields exist and where,
	///     but reads no cache data. <see cref="MetaValueReader" /> does that separately.
	///     Keeping the two apart means the schema can be parsed once and reused, and it
	///     avoids duplicating the WPF app's MetaReader (which is tangled with WPF types).
	/// </summary>
	public sealed class PluginSchemaVisitor : IPluginVisitor
	{
		private readonly List<MetaFieldDef> _fields = new();
		private readonly List<(string Name, long Value)> _pending = new();
		private int _depth;

		public IReadOnlyList<MetaFieldDef> Fields => _fields;
		public int BaseSize { get; private set; }
		public List<PluginRevision> Revisions { get; } = new();

		private void Add(MetaFieldKind kind, string name, uint offset, int size = 0,
			string? tooltip = null, string? note = null, List<(string, long)>? choices = null, uint entrySize = 0)
		{
			_fields.Add(new MetaFieldDef
			{
				Kind = kind, Name = name, Offset = offset, Size = size,
				Depth = _depth, Tooltip = string.IsNullOrWhiteSpace(tooltip) ? null : tooltip,
				Note = note, Choices = choices, EntrySize = entrySize
			});
		}

		// ---- plugin lifecycle ----
		public bool EnterPlugin(int baseSize) { BaseSize = baseSize; return true; }
		public void LeavePlugin() { }

		public bool EnterRevisions() => true;
		public void VisitRevision(PluginRevision revision) => Revisions.Add(revision);
		public void LeaveRevisions() { }

		public void VisitComment(string title, string text, uint pluginLine)
			=> Add(MetaFieldKind.Comment, title, 0, note: text);

		// ---- scalars ----
		public void VisitUInt8(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.UInt8, n, o, 1, t);
		}
		public void VisitInt8(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Int8, n, o, 1, t);
		}
		public void VisitUInt16(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.UInt16, n, o, 2, t);
		}
		public void VisitInt16(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Int16, n, o, 2, t);
		}
		public void VisitUInt32(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.UInt32, n, o, 4, t);
		}
		public void VisitInt32(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Int32, n, o, 4, t);
		}
		public void VisitUInt64(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.UInt64, n, o, 8, t);
		}
		public void VisitInt64(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Int64, n, o, 8, t);
		}
		public void VisitFloat32(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Float32, n, o, 4, t);
		}
		public void VisitUndefined(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Undefined, n, o, 4, t);
		}
		public void VisitDatum(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Datum, n, o, 4, t);
		}

		// ---- vectors / angles ----
		public void VisitPoint2(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Point2, n, o, 8, t);
		}
		public void VisitPoint3(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Point3, n, o, 12, t);
		}
		public void VisitVector2(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Vector2, n, o, 8, t);
		}
		public void VisitVector3(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Vector3, n, o, 12, t);
		}
		public void VisitVector4(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Vector4, n, o, 16, t);
		}
		public void VisitDegree(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Degree, n, o, 4, t);
		}
		public void VisitDegree2(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Degree2, n, o, 8, t);
		}
		public void VisitDegree3(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Degree3, n, o, 12, t);
		}
		public void VisitPlane2(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Plane2, n, o, 12, t);
		}
		public void VisitPlane3(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Plane3, n, o, 16, t);
		}
		public void VisitRect16(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Rect16, n, o, 8, t);
		}
		public void VisitQuat16(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Quat16, n, o, 8, t);
		}
		public void VisitPoint16(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Point16, n, o, 4, t);
		}

		// ---- ids / references ----
		public void VisitStringID(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.StringId, n, o, 4, t);
		}
		public void VisitOldStringID(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.OldStringId, n, o, 4, t);
		}

		public void VisitTagReference(string n, uint o, bool v, bool withGroup, uint l, string t)
			=> Add(MetaFieldKind.TagReference, n, o, withGroup ? 16 : 4, t,
				note: withGroup ? "with group" : "index only");

		public void VisitDataReference(string n, uint o, string format, bool v, int align, uint l, string t)
			=> Add(MetaFieldKind.DataReference, n, o, 20, t, note: format);

		// ---- ranges ----
		public void VisitRangeInt16(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.RangeInt16, n, o, 4, t);
		}
		public void VisitRangeFloat32(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.RangeFloat32, n, o, 8, t);
		}
		public void VisitRangeDegree(string n, uint o, bool v, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.RangeDegree, n, o, 8, t);
		}

		// ---- blobs / strings ----
		public void VisitRawData(string n, uint o, bool v, int size, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.RawData, n, o, size, t);
		}
		public void VisitAscii(string n, uint o, bool v, int size, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Ascii, n, o, size, t);
		}
		public void VisitUtf16(string n, uint o, bool v, int size, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.Utf16, n, o, size, t);
		}
		public void VisitHexString(string n, uint o, bool v, int size, uint l, string t)
		{
			if (!v) return;
			Add(MetaFieldKind.HexString, n, o, size, t);
		}

		// ---- colours ----
		public void VisitColorInt(string n, uint o, bool v, bool alpha, uint l, string t)
			=> Add(MetaFieldKind.ColorInt, n, o, 4, t, note: alpha ? "argb" : "rgb");

		public void VisitColorF(string n, uint o, bool v, bool alpha, bool basic, uint l, string t)
			=> Add(MetaFieldKind.ColorF, n, o, alpha ? 16 : 12, t, note: alpha ? "argb float" : "rgb float");

		// ---- flags ----
		public bool EnterFlags8(string n, uint o, bool v, uint l, string t) => BeginChoices(n, o, 1, t);
		public bool EnterFlags16(string n, uint o, bool v, uint l, string t) => BeginChoices(n, o, 2, t);
		public bool EnterFlags32(string n, uint o, bool v, uint l, string t) => BeginChoices(n, o, 4, t);
		public bool EnterFlags64(string n, uint o, bool v, uint l, string t) => BeginChoices(n, o, 8, t);
		public void VisitBit(string name, int index, string tooltip) => _pending.Add((name, 1L << index));
		public void LeaveFlags() => EndChoices(MetaFieldKind.Flags);

		// ---- enums ----
		public bool EnterEnum8(string n, uint o, bool v, uint l, string t) => BeginChoices(n, o, 1, t);
		public bool EnterEnum16(string n, uint o, bool v, uint l, string t) => BeginChoices(n, o, 2, t);
		public bool EnterEnum32(string n, uint o, bool v, uint l, string t) => BeginChoices(n, o, 4, t);
		public void VisitOption(string name, int value, string tooltip) => _pending.Add((name, value));
		public void LeaveEnum() => EndChoices(MetaFieldKind.Enum);

		// ---- tag blocks ----
		private readonly Stack<int> _blockStarts = new();

		public bool EnterTagBlock(string n, uint o, bool v, uint entrySize, int align, bool sort, uint l, string t)
		{
			Add(MetaFieldKind.TagBlock, n, o, 8, t, note: $"entry size 0x{entrySize:X}", entrySize: entrySize);
			_blockStarts.Push(_fields.Count - 1);
			_depth++;
			return true; // descend so nested fields are recorded with their depth
		}

		public void LeaveTagBlock()
		{
			_depth--;
			Add(MetaFieldKind.TagBlockEnd, "", 0);

			if (_blockStarts.Count > 0)
			{
				int blockIndex = _blockStarts.Pop();
				_fields[blockIndex].ChildStart = blockIndex + 1;
				_fields[blockIndex].ChildEnd = _fields.Count - 1; // index of the matching TagBlockEnd
			}
		}

		// ---- misc ----
		public void VisitShader(string n, uint o, bool v, ShaderType type, uint l, string t)
			=> Add(MetaFieldKind.Shader, n, o, 4, t, note: type.ToString());

		public void VisitUnicList(string n, uint o, bool v, int languages, uint l, string t)
			=> Add(MetaFieldKind.UnicList, n, o, 4, t, note: $"{languages} languages");

		// ---- helpers ----
		private string _choiceName = "";
		private uint _choiceOffset;
		private int _choiceSize;
		private string? _choiceTip;

		private bool BeginChoices(string name, uint offset, int size, string tooltip)
		{
			_pending.Clear();
			_choiceName = name;
			_choiceOffset = offset;
			_choiceSize = size;
			_choiceTip = tooltip;
			return true;
		}

		private void EndChoices(MetaFieldKind kind)
		{
			Add(kind, _choiceName, _choiceOffset, _choiceSize, _choiceTip,
				choices: new List<(string, long)>(_pending));
			_pending.Clear();
		}
	}
}
