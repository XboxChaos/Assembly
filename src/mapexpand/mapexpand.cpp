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

#include "endianreader.hpp"
#include "endianwriter.hpp"
#include "cacheheader.hpp"
#include "endianness.hpp"

template<class T>
static T parseString(const std::string& str)
{
	std::istringstream iss(str);
	T result;
	iss >> result;
	return result;
}

int main(int argc, char* argv[])
{
	if (argc != 3)
	{
		std::cout << "Usage: " << argv[0] << " <reach map file> <page count>" << std::endl;
		std::cout << "Pages are multiples of 0x10000 bytes." << std::endl;
		return 0;
	}

	int pageCount = parseString<int>(argv[2]);
	if (pageCount <= 0)
	{
		std::cerr << "The page count must be a positive integer." << std::endl;
		return 0;
	}

	std::cout << "Reading..." << std::endl;
	std::fstream file(argv[1], std::ios::in | std::ios::out | std::ios::binary);
	if (file.fail())
	{
		std::cerr << "Unable to open the map for reading and writing." << std::endl;
		return 0;
	}

	EndianReader reader(&file, EndianReader::BigEndian);
	Blam::CacheHeader header(&reader);

	if (header.version != 0xC)
	{
		std::cerr << "The map must be from the retail version of Halo: Reach." << std::endl;
		return 0;
	}

	std::cout << "- Internal name: " << header.internalName << std::endl;
	std::cout << "- Scenario name: " << header.scenarioName << std::endl;
	std::cout << "- File size: 0x" << std::hex << header.fileSize << std::dec << std::endl;
	std::cout << "- Virtual size: 0x" << std::hex << header.virtualSize << std::dec << std::endl;
	int partitionCount = sizeof(header.partitions) / sizeof(Blam::CachePartition);
	for (int i = 0; i < partitionCount; i++)
		std::cout << "  - Partition " << i << " at 0x" << std::hex << header.partitions[i].address << "-0x" << header.partitions[i].address + header.partitions[i].size - 1 << " (size=0x" << header.partitions[i].size << ")" << std::dec << std::endl;

	uint32_t addressMask = header.virtualBase - header.rawTableOffset - header.rawTableSize;
	std::cout << "- Memory address mask: 0x" << std::hex << addressMask << std::dec << std::endl;
	std::cout << "- Locale address mask: 0x" << std::hex << header.localeAddressMask << std::dec << std::endl;

	std::cout << "\nInjecting empty pages..." << std::endl;

	long injectSize = pageCount * 0x10000;
	uint32_t injectAddr = header.partitions[0].address - injectSize;
	uint32_t injectOffset = header.partitions[0].address - addressMask;
	std::cout << "- Start address: 0x" << std::hex << injectAddr << " (offset 0x" << injectOffset << ")" << std::dec << std::endl;

	EndianWriter writer(&file, EndianWriter::BigEndian);

	const long BufferSize = 0x100000; // 1 MB
	uint8_t* buffer = new uint8_t[BufferSize];

	// Push everything back
	long moved = 0;
	long moveSize = header.fileSize - injectOffset;
	while (moved < moveSize)
	{
		long readSize = std::min(moveSize - moved, BufferSize);
		reader.seekTo(header.fileSize - moved - readSize);
		long read = reader.readBlock(readSize, buffer);

		writer.seekTo(header.fileSize + injectSize - moved - readSize);
		writer.writeBlock(buffer, read);
		moved += read;
	}

	// Fill the injected area with zeroes
	writer.seekTo(injectOffset);
	long zeroed = 0;
	while (zeroed < injectSize)
	{
		long zeroSize = std::min(injectSize - zeroed, BufferSize);
		std::memset(buffer, 0, zeroSize);
		writer.writeBlock(buffer, zeroSize);
		zeroed += zeroSize;
	}
	delete buffer;

	std::cout << "\nAdjusting the header..." << std::endl;
	uint32_t oldVirtualSize = header.virtualSize;
	header.fileSize += injectSize;
	header.virtualSize += injectSize;
	header.virtualBase -= injectSize;
	header.localeAddressMask += injectSize;
	header.partitions[0].address -= injectSize;
	header.partitions[0].size += injectSize;
	writer.seekTo(0);
	header.write(&writer);

	std::cout << "\nSuccessfully injected 0x" << std::hex << injectSize << " bytes at 0x" << injectAddr << " (offset 0x" << injectOffset << ")." << std::endl;

	return 0;
}