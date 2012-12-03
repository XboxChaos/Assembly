/* Copyright (c) 2012 Aaron Dierking <amd@xboxchaos.com>.
   This file is part of mapexpand.

   mapexpand is free software: you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.

   mapexpand is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with mapexpand.  If not, see <http://www.gnu.org/licenses/>. */

#pragma once

#include "stdafx.h"
#include "endianreader.hpp"
#include "endianwriter.hpp"

namespace Blam
{
	struct CachePartition
	{
		uint32_t address;
		uint32_t size;
	};

	class CacheHeader
	{
	public:
		CacheHeader() {}
		CacheHeader(EndianReader* reader);

		void write(EndianWriter* writer);

		uint32_t magic;
		int32_t version;
		uint32_t fileSize;
		uint32_t indexHeaderAddr;
		uint32_t eofIndexOffset;
		uint32_t virtualSize;
		int16_t type;

		int32_t stringTableCount;
		int32_t stringTableSize;
		int32_t stringIndexTableOffset;
		int32_t stringTableOffset;

		char internalName[0x20];
		char scenarioName[0x104];

		int32_t filenameCount;
		int32_t filenameTableOffset;
		int32_t filenameTableSize;
		int32_t filenameIndexTableOffset;

		uint32_t virtualBase;
		uint32_t xdkVersion;
		CachePartition partitions[6];

		uint32_t rawTableOffset;
		uint32_t localeAddressMask;
		uint32_t rawTableHeaderOffset;
		uint32_t rawTableSize;

		uint32_t localeDataIndexOffset;
		uint32_t localeDataSize;
	};
}