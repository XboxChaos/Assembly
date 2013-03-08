using Blamite.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Patching
{
    public static class OldPatchLoader
    {
        public static Patch LoadPatch(IReader reader, bool isAlteration)
        {

            Patch patch = new Patch();
            SegmentChange change = new SegmentChange(0, 0, 0, 0, true);
            patch.Author = "Ascension/Alteration Patch";
            patch.Description = "Ascension/Alteration Patch";
            if (isAlteration)
            {
                //do shitty alteration stuff
                byte authorLength = reader.ReadByte();
                patch.Author = reader.ReadAscii((int)authorLength);
                byte descLength = reader.ReadByte();
                patch.Description = reader.ReadAscii((int)descLength);
                
            }
            //create ascension patch object and change segment

            while (!reader.EOF)
            {
                //get valuable info
                var segmentOffset = reader.ReadUInt32();
                var segmentSize = reader.ReadInt32();
                var segmentData = reader.ReadBlock(segmentSize);

                //Add change data

                change.DataChanges.Add(new DataChange(segmentOffset, segmentData));
                
            }
            patch.SegmentChanges.Add(change);
            return patch;
        }
    }
}
