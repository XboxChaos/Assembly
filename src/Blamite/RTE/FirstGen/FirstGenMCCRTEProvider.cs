using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Serialization;
using Blamite.Util;

namespace Blamite.RTE.FirstGen
{
    public class FirstGenMCCRTEProvider : MCCRTEProvider
    {

        public FirstGenMCCRTEProvider(EngineDescription engine) : base(engine) { }

        public override IStream GetMetaStream(ICacheFile cacheFile)
        {
            if (!CheckBuildInfo())
                return null;

            Process gameProcess = FindGameProcess();
            if (gameProcess == null)
                return null;

            //long pointer = RetrievePointer(gameProcess);
            PokingInformation info = RetrieveInformation(gameProcess);

            var gameMemory = new ProcessModuleMemoryStream(gameProcess, _buildInfo.GameModule);
            var mapInfo = new MapPointerReader(gameMemory, _buildInfo, info);

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
            //var metaStream = new OffsetStream(gameMemory, metaAddress);
            return new EndianStream(metaStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
        }
    }
}
