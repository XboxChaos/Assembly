using Blamite.IO;

/// <summary>
/// A tag interop inside a cache file.
/// </summary>
public interface ITagInterop
{
	uint Pointer { get; set; }

	int Type { get; set; }
}