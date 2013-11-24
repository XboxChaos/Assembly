using System;
using Blamite.Blam;
using Blamite.IO;

namespace Blamite.Patching
{
	public static class PatchApplier
	{
		/// <summary>
		///     Applies a patch to a cache file.
		/// </summary>
		/// <param name="patch">The patch to apply.</param>
		/// <param name="cacheFile">The cache file to apply the patch to. After the patch is applied, it may have to be reloaded.</param>
		/// <param name="stream">The stream to write changes to.</param>
		public static void ApplyPatch(Patch patch, ICacheFile cacheFile, IStream stream)
		{
			if (patch.MapInternalName != cacheFile.InternalName)
				throw new ArgumentException("The patch is for a different cache file. Expected \"" + patch.MapInternalName +
				                            "\" but got \"" + cacheFile.InternalName + "\".");

			SegmentPatcher.PatchSegments(patch.SegmentChanges, stream);

			#region Deprecated

			DataPatcher.PatchData(patch.MetaChanges, (uint) -cacheFile.MetaArea.PointerMask, stream);
			if (patch.LanguageChanges.Count > 0)
			{
				LocalePatcher.WriteLanguageChanges(patch.LanguageChanges, cacheFile, stream);
				cacheFile.SaveChanges(stream);
			}

			#endregion
		}
	}
}