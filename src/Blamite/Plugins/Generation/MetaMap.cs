using System.Collections.Generic;

namespace Blamite.Plugins.Generation
{
	public class MetaMap
	{
		private readonly HashSet<MetaMap> _ancestors = new HashSet<MetaMap>();
		private readonly SortedList<int, MetaValueGuess> _guesses = new SortedList<int, MetaValueGuess>();

		private readonly SortedList<int, int> _sizeEstimates = new SortedList<int, int>();
		// Maps size estimate -> frequency of estimate

		private readonly SortedList<int, MetaMap> _submaps = new SortedList<int, MetaMap>();

		public IEnumerable<MetaValueGuess> Guesses
		{
			get { return _guesses.Values; }
		}

		public bool HasSizeEstimates
		{
			get { return (_sizeEstimates.Count > 0); }
		}

		public bool AddGuess(MetaValueGuess guess)
		{
			// Don't overwrite good guesses with null pointers
			if (_guesses.ContainsKey(guess.Offset))
			{
				if (guess.Pointer == 0 || guess.Pointer == 0xFFFFFFFF || guess.Pointer == 0xCDCDCDCD)
					return false;
			}

			/*if (_submaps.ContainsKey(guess.Offset))
                return;*/

			// Just store it
			_guesses[guess.Offset] = guess;
			return true;
		}

		/*public void AddGuessAndSubMap(MetaValueGuess guess, MetaMap map)
        {
            if (map == null)
            {
                AddGuess(guess);
                return;
            }

            // Don't overwrite good guesses with null pointers
            if (_guesses.ContainsKey(guess.Offset))
            {
                if (guess.Pointer == 0 || guess.Pointer == 0xFFFFFFFF || guess.Pointer == 0xCDCDCDCD)
                    return;
            }

            MetaMap previousMap;
            if (_submaps.TryGetValue(guess.Offset, out previousMap))
            {
                if (map.EstimatedSize > previousMap.EstimatedSize)
                    _guesses[guess.Offset] = guess;
            }
            else
            {
                _guesses[guess.Offset] = guess;
            }
            AssociateSubMap(guess.Offset, map);
        }*/

		public void AssociateSubMap(int offset, MetaMap map)
		{
			if (map == null)
				return;
			if (!_guesses.ContainsKey(offset))
				return;

			// Bail out if this will cause a cyclic dependency to happen
			if (map == this || _ancestors.Contains(map) || map._ancestors.Contains(this))
				return;

			// If a map has already been associated with the offset, then merge it,
			// otherwise just insert it
			MetaMap oldMap;
			if (_submaps.TryGetValue(offset, out oldMap))
				oldMap.MergeWith(map);
			else
				_submaps[offset] = map;

			// Combine the map's ancestor set with ours
			map._ancestors.UnionWith(_ancestors);
			map._ancestors.Add(this);
		}

		public void RemoveSubMap(int offset)
		{
			if (_submaps.ContainsKey(offset))
				_submaps.Remove(offset);
		}

		public MetaValueGuess GetGuess(int offset)
		{
			MetaValueGuess guess;
			if (_guesses.TryGetValue(offset, out guess))
				return guess;
			return null; // Nothing has been guessed for this offset
		}

		public MetaMap GetSubMap(int offset)
		{
			MetaMap map;
			if (_submaps.TryGetValue(offset, out map))
				return map;
			return null; // No map for the pointer at this offset
		}

		public void Fold(int firstBlockSize)
		{
			if (firstBlockSize <= 0)
				return;

			// Repeatedly fold the last guess in the list until the key is less than firstBlockSize
			while (_guesses.Count > 0 && _guesses.Keys[_guesses.Count - 1] >= firstBlockSize)
			{
				// Grab the guess at the end and remove it along with its submap
				MetaValueGuess guess = _guesses.Values[_guesses.Count - 1];
				_guesses.RemoveAt(_guesses.Count - 1);

				MetaMap subMap = GetSubMap(guess.Offset);
				if (subMap != null)
					_submaps.Remove(guess.Offset);

				// Wrap its offset
				guess.Offset %= firstBlockSize;

				// Add them back in
				if (AddGuess(guess))
					AssociateSubMap(guess.Offset, subMap);
			}
		}

		public bool IsFolded(int firstBlockSize)
		{
			return (_guesses.Count == 0 || _guesses.Keys[_guesses.Count - 1] < firstBlockSize);
		}

		public void Truncate(int size)
		{
			while (_guesses.Count > 0 && _guesses.Keys[_guesses.Count - 1] >= size)
				_guesses.RemoveAt(_guesses.Count - 1);

			while (_submaps.Count > 0 && _submaps.Keys[_submaps.Count - 1] >= size)
				_submaps.RemoveAt(_submaps.Count - 1);
		}

		public void MergeWith(MetaMap other)
		{
			if (other == this || _ancestors.Contains(other) || other._ancestors.Contains(this))
				return;

			foreach (MetaValueGuess guess in other._guesses.Values)
			{
				MetaMap otherMap = other.GetSubMap(guess.Offset);
				MetaMap myMap = GetSubMap(guess.Offset);
				if (otherMap != null || myMap == null)
				{
					if (AddGuess(guess))
						AssociateSubMap(guess.Offset, otherMap);
				}
			}

			foreach (var estimate in other._sizeEstimates)
				EstimateSize(estimate.Key, estimate.Value);
		}

		public void EstimateSize(int size)
		{
			EstimateSize(size, 1);
		}

		private void EstimateSize(int size, int frequency)
		{
			int oldFrequency;
			if (_sizeEstimates.TryGetValue(size, out oldFrequency))
				_sizeEstimates[size] = oldFrequency + frequency;
			else
				_sizeEstimates[size] = frequency;
		}

		public int GetBestSizeEstimate()
		{
			int highestSize = 0;
			int highestFreq = 0;
			foreach (var estimate in _sizeEstimates)
			{
				if ((estimate.Value > highestFreq) || (estimate.Value == highestFreq && estimate.Key > highestSize))
				{
					highestSize = estimate.Key;
					highestFreq = estimate.Value;
				}
			}
			return highestSize;
		}
	}
}