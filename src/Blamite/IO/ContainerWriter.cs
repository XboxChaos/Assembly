using System;
using System.Collections.Generic;
using Blamite.Util;

namespace Blamite.IO
{
	/// <summary>
	///     Allows data to be written to files in named blocks.
	/// </summary>
	public class ContainerWriter
	{
		private readonly Stack<long> _blockSizeOffsets = new Stack<long>();
		private readonly IWriter _writer;

		/// <summary>
		///     Initializes a new instance of the <see cref="Blamite.IO.ContainerWriter" /> class.
		/// </summary>
		/// <param name="writer">The stream to write to.</param>
		public ContainerWriter(IWriter writer)
		{
			_writer = writer;
		}

		/// <summary>
		///     Writes the header for a new block.
		/// </summary>
		/// <param name="id">The name of the block. Can be no more than 4 characters long.</param>
		/// <param name="version">The block's version number.</param>
		public void StartBlock(string id, byte version)
		{
			if (id.Length > 4)
				throw new ArgumentException("Container block IDs can be no more than 4 bytes long");

			id = id.PadRight(4);
			StartBlock((uint) CharConstant.FromString(id), version);
		}

		/// <summary>
		///     Writes the header for a new block.
		/// </summary>
		/// <param name="id">The block's ID number.</param>
		/// <param name="version">The block's version number.</param>
		public void StartBlock(uint id, byte version)
		{
			_writer.WriteUInt32(id);

			// Save the position of the block size for when the block is closed,
			// and just write a zero for the size for now
			// because it'll get updated when the block is closed
			_blockSizeOffsets.Push(_writer.Position);
			_writer.WriteInt32(0); // Just write zero for the size for now - this gets updated later

			_writer.WriteByte(version);
		}

		/// <summary>
		///     Closes the currently-active container block.
		/// </summary>
		public void EndBlock()
		{
			if (_blockSizeOffsets.Count == 0)
				throw new InvalidOperationException("A block is not currently open");

			// Calculate and write the block size
			long currentPos = _writer.Position;
			long blockSizePos = _blockSizeOffsets.Pop();
			var blockSize = (int) (currentPos - blockSizePos + 4); // Add 4 bytes for the header
			_writer.SeekTo(blockSizePos);
			_writer.WriteInt32(blockSize);
			_writer.SeekTo(currentPos);
		}
	}
}