﻿using Blamite.Serialization;

namespace Blamite.Blam.FourthGen.Structures
{
	/// <summary>
	///     Interop section indices.
	/// </summary>
	public enum FourthGenInteropSectionType
	{
		Debug,
		Resource,
		Tag,
		Localization
	}

	/// <summary>
	///     Contains information about a section of a cache file.
	/// </summary>
	public class FourthGenInteropSection
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="FourthGenInteropSection" /> class.
		/// </summary>
		/// <param name="virtAddr">The virtual address.</param>
		/// <param name="size">The size.</param>
		public FourthGenInteropSection(uint virtAddr, uint size)
		{
			VirtualAddress = virtAddr;
			Size = size;
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="FourthGenInteropSection" /> class.
		/// </summary>
		/// <param name="values">The structure to load values from.</param>
		public FourthGenInteropSection(StructureValueCollection values)
		{
			VirtualAddress = values.GetInteger("virtual address");
			Size = values.GetInteger("size");
		}

		/// <summary>
		///     Gets or sets the virtual address of the section.
		/// </summary>
		/// <remarks>
		///     This is not necessarily a memory address.
		///     It is relative to another base address, such as one of the offset masks in the interop data.
		/// </remarks>
		public uint VirtualAddress { get; set; }

		/// <summary>
		///     Gets or sets the size of the section.
		/// </summary>
		public uint Size { get; set; }

		/// <summary>
		///     Serializes this instance.
		/// </summary>
		/// <returns>A collection of structure values.</returns>
		public StructureValueCollection Serialize()
		{
			var result = new StructureValueCollection();
			result.SetInteger("virtual address", VirtualAddress);
			result.SetInteger("size", Size);
			return result;
		}
	}
}