using ExtryzeDLL.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Patching
{
    public static class AscensionPatchLoader
    {
        public static Patch LoadPatch(IReader reader)
        {
            
            //create ascension patch object and change segment
            Patch patch = new Patch();
            SegmentChange change = new SegmentChange(0, 0, 0, 0, true);
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
