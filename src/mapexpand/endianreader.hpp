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

#include <fstream>
#include <string>
#include <stdint.h>

class EndianReader
{
public:
	// Endianness constants
	enum Endianness
	{
		BigEndian,
		LittleEndian
	};

	// Constructs a new EndianReader, reading from an std::istream and using the specified endianness.
	EndianReader(std::istream* stream, Endianness endianness);

	// Reads an unsigned byte and advances the current position by 1.
	uint8_t readByte();

	// Reads an unsigned 2-byte word and advances the current position by 2.
	uint16_t readWord();

	// Reads an unsigned 4-byte dword and advances the current position by 4.
	uint32_t readDword();

	// Reads and returns a block of data.
	std::string readBlock(long size);

	// Reads a block of data into a buffer.
	long readBlock(long size, uint8_t* out);

	// Reads and returns a null-terminated ASCII string.
	std::string readAsciizString();

	// Reads and returns a null-terminated ASCII string of the specified size.
	std::string readAsciizString(long size);
	
	// Seeks to an absolute position in the stream.
	void seekTo(long position);

	// Seeks relative to the current position in the stream.
	void seekRelative(long offset);

	// Returns the current offset.
	long tell();

private:
	std::istream* stream_;
	bool bigEndian_;
};