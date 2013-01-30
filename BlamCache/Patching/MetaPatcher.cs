using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Patching
{
    /// <summary>
    /// Provides static methods for patching tag meta in a cache file.
    /// </summary>
    public static class MetaPatcher
    {
        /// <summary>
        /// Writes a series of meta changes back to a cache file.
        /// </summary>
        /// <param name="cacheFile">The cache file to write the changes to.</param>
        /// <param name="change">The changes to write.</param>
        /// <param name="output">The stream to write the changes to.</param>
        public static void WriteChanges(IEnumerable<MetaChange> changes, ICacheFile cacheFile, IWriter output)
        {
            foreach (MetaChange change in changes)
                WriteChange(cacheFile, change, output);
        }

        /// <summary>
        /// Writes a meta change back to a cache file.
        /// </summary>
        /// <param name="cacheFile">The cache file to write the change to.</param>
        /// <param name="change">The change to write.</param>
        /// <param name="output">The stream to write the change to.</param>
        public static void WriteChange(ICacheFile cacheFile, MetaChange change, IWriter output)
        {
            output.SeekTo(cacheFile.MetaPointerConverter.AddressToOffset(change.Address));
            output.WriteBlock(change.Data);
        }

        /// <summary>
        /// Pokes a series of meta changes back to an Xbox.
        /// </summary>
        /// <param name="change">The changes to poke.</param>
        /// <param name="output">The Xbox memory stream to write the changes to.</param>
        public static void PokeChanges(IEnumerable<MetaChange> changes, IWriter output)
        {
            foreach (MetaChange change in changes)
                PokeChange(change, output);
        }

        /// <summary>
        /// Pokes a meta change back to an Xbox.
        /// </summary>
        /// <param name="change">The change to poke.</param>
        /// <param name="output">The Xbox memory stream to write the change to.</param>
        public static void PokeChange(MetaChange change, IWriter output)
        {
            output.SeekTo(change.Address);
            output.WriteBlock(change.Data);
        }
    }
}
