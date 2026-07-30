using System;
using System.Collections.Generic;
using Blamite.Blam.FifthGen.Structures;

namespace Blamite.Blam.FifthGen
{
	/// <summary>
	///     Tag names for a Campaign Evolved tag namespace, built alongside its
	///     <see cref="Structures.FifthGenTagTable" /> from whichever of the sources in
	///     <see cref="FifthGenNameOrigin" /> was available for each tag.
	/// </summary>
	/// <remarks>
	///     There is nowhere to persist a rename back to - a mounted CE tag namespace is a folder of
	///     IoStore containers, not a single side file the way <see cref="Eldorado.CSVFilenameSource" />'s
	///     <c>tag_list.csv</c> is - so <see cref="SetTagName" /> only ever changes what this instance
	///     reports for the rest of the process's lifetime. <see cref="FifthGenCacheFile.SaveTagNames" />
	///     reflects that by throwing rather than silently discarding a rename it cannot keep.
	/// </remarks>
	public class FifthGenFileNameSource : FileNameSource
	{
		private readonly List<string> _names;
		private readonly List<FifthGenNameOrigin> _origins;

		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenFileNameSource" /> class.
		/// </summary>
		/// <param name="names">
		///     One name per tag, indexed the same way as the <see cref="Structures.FifthGenTagTable" />
		///     that produced it. Owned by this instance from here on.
		/// </param>
		/// <param name="origins">
		///     Parallel to <paramref name="names" />: where each name came from.
		/// </param>
		internal FifthGenFileNameSource(List<string> names, List<FifthGenNameOrigin> origins)
		{
			_names = names;
			_origins = origins;
		}

		public override string GetTagName(int tagIndex)
		{
			if (tagIndex < 0 || tagIndex >= _names.Count)
				return null;
			return _names[tagIndex];
		}

		public override void SetTagName(int tagIndex, string name)
		{
			if (tagIndex < 0 || tagIndex >= _names.Count)
				throw new ArgumentOutOfRangeException("tagIndex");
			_names[tagIndex] = name;

			// The name no longer came from any of the documented sources - it was overwritten by a
			// caller - so there is nothing truthful left to record for it.
			_origins[tagIndex] = FifthGenNameOrigin.None;
		}

		public override int FindName(string name)
		{
			return _names.IndexOf(name);
		}

		public override IEnumerator<string> GetEnumerator()
		{
			return _names.GetEnumerator();
		}

		/// <summary>
		///     Gets which of <see cref="FifthGenNameOrigin" />'s sources produced a tag's name.
		/// </summary>
		/// <param name="tagIndex">The index of the tag to look up.</param>
		/// <returns>The origin of the tag's name.</returns>
		public FifthGenNameOrigin GetNameOrigin(int tagIndex)
		{
			if (tagIndex < 0 || tagIndex >= _origins.Count)
				return FifthGenNameOrigin.None;
			return _origins[tagIndex];
		}
	}
}
