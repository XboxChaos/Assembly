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

class EndianWriter
{
public:
	// Endianness constants
	enum Endianness
	{
		BigEndian,
		LittleEndian
	};

	// Constructs a new EndianWriter, writing to an std::ostream and using the specified endianness.
	EndianWriter(std::ostream* stream, Endianness endianness);

	// Writes an unsigned byte and advances the current position by 1.
	void writeByte(uint8_t b);

	// Writes an unsigned 2-byte word and advances the current position by 2.
	void writeWord(uint16_t w);

	// Writes an unsigned 4-byte dword and advances the current position by 4.
	void writeDword(uint32_t d);

	// Writes a block of data.
	void writeBlock(const std::string& data);

	// Writes a block of data.
	void writeBlock(uint8_t* buffer, long size);

	// Writes a null-terminated ASCII string.
	void writeAsciizString(const std::string& str);
	
	// Seeks to an absolute position in the stream.
	void seekTo(long position);

	// Seeks relative to the current position in the stream.
	void seekRelative(long offset);

	// Returns the current offset.
	long tell();

private:
	std::ostream* stream_;
	bool bigEndian_;
};