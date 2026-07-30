using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Assembly.MultiPlatform.ViewModels;

namespace Assembly.MultiPlatform.Services
{
	/// <summary>
	///     A per-document undo/redo stack for field edits made through the properties sidebar.
	///     <see cref="TagDocumentViewModel" /> does not carry one itself - it is being rewritten
	///     elsewhere for performance while this was written, so a stored field there was not an
	///     option. Keying an instance per document in a side table, rather than needing a member
	///     on the view model, is the workaround: a document that closes and is garbage collected
	///     takes its history with it for free (a <see cref="ConditionalWeakTable{TKey,TValue}" />
	///     does not keep the document alive on the history's account, and nothing else here holds
	///     a document reference longer than one push/undo/redo call).
	/// </summary>
	public sealed class EditHistory
	{
		private static readonly ConditionalWeakTable<TagDocumentViewModel, EditHistory> ByDocument = new();

		/// <summary>Gets (creating on first use) the undo/redo stack for one open document.</summary>
		public static EditHistory For(TagDocumentViewModel doc) => ByDocument.GetValue(doc, static _ => new EditHistory());

		private sealed class Entry
		{
			public required MetaRowViewModel Row { get; init; }
			public required FieldEditState Before { get; init; }
			public required FieldEditState After { get; init; }
		}

		private readonly Stack<Entry> _undo = new();
		private readonly Stack<Entry> _redo = new();

		/// <summary>Raised after any push, undo, or redo, so a toolbar can refresh its enabled state and counts.</summary>
		public event Action? Changed;

		public bool CanUndo => _undo.Count > 0;
		public bool CanRedo => _redo.Count > 0;
		public int UndoCount => _undo.Count;
		public int RedoCount => _redo.Count;

		/// <summary>
		///     Records one committed field edit as a single undo step. Called once per discrete
		///     edit gesture (a text box losing focus, a checkbox toggling, an element navigated
		///     to) rather than once per keystroke - <see cref="Views.Editors.FieldEditorContext.Commit" />
		///     is the only caller, and it already filters out no-op edits before reaching here.
		/// </summary>
		public void Push(MetaRowViewModel row, FieldEditState before, FieldEditState after)
		{
			_undo.Push(new Entry { Row = row, Before = before, After = after });
			_redo.Clear();
			Changed?.Invoke();
		}

		public void Undo(TagDocumentViewModel doc)
		{
			if (_undo.Count == 0) return;
			var entry = _undo.Pop();
			entry.Row.Current = entry.Before.Clone();
			entry.Row.NotifyEdited();
			doc.RecomputeDirty();
			_redo.Push(entry);
			Changed?.Invoke();
		}

		public void Redo(TagDocumentViewModel doc)
		{
			if (_redo.Count == 0) return;
			var entry = _redo.Pop();
			entry.Row.Current = entry.After.Clone();
			entry.Row.NotifyEdited();
			doc.RecomputeDirty();
			_undo.Push(entry);
			Changed?.Invoke();
		}
	}
}
