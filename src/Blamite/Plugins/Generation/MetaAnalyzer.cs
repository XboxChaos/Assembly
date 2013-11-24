using System;
using System.Collections.Generic;
using Blamite.Blam;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Plugins.Generation
{
	public class MetaAnalyzer
	{
		private readonly HashSet<int> _classIds = new HashSet<int>();
		private readonly MemoryMap _memMap = new MemoryMap();
		private uint _maxAddress;
		private uint _minAddress;

		public MetaAnalyzer(ICacheFile cacheFile)
		{
			InitializeMemoryMap(cacheFile);
			RecognizeClassIDs(cacheFile);
		}

		public MemoryMap GeneratedMemoryMap
		{
			get { return _memMap; }
		}

		public void AnalyzeArea(IReader reader, uint startAddress, MetaMap resultMap)
		{
			// Right now, this method only searches for the signatures of a few complex meta values.
			// Reflexives:      int32 entry count + uint32 address     + 4 bytes of padding
			// Data references: int32 size        + 8 bytes of padding + uint32 address
			// Tag references:  int32 class id    + 8 bytes of padding + uint32 datum index
			// ASCII strings:   characters with the values 0 or 0x20 - 0x7F

			// End at the next-highest address
			uint endAddress = _memMap.GetNextHighestAddress(startAddress);
			if (endAddress == 0xFFFFFFFF)
				throw new InvalidOperationException("Invalid start address for area analysis");

			uint size = endAddress - startAddress; // The size of the block of data
			int paddingLength = 0; // The number of 4-byte padding values that have been read
			uint prePadding = 0; // The last non-padding uint32 that was read

			MetaValueGuess pendingGuess = null; // If not null and padding is encountered, this guess is confirmed

			for (int offset = 0; offset < size; offset += 4)
			{
				uint value = reader.ReadUInt32();

				if (IsPadding(value))
				{
					if (paddingLength == 0 && pendingGuess != null)
					{
						resultMap.AddGuess(pendingGuess);

						// Add the address to the memory map
						uint address = pendingGuess.Pointer;
						_memMap.AddAddress(address, (int) reader.Position);
					}

					// More padding! :D
					paddingLength++;
					pendingGuess = null;
				}
				else
				{
					pendingGuess = null;
					if (offset <= size - 8
					    && prePadding > 0
					    && prePadding < 0x80000000
					    && (value & 3) == 0
					    && IsValidAddress(value)
					    && value + prePadding > value
					    && IsValidAddress(value + prePadding - 1)
					    && !_memMap.BlockCrossesBoundary(value, (int) prePadding))
					{
						// Either a reflexive or a data reference
						// Check the padding to determine which (see the comments at the beginning of this method)
						if (paddingLength == 2 && offset >= 12 && (prePadding & 3) == 0)
						{
							// Found a data reference
							uint dataSize = prePadding;
							uint address = value;
							pendingGuess = new MetaValueGuess(offset - 12, MetaValueType.DataReference, address, dataSize);
							// Guess with Pointer = address, Data1 = data size
						}
						else if (paddingLength == 0 && offset >= 4)
						{
							// Found a reflexive!
							uint entryCount = prePadding;
							uint address = value;
							pendingGuess = new MetaValueGuess(offset - 4, MetaValueType.Reflexive, address, entryCount);
							// Guess with Pointer = address, Data1 = entry count
						}
					}
					if (paddingLength == 2 && offset >= 12 &&
					    (_classIds.Contains((int) prePadding) || (prePadding == 0xFFFFFFFF && value == 0xFFFFFFFF)))
					{
						// Found a tag reference
						uint classId = prePadding;
						uint datumIndex = value;
						var guess = new MetaValueGuess(offset - 12, MetaValueType.TagReference, datumIndex, classId);
						// Guess with Pointer = datum index, Data1 = class id
						resultMap.AddGuess(guess);
					}

					// This obviously isn't a padding value because IsPadding returned false,
					// so update padding run information accordingly
					prePadding = value;
					paddingLength = 0;
				}
			}
		}

		private void InitializeMemoryMap(ICacheFile cacheFile)
		{
			RecognizePartitionBoundaries(cacheFile);
			RecognizeTagBoundaries(cacheFile);
		}

		private void RecognizePartitionBoundaries(ICacheFile cacheFile)
		{
			_minAddress = cacheFile.MetaArea.BasePointer;
			foreach (Partition partition in cacheFile.Partitions)
			{
				if (partition.BasePointer != null)
					_memMap.AddAddress(partition.BasePointer.AsPointer(), 0);
			}

			// Add the end of meta as well
			_maxAddress = (uint) (_minAddress + cacheFile.MetaArea.Size);
			_memMap.AddAddress(_maxAddress, 0);
		}

		private void RecognizeTagBoundaries(ICacheFile cacheFile)
		{
			foreach (ITag tag in cacheFile.Tags)
			{
				if (tag.MetaLocation != null)
					_memMap.AddAddress(tag.MetaLocation.AsPointer(), 0);
			}

			// Add the index header as well :P
			_memMap.AddAddress(cacheFile.IndexHeaderLocation.AsPointer(), 0);
		}

		private void RecognizeClassIDs(ICacheFile cacheFile)
		{
			foreach (ITagClass tagClass in cacheFile.TagClasses)
				_classIds.Add(tagClass.Magic);
		}

		/// <summary>
		///     Returns whether or not a value appears to be padding.
		/// </summary>
		/// <param name="value">The value to test.</param>
		/// <returns>true if the value appears to be padding.</returns>
		private static bool IsPadding(uint value)
		{
			return (value == 0 || value == 0xCDCDCDCD);
		}

		/// <summary>
		///     Returns whether or not a value appears to be a memory address.
		/// </summary>
		/// <param name="value">The value to test.</param>
		/// <returns>true if the value is a valid memory address.</returns>
		private bool IsValidAddress(uint value)
		{
			// Check that the address is between the meta start and end
			return (value >= _minAddress && value < _maxAddress);
		}
	}
}