#if NET45

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Serialization;

namespace Blamite.RTE.H1PC
{
    public class H1PCRTEProvider : IRTEProvider
    {
        private readonly EngineDescription _buildInfo;

        public H1PCRTEProvider(EngineDescription engine)
        {
            _buildInfo = engine;
            ExecutableName = _buildInfo.GameExecutable;
        }

        public string ExecutableName { get; set; }

        public RTEConnectionType ConnectionType
        {
            get { return RTEConnectionType.LocalProcess; }
        }

        public IStream GetMetaStream(ICacheFile cacheFile)
        {
            if (string.IsNullOrEmpty(ExecutableName))
                throw new InvalidOperationException("No gameExecutable value found in Engines.xml for engine " + _buildInfo.Name + ".");
            if (_buildInfo.Poking == null)
                throw new InvalidOperationException("No poking definitions found in Engines.xml for engine " + _buildInfo.Name + ".");

            Process gameProcess = FindGameProcess();
            if (gameProcess == null)
                return null;

            string version = gameProcess.MainModule.FileVersionInfo.FileVersion;
            long pointer = _buildInfo.Poking.RetrievePointer(version);
            if (pointer == -1)
                throw new InvalidOperationException("Game version " + version + " does not have a pointer defined in the Formats folder.");

            var gameMemory = new ProcessMemoryStream(gameProcess);
            var mapInfo = new H1PCMapPointerReader(gameMemory, pointer);

            long metaAddress;
            if (cacheFile.Type != CacheFileType.Shared)
            {
                metaAddress = mapInfo.CurrentMetaAddress;

                // The map isn't shared, so make sure the map names match
                if (mapInfo.MapName != cacheFile.InternalName)
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

            var metaStream = new OffsetStream(gameMemory, metaAddress - cacheFile.MetaArea.BasePointer);
            return new EndianStream(metaStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
        }

        private Process FindGameProcess()
        {
            Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ExecutableName));
            return processes.Length > 0 ? processes[0] : null;
        }
    }
}

#endif