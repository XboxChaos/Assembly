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
#include "endianness.hpp"
#include "endianreader.hpp"

EndianReader::EndianReader(std::istream* stream, Endianness endianness)
	: stream_(stream)
{
	if (stream->bad())
		throw std::invalid_argument("Cannot read from a stream with badbit set");

	bigEndian_ = (endianness == BigEndian);
}

uint8_t EndianReader::readByte()
{
	uint8_t result;
	stream_->read(reinterpret_cast<char*>(&result), 1);
	return result;
}

uint16_t EndianReader::readWord()
{
	uint16_t result;
	stream_->read(reinterpret_cast<char*>(&result), 2);

	// Swap if endianness doesn't match
	if (cpuIsBigEndian() == bigEndian_)
		return result;
	else
		return SWAP16(result);
}

uint32_t EndianReader::readDword()
{
	uint32_t result;
	stream_->read(reinterpret_cast<char*>(&result), 4);

	// Swap if endianness doesn't match
	if (cpuIsBigEndian() == bigEndian_)
		return result;
	else
		return SWAP32(result);
}

std::string EndianReader::readBlock(long size)
{
	// Read into a char buffer
	char* buffer = new char[size];
	stream_->read(buffer, size);

	// Now make a std::string from it
	std::string result(buffer, size);
	delete [] buffer;
	return result;
}

long EndianReader::readBlock(long size, uint8_t* out)
{
	stream_->read(reinterpret_cast<char*>(out), size);
	return stream_->gcount();
}

std::string EndianReader::readAsciizString()
{
	std::string result;
	long pos = static_cast<long>(stream_->tellg());

	while (true)
	{
		char ch;
		stream_->get(ch);

		if (ch && !stream_->eof())
			result += ch;
		else
			break;
	}

	return result;
}

std::string EndianReader::readAsciizString(long size)
{
	char* buffer = new char[size + 1];
	buffer[size] = 0;
	stream_->read(buffer, size);

	std::string result(buffer);
	delete [] buffer;
	return result;
}

void EndianReader::seekTo(long position)
{
	stream_->seekg(position);
}

void EndianReader::seekRelative(long offset)
{
	stream_->seekg(offset, std::ios_base::cur);
}

long EndianReader::tell()
{
	return static_cast<long>(stream_->tellg());
}