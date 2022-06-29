namespace Blamite.Serialization
{
	public class PokingInformation
	{
		public long? HeaderPointer { get; private set; }

		public long? HeaderAddress { get; private set; }
		public long? MagicAddress { get; private set; }
		public long? MagicOffset { get; private set; }

		public long? SharedMagicAddress { get; private set; }

		public PokingInformation(long headerPointer, long headerAddress, long magicaddress, long magicoffset, long sharedmagicaddress)
		{
			if (headerPointer != -1) HeaderPointer = headerPointer;
			if (headerAddress != -1) HeaderAddress = headerAddress;
			if (magicaddress != -1) MagicAddress = magicaddress;
			if (magicoffset != -1) MagicOffset = magicoffset;
			if (sharedmagicaddress != -1) SharedMagicAddress = sharedmagicaddress;
		}
	}
}
