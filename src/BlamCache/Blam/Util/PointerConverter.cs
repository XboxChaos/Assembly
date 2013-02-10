using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Blam.Util
{
    /// <summary>
    /// Represents the base for a class which converts values between pointers stored in the file, file offsets, and memory addresses.
    /// </summary>
    public abstract class PointerConverter
    {
        /// <summary>
        /// Converts a pointer stored in the file into a file offset.
        /// </summary>
        /// <param name="pointer">The pointer to convert.</param>
        /// <returns>The pointer's equivalent file offset.</returns>
        public abstract uint PointerToOffset(uint pointer);

        /// <summary>
        /// Converts a pointer stored in the file into a memory address.
        /// </summary>
        /// <param name="pointer">The pointer to convert.</param>
        /// <returns>The pointer's equivalent memory address.</returns>
        public abstract uint PointerToAddress(uint pointer);

        /// <summary>
        /// Converts a file offset into a pointer which can be written to the file.
        /// </summary>
        /// <param name="offset">The file offset to convert.</param>
        /// <returns>The file offset's equivalent pointer.</returns>
        public abstract uint OffsetToPointer(uint offset);

        /// <summary>
        /// Converts a memory address into a pointer which can be written to the file.
        /// </summary>
        /// <param name="address">The memory address to convert.</param>
        /// <returns>The memory address's equivalent pointer.</returns>
        public abstract uint AddressToPointer(uint address);

        /// <summary>
        /// Gets whether or not the converter supports converting to/from memory addresses.
        /// </summary>
        public abstract bool SupportsAddresses { get; }

        /// <summary>
        /// Gets whether or not the converter supports converting to/from file offsets.
        /// </summary>
        public abstract bool SupportsOffsets { get; }

        /// <summary>
        /// Converts a file offset into a memory address.
        /// </summary>
        /// <param name="offset">The file offset to convert.</param>
        /// <returns>The offset's equivalent memory address.</returns>
        public uint OffsetToAddress(uint offset)
        {
            return PointerToAddress(OffsetToPointer(offset));
        }

        /// <summary>
        /// Converts a memory address into a file offset.
        /// </summary>
        /// <param name="address">The memory address to convert.</param>
        /// <returns>The address's equivalent file offset.</returns>
        public uint AddressToOffset(uint address)
        {
            return PointerToOffset(AddressToPointer(address));
        }

        /// <summary>
        /// Converts a Pointer into a pointer which can be written to the file.
        /// </summary>
        /// <param name="pointer">The Pointer to convert.</param>
        /// <returns>The raw value of the Pointer.</returns>
        public uint PointerToRaw(Pointer pointer)
        {
            return OffsetToPointer(pointer.AsOffset());
        }
    }
}
