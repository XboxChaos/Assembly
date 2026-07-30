using System;
using Assembly.MultiPlatform.Services;
using Assembly.MultiPlatform.ViewModels;

namespace Assembly.MultiPlatform.Views.Editors
{
	/// <summary>
	///     Everything one field editor needs to talk to its row, its document, and the shared undo
	///     stack, without holding its own reference to the session. <see cref="PropertiesPanel" />
	///     constructs one fresh each time the selected row changes and hands it to the editor it
	///     builds for that row's <see cref="MetaRowViewModel.Editor" /> kind.
	/// </summary>
	public sealed class FieldEditorContext
	{
		public FieldEditorContext(TagDocumentViewModel doc, MetaRowViewModel row)
		{
			Doc = doc;
			Row = row;
			History = EditHistory.For(doc);
		}

		public TagDocumentViewModel Doc { get; }
		public MetaRowViewModel Row { get; }
		public EditHistory History { get; }

		/// <summary>
		///     Snapshots the row's current edit state at the start of a discrete edit gesture (a
		///     text box gaining focus, or immediately before an atomic change like a checkbox
		///     toggle or a combo-box selection). Pass the result back to <see cref="Commit" /> or
		///     <see cref="Cancel" /> once the gesture ends.
		/// </summary>
		public FieldEditState? Snapshot() => Row.Current?.Clone();

		/// <summary>
		///     Ends an edit gesture: if the value actually changed since <paramref name="before" />
		///     was taken, records one undo step and raises the usual dirty-state notifications. A
		///     no-op if nothing changed, so tabbing through a field without touching it does not
		///     pollute the undo stack with an empty step.
		///
		///     Deliberately separate from whatever an editor does on every keystroke (see the
		///     integer/float/text editors' remarks): those call <c>Row.NotifyEdited()</c> and
		///     <c>Doc.RecomputeDirty()</c> directly, live, on every valid keystroke, so a save
		///     triggered mid-edit (without first tabbing away) never loses what was typed. This
		///     method is only about where the undo boundary falls.
		/// </summary>
		public void Commit(FieldEditState? before)
		{
			if (before == null || Row.Current == null) return;
			if (before.ValueEquals(Row.Current)) return;
			History.Push(Row, before, Row.Current.Clone());
			Row.NotifyEdited();
			Doc.RecomputeDirty();
		}

		/// <summary>Escape: throws away whatever changed since <paramref name="before" /> was taken, without creating an undo step.</summary>
		public void Cancel(FieldEditState? before)
		{
			if (before == null) return;
			Row.Current = before.Clone();
			Row.NotifyEdited();
		}

		/// <summary>
		///     Reads this field's raw on-disk bytes fresh, for the hex view and the "copy raw
		///     bytes" action. Only possible for a classic (plugin-XML) tag: a Campaign Evolved
		///     tag's data was parsed once from an in-memory payload with no cache-relative file
		///     offset to seek back to (see <see cref="TagDocumentViewModel" />'s fifth-generation
		///     remarks - <see cref="MetaRowViewModel.AbsoluteOffset" /> means something different
		///     there, a within-struct field offset, not a file position), so this deliberately
		///     returns false rather than fabricate one.
		/// </summary>
		public bool TryReadRawBytes(out byte[] bytes)
		{
			bytes = Array.Empty<byte>();
			if (Doc.IsFifthGen) return false;

			var session = Doc.Tag.Owner;
			if (session == null) return false;

			try
			{
				using var reader = session.Streams.OpenRead();
				bytes = MetaValueReader.ReadRawBytes(reader, Row.AbsoluteOffset, Row.Def.Size);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public string RawBytesUnavailableReason => Doc.IsFifthGen
			? "Campaign Evolved tags have no cache-relative file offset to read raw bytes from - the whole tag was parsed once from its own IoStore payload, not addressed by seeking a stream."
			: "Raw bytes are not available for this field.";
	}
}
