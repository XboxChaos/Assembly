using Blamite.IO;

/// <summary>
/// A simulation definition table in a cache file.
/// </summary>
public interface IPolyart
{
	uint Pointer { get; set; }

	int Type { get; set; }
}