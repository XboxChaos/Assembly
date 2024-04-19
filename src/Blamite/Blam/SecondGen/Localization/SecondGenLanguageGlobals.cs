using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Localization;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.SecondGen.Localization
{
	/// <summary>
	///     Loads language data from MATG tags.
	/// </summary>
	public class SecondGenLanguageGlobals
	{
		public SecondGenLanguageGlobals(StructureValueCollection values, FileSegmenter segmenter, EngineDescription buildInfo)
		{
			LocaleArea = new FileSegmentGroup();
			Languages = LoadLanguages(values, segmenter, buildInfo);
		}

		/// <summary>
		///     The locale area that was loaded.
		/// </summary>
		public FileSegmentGroup LocaleArea { get; private set; }

		/// <summary>
		///     The languages that were loaded.
		/// </summary>
		public List<SecondGenLanguage> Languages { get; private set; }

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

		//hax
		public static Dictionary<GameLanguage, int> LanguageRemaps = new Dictionary<GameLanguage, int>()
		{
			{ GameLanguage.English, 0 },
			{ GameLanguage.Japanese, 1 },
			{ GameLanguage.German, 2 },
			{ GameLanguage.French, 3 },
			{ GameLanguage.Spanish, 4 },
			{ GameLanguage.Italian, 5 },
			{ GameLanguage.Korean, 6 },
			{ GameLanguage.ChineseTrad, 7 },
			{ GameLanguage.Portuguese, 8 },
		};

		private List<SecondGenLanguage> LoadLanguages(StructureValueCollection values, FileSegmenter segmenter,
			EngineDescription buildInfo)
		{
			StructureValueCollection[] languageSet = values.GetArray("languages");

			IEnumerable<SecondGenLanguage> result =
				languageSet.Select((l, i) => new SecondGenLanguage(LanguageRemaps.Keys.ElementAt(i), l, segmenter, LocaleArea, buildInfo));
			return result.ToList();
		}

	}
}