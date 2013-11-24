using Blamite.IO;

namespace Blamite.Blam
{
	/// <summary>
	///     Retrieves engine version information from a cache file.
	/// </summary>
	public class CacheFileVersionInfo
	{
		private const int SecondGenVersion = 8;
		private const int ThirdGenVersion = 9;

		public CacheFileVersionInfo(IReader reader)
		{
			reader.SeekTo(0x4);
			Version = reader.ReadInt32();

			if (Version == SecondGenVersion)
			{
				Engine = EngineType.SecondGeneration;

				// Read second-generation build string
				reader.SeekTo(0x12C);
				BuildString = reader.ReadAscii();
			}
			else if (Version >= ThirdGenVersion)
			{
				Engine = EngineType.ThirdGeneration;

				// Read third-generation build string
				reader.SeekTo(0x11C);
				BuildString = reader.ReadAscii();
			}

			if (string.IsNullOrWhiteSpace(BuildString))
			{
				// Assume it's a first-generation build
				Engine = EngineType.FirstGeneration;
				Version = 0;

				// Read first-generation build string
				reader.SeekTo(0x40);
				BuildString = reader.ReadAscii();
			}
		}

		/// <summary>
		///     The version number stored in the file header (if there is one).
		/// </summary>
		public int Version { get; private set; }

		/// <summary>
		///     The engine type the map was built for.
		/// </summary>
		public EngineType Engine { get; private set; }

		/// <summary>
		///     The engine build version string stored in the file.
		/// </summary>
		public string BuildString { get; private set; }
	}
}