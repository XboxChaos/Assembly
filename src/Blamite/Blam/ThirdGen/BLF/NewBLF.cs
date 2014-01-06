using System;
using System.Collections.Generic;
using System.IO;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.BLF
{
	public class NewBLF
	{
		public void NewMapInfo(string file, MapInfo.GameIdentifier game)
		{
			int levlLength = 0;

			var stream = new EndianStream(File.Create(file), Endian.BigEndian);

			// _blf chunk
			stream.WriteAscii("_blf");
			stream.BaseStream.Position = 4;
			stream.WriteInt32(0x30);
			stream.WriteInt16(1);
			stream.WriteInt16(2);
			stream.WriteInt16(-2);
			byte[] blfBytes = new byte[0x22];
			stream.WriteBlock(blfBytes);

			// levl chunk
			stream.WriteAscii("levl");
			stream.BaseStream.Position = 0x34;
			if (game == MapInfo.GameIdentifier.Halo3)
			{
				stream.WriteInt32(0x4D50);
				stream.WriteInt16(3);
				levlLength = 0x4D50;
			}
			if (game == MapInfo.GameIdentifier.Halo3ODST)
			{
				stream.WriteInt32(0x98C0);
				stream.WriteInt16(3);
				levlLength = 0x98C0;
			}
			if (game == MapInfo.GameIdentifier.HaloReachBetas)
			{
				stream.WriteInt32(0xCC88);
				stream.WriteInt16(5);
				levlLength = 0xCC88;
			}
			if (game == MapInfo.GameIdentifier.HaloReach)
			{
				stream.WriteInt32(0xCC98);
				stream.WriteInt16(7);
				levlLength = 0xCC98;
			}
			if (game == MapInfo.GameIdentifier.Halo4NetworkTest)
			{
				stream.WriteInt32(0xCC98);
				stream.WriteInt16(8);
				levlLength = 0xCC98;
			}
			if (game == MapInfo.GameIdentifier.Halo4)
			{
				stream.WriteInt32(0x011DD8);
				stream.WriteInt16(9);
				levlLength = 0x11DD8;
			}
			stream.WriteInt16(1);
			byte[] levlBytes = new byte[levlLength - 0xC];
			stream.WriteBlock(levlBytes);

			// _eof chunk
			stream.WriteAscii("_eof");
			stream.BaseStream.Position = levlLength + 0x34;
			stream.WriteInt32(0x111);
			stream.WriteInt16(1);
			stream.WriteInt16(1);
			stream.WriteInt32(levlLength + 0x30);
			stream.WriteByte(3);
			byte[] eofBytes = new byte[0x100];
			stream.WriteBlock(eofBytes);

			stream.Close();
		}
	}
}
