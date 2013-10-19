#include <cstring>

#include "NvTriStripDotNet.h"

using namespace System::Runtime::InteropServices;

namespace NvTriStripDotNet
{
	// Converts an unmanaged PrimitiveGroup array to a managed one.
	static array<PrimitiveGroup^>^ ToManagedPrimGroupArray(::PrimitiveGroup *groups, int numGroups);

	PrimitiveGroup::PrimitiveGroup(PrimitiveType type, array<unsigned short> ^indices)
		: data_(NULL), originalIndices_(nullptr)
	{
		data_ = new ::PrimitiveGroup();
		data_->type = static_cast<PrimType>(type);
		SetIndices(indices);
	}

	PrimitiveGroup::PrimitiveGroup(::PrimitiveGroup *data)
		: data_(NULL), originalIndices_(nullptr)
	{
		data_ = new ::PrimitiveGroup();
		std::memcpy(data_, data, sizeof(::PrimitiveGroup));
	}

	PrimitiveGroup::!PrimitiveGroup()
	{
		if (data_ != NULL)
		{
			if (data_->indices != NULL)
			{
				delete data_->indices;
				data_->indices = NULL;
			}
			delete data_;
			data_ = NULL;
		}
		originalIndices_ = nullptr;
	}

	void PrimitiveGroup::SetIndices(array<unsigned short> ^indices)
	{
		// Cache the array
		originalIndices_ = indices;
		if (data_->indices != NULL)
			delete [] data_->indices;

		// Update the internal array
		data_->indices = new unsigned short[indices->Length];
		Marshal::Copy(reinterpret_cast<array<short>^>(indices), 0, IntPtr(data_->indices), indices->Length);
		data_->numIndices = indices->Length;
	}

	array<unsigned short>^ PrimitiveGroup::GetIndices()
	{
		// Return the original array if cached
		if (originalIndices_ != nullptr)
			return originalIndices_;

		// Otherwise allocate a new one
		originalIndices_ = gcnew array<unsigned short>(data_->numIndices);
		Marshal::Copy(IntPtr(data_->indices), reinterpret_cast<array<short>^>(originalIndices_), 0, data_->numIndices);
		return originalIndices_;
	}

	static array<PrimitiveGroup^>^ ToManagedPrimGroupArray(::PrimitiveGroup *groups, int numGroups)
	{
		array<PrimitiveGroup^> ^resultArray = gcnew array<PrimitiveGroup^>(numGroups);
		for (int i = 0; i < numGroups; i++)
			resultArray[i] = gcnew PrimitiveGroup(&groups[i]);
		return resultArray;
	}

	void TriStripGenerator::EnableRestart(unsigned int restartVal)
	{
		::EnableRestart(restartVal);
	}

	void TriStripGenerator::DisableRestart()
	{
		::DisableRestart();
	}

	void TriStripGenerator::SetCacheSize(unsigned int cacheSize)
	{
		::SetCacheSize(cacheSize);
	}

	void TriStripGenerator::SetStitchStrips(bool stitchStrips)
	{
		::SetStitchStrips(stitchStrips);
	}

	void TriStripGenerator::SetMinStripSize(unsigned int minSize)
	{
		::SetMinStripSize(minSize);
	}

	void TriStripGenerator::SetListsOnly(bool listsOnly)
	{
		::SetListsOnly(listsOnly);
	}

	array<PrimitiveGroup^>^ TriStripGenerator::GenerateStrips(array<unsigned short> ^indices)
	{
		return GenerateStrips(indices, false);
	}

	array<PrimitiveGroup^>^ TriStripGenerator::GenerateStrips(array<unsigned short> ^indices, bool validateEnabled)
	{
		pin_ptr<unsigned short> indicesPinned = &indices[0];
		::PrimitiveGroup *result = NULL;
		unsigned short numGroups = 0;

		bool success = ::GenerateStrips(indicesPinned, indices->Length, &result, &numGroups, validateEnabled);
		array<PrimitiveGroup^> ^resultArray = nullptr;
		if (success)
			resultArray = ToManagedPrimGroupArray(result, numGroups);

		if (result != NULL)
		{
			delete [] result;
			result = NULL;
		}

		return resultArray;
	}

	array<PrimitiveGroup^>^ TriStripGenerator::RemapIndices(array<PrimitiveGroup^> ^primGroups, unsigned short numVerts)
	{
		::PrimitiveGroup *inGroups = new ::PrimitiveGroup[primGroups->Length];
		for (int i = 0; i < primGroups->Length; i++)
			inGroups[i] = *primGroups[i]->GetData();

		::PrimitiveGroup *remappedGroups = NULL;
		::RemapIndices(inGroups, primGroups->Length, numVerts, &remappedGroups);

		delete [] inGroups;
		inGroups = NULL;

		array<PrimitiveGroup^> ^resultArray = ToManagedPrimGroupArray(remappedGroups, primGroups->Length);

		delete [] remappedGroups;
		remappedGroups = NULL;

		return resultArray;
	}
}