#pragma once

#pragma unmanaged
#include "NvTriStrip.h"
#pragma managed

using namespace System;

namespace NvTriStripDotNet
{
	/// <summary>
	/// Primitive group types.
	/// </summary>
	public enum class PrimitiveType
	{
		TriangleList = PT_LIST,
		TriangleStrip = PT_STRIP,
		TriangleFan = PT_FAN
	};

	/// <summary>
	/// A group of primitive shapes.
	/// </summary>
	public ref class PrimitiveGroup
	{
	public:
		/// <summary>
		/// Initializes a new instance of the <see cref="PrimitiveGroup"/> class.
		/// </summary>
		/// <param name="type">The type of primitive that the group represents.</param>
		/// <param name="indices">The index buffer for the group.</param>
		PrimitiveGroup(PrimitiveType type, array<unsigned short> ^indices);

		~PrimitiveGroup() { this->!PrimitiveGroup(); }
		!PrimitiveGroup();

		/// <summary>
		/// Gets or sets the group's primitive type.
		/// </summary>
		property PrimitiveType Type
		{
			PrimitiveType get() { return static_cast<PrimitiveType>(data_->type); }
			void set(PrimitiveType type) { data_->type = static_cast<PrimType>(type); }
		}

		/// <summary>
		/// Gets or sets the group's index buffer.
		/// </summary>
		property array<unsigned short>^ Indices
		{
			array<unsigned short>^ get() { return GetIndices(); }
			void set(array<unsigned short> ^indices) { SetIndices(indices); }
		}

	internal:
		PrimitiveGroup(::PrimitiveGroup *data);
		::PrimitiveGroup* GetData() { return data_; }

	private:
		::PrimitiveGroup *data_;
		array<unsigned short> ^originalIndices_;

		void SetIndices(array<unsigned short> ^indices);
		array<unsigned short>^ GetIndices();
	};

	/// <summary>
	/// Generates triangle strips.
	/// </summary>
	public ref class TriStripGenerator abstract sealed
	{
	public:
		/// <summary>
		/// For GPUs that support primitive restart, this sets a value as the restart index.
		/// Restart is disabled by default.
		/// </summary>
		/// <remarks>
		/// Restart is meaningless if strips are not being stitched together, so enabling restart makes NvTriStrip force stitching.
		/// So, you'll get back one strip.
		/// </remarks>
		/// <param name="restartVal">The restart index to set.</param>
		static void EnableRestart(unsigned int restartVal);

		/// <summary>
		/// For GPUs that support primitive restart, this disables using primitive restart.
		/// </summary>
		static void DisableRestart();

		/// <summary>
		/// Sets the cache size which the stripfier uses to optimize the data.
		/// This is set to 16 by default.
		/// </summary>
		/// <remarks>
		/// This is the "actual" cache size, so 24 for GeForce3 and 16 for GeForce1/2
		/// You may want to play around with this number to tweak performance.
		/// </remarks>
		/// <param name="cacheSize">The cache size to use.</param>
		static void SetCacheSize(unsigned int cacheSize);

		/// <summary>
		/// Sets whether to stitch together strips into one huge strip or not.
		/// This is enabled by default.
		/// </summary>
		/// <param name="stitchStrips"><c>true</c> if strips should be stitched together using degenerate triangles.</param>
		static void SetStitchStrips(bool stitchStrips);

		/// <summary>
		/// Sets the minimum acceptable size for a strip, in triangles.
		/// All strips generated which are shorter than this will be thrown into one big, separate list.
		/// This is set to 0 by default.
		/// </summary>
		/// <param name="minSize">The minimum acceptable size for a strip, in triangles.</param>
		static void SetMinStripSize(unsigned int minSize);

		/// <summary>
		/// If set to <c>true</c>, will return an optimized list, with no strips at all.
		/// This is set to <c>false</c> by default.
		/// </summary>
		/// <param name="listsOnly"><c>true</c> if optimized lists should be returned.</param>
		static void SetListsOnly(bool listsOnly);

		/// <summary>
		/// Generates optimized/stripified primitives from a triangle list.
		/// </summary>
		/// <param name="indices">The indices for the triangle list.</param>
		/// <returns>
		/// An array containing the resulting primitives if successful, or <c>null</c> otherwise.
		/// </returns>
		static array<PrimitiveGroup^>^ GenerateStrips(array<unsigned short> ^indices);

		/// <summary>
		/// Generates optimized/stripified primitives from a triangle list.
		/// </summary>
		/// <param name="indices">The indices for the triangle list.</param>
		/// <param name="validateEnabled"><c>true</c> if the output should be validated against the input data.</param>
		/// <returns>
		/// An array containing the resulting primitives if successful, or <c>null</c> otherwise.
		/// </returns>
		static array<PrimitiveGroup^>^ GenerateStrips(array<unsigned short> ^indices, bool validateEnabled);
		
		/// <summary>
		/// Remaps indices to improve spatial locality in your vertex buffer.
		/// </summary>
		/// <remarks>
		/// Note that you must remap your vertex buffer accordingly after calling this method.
		/// Credit goes to the MS Xbox crew for the idea for this interface.
		/// </remarks>
		/// <param name="primGroups">The primitive groups to remap.</param>
		/// <param name="numVerts">The total number of vertices in your vertex buffer.</param>
		/// <returns>An array of primitive groups with remapped indices.</returns>
		static array<PrimitiveGroup^>^ RemapIndices(array<PrimitiveGroup^> ^primGroups, unsigned short numVerts);
	};
}
