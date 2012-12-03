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

#include "stdafx.h"
#include "cacheheader.hpp"

namespace Blam
{
	CacheHeader::CacheHeader(EndianReader* reader)
	{
		magic = reader->readDword();
		version = reader->readDword();
		fileSize = reader->readDword();
		reader->seekRelative(4);
		indexHeaderAddr = reader->readDword();
		eofIndexOffset = reader->readDword();
		virtualSize = reader->readDword();
		
		reader->seekTo(0x13C);
		type = reader->readWord();

		reader->seekTo(0x158);
		stringTableCount = reader->readDword();
		stringTableSize = reader->readDword();
		stringIndexTableOffset = reader->readDword();
		stringTableOffset = reader->readDword();

		reader->seekTo(0x18C);
		reader->readBlock(sizeof(internalName), reinterpret_cast<uint8_t*>(internalName));

		reader->seekTo(0x1B0);
		reader->readBlock(sizeof(scenarioName), reinterpret_cast<uint8_t*>(scenarioName));

		reader->seekTo(0x2B4);
		filenameCount = reader->readDword();
		filenameTableOffset = reader->readDword();
		filenameTableSize = reader->readDword();
		filenameIndexTableOffset = reader->readDword();

		reader->seekTo(0x2E8);
		virtualBase = reader->readDword();
		xdkVersion = reader->readDword();
		for (int i = 0; i < sizeof(partitions) / sizeof(CachePartition); i++)
		{
			partitions[i].address = reader->readDword();
			partitions[i].size = reader->readDword();
		}

		reader->seekTo(0x470);
		rawTableOffset = reader->readDword();
		reader->seekRelative(4);
		localeAddressMask = reader->readDword();
		reader->seekRelative(4);
		rawTableHeaderOffset = reader->readDword();
		reader->seekRelative(4);
		rawTableSize = reader->readDword();

		reader->seekTo(0x494);
		localeDataIndexOffset = reader->readDword();
		localeDataSize = reader->readDword();
	}

	void CacheHeader::write(EndianWriter* writer)
	{
		writer->writeDword(magic);
		writer->writeDword(version);
		writer->writeDword(fileSize);
		writer->seekRelative(4);
		writer->writeDword(indexHeaderAddr);
		writer->writeDword(eofIndexOffset);
		writer->writeDword(virtualSize);
		
		writer->seekTo(0x13C);
		writer->writeWord(type);

		writer->seekTo(0x158);
		writer->writeDword(stringTableCount);
		writer->writeDword(stringTableSize);
		writer->writeDword(stringIndexTableOffset);
		writer->writeDword(stringTableOffset);

		writer->seekTo(0x18C);
		writer->writeBlock(reinterpret_cast<uint8_t*>(internalName), sizeof(internalName));

		writer->seekTo(0x1B0);
		writer->writeBlock(reinterpret_cast<uint8_t*>(scenarioName), sizeof(scenarioName));

		writer->seekTo(0x2B4);
		writer->writeDword(filenameCount);
		writer->writeDword(filenameTableOffset);
		writer->writeDword(filenameTableSize);
		writer->writeDword(filenameIndexTableOffset);

		writer->seekTo(0x2E8);
		writer->writeDword(virtualBase);
		writer->writeDword(xdkVersion);
		for (int i = 0; i < sizeof(partitions) / sizeof(CachePartition); i++)
		{
			writer->writeDword(partitions[i].address);
			writer->writeDword(partitions[i].size);
		}

		writer->seekTo(0x470);
		writer->writeDword(rawTableOffset);
		writer->seekRelative(4);
		writer->writeDword(localeAddressMask);
		writer->writeDword(stringIndexTableOffset);
		writer->writeDword(rawTableHeaderOffset);
		writer->seekRelative(4);
		writer->writeDword(rawTableSize);
		writer->writeDword(eofIndexOffset);
		writer->writeDword(virtualSize);
		writer->writeDword(localeDataIndexOffset);
		writer->writeDword(localeDataSize);
	}
}