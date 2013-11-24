using Blamite.IO;

namespace Blamite.Patching
{
	public static class OldPatchLoader
	{
		public static Patch LoadPatch(IReader reader, bool isAlteration)
		{
			var patch = new Patch();
			var change = new SegmentChange(0, 0, 0, 0, true);
			patch.Author = "Ascension/Alteration Patch";
			patch.Description = "Ascension/Alteration Patch";
			if (isAlteration)
			{
				//do shitty alteration stuff
				byte authorLength = reader.ReadByte();
				patch.Author = reader.ReadAscii(authorLength);
				byte descLength = reader.ReadByte();
				patch.Description = reader.ReadAscii(descLength);
			}
			//create ascension patch object and change segment

			while (!reader.EOF)
			{
				//get valuable info
				uint segmentOffset = reader.ReadUInt32();
				int segmentSize = reader.ReadInt32();
				byte[] segmentData = reader.ReadBlock(segmentSize);

				//Add change data

				change.DataChanges.Add(new DataChange(segmentOffset, segmentData));
			}
			patch.SegmentChanges.Add(change);
			return patch;
		}
	}
}