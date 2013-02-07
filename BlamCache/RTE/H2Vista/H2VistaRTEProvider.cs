using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam;
using ExtryzeDLL.IO;
using ExtryzeDLL.Native;

namespace ExtryzeDLL.RTE.H2Vista
{
    /// <summary>
    /// A real-time editing provider which connects to Halo 2 Vista.
    /// </summary>
    public class H2VistaRTEProvider : IRTEProvider
    {
        /// <summary>
        /// Constructs a new H2VistaRTEProvider.
        /// </summary>
        /// <param name="exeName">The name of the executable to connect to.</param>
        public H2VistaRTEProvider(string exeName)
        {
            EXEName = exeName;
        }

        /// <summary>
        /// Gets or sets the name of the executable to connect to.
        /// </summary>
        public string EXEName { get; set; }

        /// <summary>
        /// The type of connection that the provider will establish.
        /// Always RTEConnectionType.LocalProcess.
        /// </summary>
        public RTEConnectionType ConnectionType
        {
            get { return RTEConnectionType.LocalProcess; }
        }

        /// <summary>
        /// Obtains a stream which can be used to read and write a cache file's meta in realtime.
        /// The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
        /// </summary>
        /// <param name="cacheFile">The cache file to get a stream for.</param>
        /// <returns>The stream if it was opened successfully, or null otherwise.</returns>
        public IStream GetMetaStream(ICacheFile cacheFile)
        {
            Process gameProcess = FindGameProcess();
            if (gameProcess == null)
                return null;

            ProcessMemoryStream gameMemory = new ProcessMemoryStream(gameProcess);
            H2VistaMapPointerReader mapInfo = new H2VistaMapPointerReader(gameMemory);

            long metaAddress;
            if (cacheFile.Info.Type != CacheFileType.Shared)
            {
                metaAddress = mapInfo.CurrentMetaAddress;

                // The map isn't shared, so make sure the map names match
                if (mapInfo.MapName != cacheFile.Info.InternalName)
                {
                    gameMemory.Close();
                    return null;
                }
            }
            else
            {
                metaAddress = mapInfo.SharedMetaAddress;

                // Make sure the shared and current map pointers are different,
                // or that the current map is the shared map
                if (mapInfo.MapType != CacheFileType.Shared && mapInfo.CurrentMetaAddress == mapInfo.SharedMetaAddress)
                {
                    gameMemory.Close();
                    return null;
                }
            }

            OffsetStream metaStream = new OffsetStream(gameMemory, metaAddress - cacheFile.Info.VirtualBaseAddress);
            return new EndianStream(metaStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
        }

        private Process FindGameProcess()
        {
            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(EXEName));
            if (processes.Length > 0)
                return processes[0]; // Just take the first process that was found for now...
            return null;
        }
    }
}
