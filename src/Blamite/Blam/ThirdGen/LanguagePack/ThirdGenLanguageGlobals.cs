using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.LanguagePack;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.LanguagePack
{
	/// <summary>
	///     Loads language data from MATG and PATG tags.
	/// </summary>
	public class ThirdGenLanguageGlobals
	{
		public ThirdGenLanguageGlobals(StructureValueCollection values, FileSegmenter segmenter,
			IPointerConverter localePointerConverter, EngineDescription buildInfo)
		{
			LocaleArea = new FileSegmentGroup(localePointerConverter);
			Languages = LoadLanguages(values, segmenter, buildInfo);
		}

		/// <summary>
		///     The locale area that was loaded.
		/// </summary>
		public FileSegmentGroup LocaleArea { get; private set; }

		/// <summary>
		///     The languages that were loaded.
		/// </summary>
		public List<ThirdGenLanguage> Languages { get; private set; }

		/// <summary>
		///     Serializes the language data into a StructureValueCollection.
		/// </summary>
		/// <returns>The StructureValueCollection that was created from the language table.</returns>
		public StructureValueCollection Serialize()
		{
			var languageSet = new StructureValueCollection[Languages.Count];
			for (int i = 0; i < Languages.Count; i++)
				languageSet[i] = Languages[i].Serialize();

			var result = new StructureValueCollection();
			result.SetArray("languages", languageSet);
			return result;
		}

		private List<ThirdGenLanguage> LoadLanguages(StructureValueCollection values, FileSegmenter segmenter,
			EngineDescription buildInfo)
		{
			StructureValueCollection[] languageSet = values.GetArray("languages");

			IEnumerable<ThirdGenLanguage> result =
				languageSet.Select((l, i) => new ThirdGenLanguage((GameLanguage) i, l, segmenter, LocaleArea, buildInfo));
			return result.ToList();
		}
	}
}