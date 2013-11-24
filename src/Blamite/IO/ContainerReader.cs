using System;
using System.Collections.Generic;
using Blamite.Util;

namespace Blamite.IO
{
	/// <summary>
	///     Allows data to be read from a file split up into named blocks.
	/// </summary>
	public class ContainerReader
	{
		private const int BlockHeaderSize = 9; // uint32 + int32 + byte
		private readonly Stack<long> _blockEnds = new Stack<long>();
		private readonly IReader _reader;
		private long _currentBlockEnd = -1;

		/// <summary>
		///     Initializes a new instance of the <see cref="ContainerReader" /> class.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		public ContainerReader(IReader reader)
		{
			_reader = reader;
			_blockEnds.Push(reader.Length);
			_currentBlockEnd = _reader.Position;
		}

		/// <summary>
		///     Gets the ID number of the current block.
		/// </summary>
		/// <value>The ID number of the current block.</value>
		public uint BlockID { get; private set; }

		/// <summary>
		///     Gets the ID of the current block as a string.
		/// </summary>
		/// <value>The ID of the current block as a string.</value>
		public string BlockName { get; private set; }

		/// <summary>
		///     Gets the size of the current block's data (excluding the block header).
		/// </summary>
		/// <value>The size of the current block's data (excluding the block header).</value>
		public int BlockSize { get; private set; }

		/// <summary>
		///     Gets the version number of the current block.
		/// </summary>
		/// <value>The version number of the current block.</value>
		public byte BlockVersion { get; private set; }

		/// <summary>
		///     Jumps to the next block and reads its header.
		///     The <see cref="BlockID" />, <see cref="BlockName" />, <see cref="BlockSize" />, and <see cref="BlockVersion" />
		///     properties will be updated to hold information about the block.
		/// </summary>
		/// <returns><c>true</c> if another block was available, or <c>false</c> otherwise.</returns>
		public bool NextBlock()
		{
			// If we're at the end of the parent block, then we're done here
			if (_blockEnds.Count > 0 && _currentBlockEnd + BlockHeaderSize > _blockEnds.Peek())
				return false;

			// Skip to the end of the current block
			_reader.SeekTo(_currentBlockEnd);

			// Read a new block header
			BlockID = _reader.ReadUInt32();
			BlockName = CharConstant.ToString((int) BlockID);
			BlockSize = _reader.ReadInt32() - BlockHeaderSize;
			BlockVersion = _reader.ReadByte();

			_currentBlockEnd = _reader.Position + BlockSize;
			return true;
		}

		/// <summary>
		///     Descends into the current block. The next block to be read will be the first block contained inside of it.
		/// </summary>
		public void EnterBlock()
		{
			_blockEnds.Push(_currentBlockEnd);
			_currentBlockEnd = _reader.Position;
		}

		/// <summary>
		///     Ascends from the current block. The next block to be read will be the first block after the current block's last
		///     child block.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if no block is currently open.</exception>
		public void LeaveBlock()
		{
			if (_blockEnds.Count <= 1)
				throw new InvalidOperationException("No block is currently open");

			_currentBlockEnd = _blockEnds.Pop();
		}
	}
}