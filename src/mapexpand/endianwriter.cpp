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
#include "endianwriter.hpp"

EndianWriter::EndianWriter(std::ostream* stream, Endianness endianness)
	: stream_(stream)
{
	if (stream->bad())
		throw std::invalid_argument("Cannot write to a stream with badbit set");

	bigEndian_ = (endianness == BigEndian);
}

void EndianWriter::writeByte(uint8_t b)
{
	stream_->write(reinterpret_cast<const char*>(&b), 1);
}

void EndianWriter::writeWord(uint16_t w)
{
	// Swap if endianness doesn't match
	if (cpuIsBigEndian() != bigEndian_)
		w = SWAP16(w);
	stream_->write(reinterpret_cast<const char*>(&w), 2);
}

void EndianWriter::writeDword(uint32_t d)
{
	// Swap if endianness doesn't match
	if (cpuIsBigEndian() != bigEndian_)
		d = SWAP32(d);
	stream_->write(reinterpret_cast<const char*>(&d), 4);
}

void EndianWriter::writeBlock(const std::string& data)
{
	stream_->write(data.c_str(), data.length());
}

void EndianWriter::writeBlock(uint8_t* buffer, long size)
{
	stream_->write(reinterpret_cast<const char*>(buffer), size);
}

void EndianWriter::writeAsciizString(const std::string& str)
{
	stream_->write(str.c_str(), str.length() + 1);
}

void EndianWriter::seekTo(long position)
{
	stream_->seekp(position);
}

void EndianWriter::seekRelative(long offset)
{
	stream_->seekp(offset, std::ios_base::cur);
}

long EndianWriter::tell()
{
	return static_cast<long>(stream_->tellp());
}