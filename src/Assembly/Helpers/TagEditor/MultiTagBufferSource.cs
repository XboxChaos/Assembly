using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Helpers.TagEditor
{
	/// <summary>
	/// A <see cref="TagBufferSource"/> which is a collection of other sources.
	/// Each source is associated with a key, and the buffer of the source with the currently-active key is always returned.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	public class MultiTagBufferSource<TKey> : TagBufferSource
	{
		private TKey _activeKey;
		private TagBufferSource _activeSource;
		private Dictionary<TKey, TagBufferSource> _sources = new Dictionary<TKey, TagBufferSource>();

		/// <summary>
		/// Gets or sets the active key.
		/// </summary>
		public TKey ActiveKey
		{
			get { return _activeKey; }
			set
			{
				_activeKey = value;
				_sources.TryGetValue(value, out _activeSource);
				OnActiveBufferChanged();
			}
		}

		/// <summary>
		/// Gets the active source.
		/// </summary>
		public TagBufferSource ActiveSource
		{
			get { return _activeSource; }
		}

		/// <summary>
		/// Gets an enumerable collection of child sources.
		/// </summary>
		public IEnumerable<TagBufferSource> Sources
		{
			get { return _sources.Values; }
		}

		/// <summary>
		/// Gets the currently-active tag buffer.
		/// </summary>
		/// <returns>
		/// The currently-active tag buffer, or <c>null</c> if none.
		/// </returns>
		public override TagBuffer GetActiveBuffer()
		{
			return (_activeSource != null) ? _activeSource.GetActiveBuffer() : null;
		}

		/// <summary>
		/// Associates a source with a key.
		/// </summary>
		/// <param name="key">The key to associate the source with.</param>
		/// <param name="source">The source.</param>
		public void SetSource(TKey key, TagBufferSource source)
		{
			if (source == null)
				throw new ArgumentNullException("Source is null");
			DoRemove(key);
			_sources[key] = source;
			source.ActiveBufferChanged += source_ActiveBufferChanged;
			if (key.Equals(_activeKey))
			{
				_activeSource = source;
				OnActiveBufferChanged();
			}
		}

		/// <summary>
		/// Removes the source associated with a key, if there is one.
		/// </summary>
		/// <param name="key">The key.</param>
		public void RemoveSource(TKey key)
		{
			DoRemove(key);
			if (key.Equals(_activeKey))
			{
				_activeSource = null;
				OnActiveBufferChanged();
			}
		}

		/// <summary>
		/// Removes the source associated with a key, if there is one.
		/// A buffer change will not be signaled.
		/// </summary>
		/// <param name="key">The key.</param>
		private void DoRemove(TKey key)
		{
			TagBufferSource source;
			if (_sources.TryGetValue(key, out source))
			{
				source.ActiveBufferChanged -= source_ActiveBufferChanged;
				_sources.Remove(key);
			}
		}

		private void source_ActiveBufferChanged(object sender, ActiveBufferChangedEventArgs e)
		{
			// Propagate the event if the source is active
			if ((TagBufferSource)sender == _activeSource)
				OnActiveBufferChanged(e);
		}
	}
}
