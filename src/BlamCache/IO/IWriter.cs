/* Copyright 2012 Aaron Dierking, TJ Tunnell, Jordan Mueller, Alex Reed
 * 
 * This file is part of ExtryzeDLL.
 * 
 * Extryze is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Extryze is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ExtryzeDLL.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace ExtryzeDLL.IO
{
    public interface IWriter : IBaseStream
    {
        void WriteAscii(string str);
        void WriteBlock(byte[] data);
        void WriteBlock(byte[] data, int offset, int size);
        void WriteByte(byte value);
        void WriteFloat(float value);
        void WriteInt16(short value);
        void WriteInt32(int value);
        void WriteInt64(long value);
        void WriteSByte(sbyte value);
        void WriteUInt16(ushort value);
        void WriteUInt32(uint value);
        void WriteUInt64(ulong value);
        void WriteUTF8(string str);
        void WriteUTF16(string str);
    }
}      
