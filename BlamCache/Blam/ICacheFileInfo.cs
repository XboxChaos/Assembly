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
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam
{
    /// <summary>
    /// Information about a cache file, as stored in its header.
    /// </summary>
    public interface ICacheFileInfo
    {
        /// <summary>
        /// The size of the file header.
        /// </summary>
        int HeaderSize { get; }

        /// <summary>
        /// The size of the cache file.
        /// </summary>
        uint FileSize { get; set; }

        /// <summary>
        /// The purpose of the cache file.
        /// </summary>
        CacheFileType Type { get; }

        /// <summary>
        /// The cache file's internal name.
        /// </summary>
        string InternalName { get; }

        /// <summary>
        /// The name of the cache file's scenario tag (can be null if none).
        /// </summary>
        string ScenarioName { get; }

        /// <summary>
        /// The engine build string that the cache file is intended for.
        /// </summary>
        string BuildString { get; }

        /// <summary>
        /// The location of the tag table header in the file.
        /// (Dependent upon VirtualBaseAddress and MetaOffset - changing one of them will change this value.)
        /// </summary>
        Pointer IndexHeaderLocation { get; }
        
        /// <summary>
        /// The location of the first partition in the file.
        /// (<see cref="VirtualBaseAddress"/> and <see cref="MetaOffset"/> combined into a Pointer).
        /// </summary>
        Pointer MetaBase { get; }

        /// <summary>
        /// The address of the start of the first partition in the cache file.
        /// </summary>
        uint VirtualBaseAddress { get; set; }

        /// <summary>
        /// The offset of the start of the first partition in the cache file.
        /// </summary>
        uint MetaOffset { get; set; }

        /// <summary>
        /// The total size of the cache file's meta partitions.
        /// </summary>
        uint MetaSize { get; set; }

        /// <summary>
        /// The XDK version that the cache file was developed with, or 0 if unknown.
        /// </summary>
        int XDKVersion { get; set; }

        /// <summary>
        /// The meta partitions in the cache file.
        /// </summary>
        Partition[] Partitions { get; }

        /// <summary>
        /// The offset of the raw table in the cache file, or 0 if unknown.
        /// </summary>
        uint RawTableOffset { get; set; }

        /// <summary>
        /// The size of the raw table in the cache file, or 0 if unknown.
        /// </summary>
        uint RawTableSize { get; set; }

        /// <summary>
        /// The value which can be subtracted from an address to get its file offset.
        /// AKA map magic.
        /// </summary>
        uint AddressMask { get; }

        /// <summary>
        /// The value which can be added to a locale table pointer to get its file offset.
        /// </summary>
        uint LocaleOffsetMask { get; set; }

        /// <summary>
        /// The location of the locale data in the cache file.
        /// </summary>
        Pointer LocaleDataLocation { get; set; }

        /// <summary>
        /// The size of the locale data in the cache file.
        /// </summary>
        int LocaleDataSize { get; set; }
    }
}
