using System.Collections.Generic;

namespace Assembly.Avalonia.Services
{
	/// <summary>
	///     A small bounded most-recently-used list, shared by the command palette's command and
	///     tag search paths to back the "recently-used entries should float" ranking rule. Session
	///     -scoped only (not persisted to disk) - the palette is rebuilt fresh each launch, and
	///     there is no existing settings/preferences service in this project to hang persistence
	///     off without inventing one, which is out of scope for keyboard navigation.
	/// </summary>
	public sealed class RecencyTracker<TKey> where TKey : notnull
	{
		private readonly List<TKey> _order = new();
		private readonly int _capacity;

		public RecencyTracker(int capacity) => _capacity = capacity;

		/// <summary>Records <paramref name="key" /> as just-used, moving it to the front.</summary>
		public void Touch(TKey key)
		{
			_order.Remove(key);
			_order.Insert(0, key);
			if (_order.Count > _capacity) _order.RemoveAt(_order.Count - 1);
		}

		/// <summary>0 = most recently used, increasing = older; -1 = not in the tracked window.</summary>
		public int RankOf(TKey key) => _order.IndexOf(key);

		/// <summary>Most-recent-first snapshot, for an empty-query "recently used" view.</summary>
		public IReadOnlyList<TKey> Recent => _order;
	}
}
