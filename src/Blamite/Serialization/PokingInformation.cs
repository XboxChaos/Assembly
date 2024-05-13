namespace Blamite.Serialization
{
	public class PokingInformation
	{
		public long? HeaderPointer { get; private set; }

		public long? HeaderAddress { get; private set; }
		public long? MagicAddress { get; private set; }
		public long? MagicOffset { get; private set; }

		public long? SharedMagicAddress { get; private set; }

		public string VersionString { get; private set; }
		public long? VersionAddress { get; private set; }
		public long? LastTagIndexAddress { get; private set; }
		public long? IndexArrayPointer { get; private set; }
		public long? AddressArrayPointer { get; private set; }

		public PokingInformation(long headerPointer, long headerAddress, long magicAddress, long magicOffset, long sharedMagicAddress,
			string versionString, long versionAddress, long lastTagIndexAddress, long indexArrayPointer, long addressArrayPointer)
		{
			if (headerPointer != -1) HeaderPointer = headerPointer;
			if (headerAddress != -1) HeaderAddress = headerAddress;
			if (magicAddress != -1) MagicAddress = magicAddress;
			if (magicOffset != -1) MagicOffset = magicOffset;
			if (sharedMagicAddress != -1) SharedMagicAddress = sharedMagicAddress;

			VersionString = versionString;
			if (versionAddress != -1) VersionAddress = versionAddress;
			if (lastTagIndexAddress != -1) LastTagIndexAddress = lastTagIndexAddress;
			if (indexArrayPointer != -1) IndexArrayPointer = indexArrayPointer;
			if (addressArrayPointer != -1) AddressArrayPointer = addressArrayPointer;
		}
	}
}
