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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.IO
{
    public sealed class StreamUtil
    {
        public static void Copy(IReader input, IWriter output)
        {
            // http://stackoverflow.com/questions/230128/best-way-to-copy-between-two-stream-instances-c-sharp
            const int BufferSize = 0x1000;
            byte[] buffer = new byte[BufferSize];
            int read;
            while ((read = input.ReadBlock(buffer, 0, BufferSize)) > 0)
                output.WriteBlock(buffer, 0, read);
        }

        public static void Copy(IReader input, IWriter output, int size)
        {
            const int BufferSize = 0x1000;
            byte[] buffer = new byte[BufferSize];
            while (size > 0)
            {
                int read = input.ReadBlock(buffer, 0, Math.Min(BufferSize, size));
                output.WriteBlock(buffer, 0, read);
                size -= BufferSize;
            }
        }
    }
}      
