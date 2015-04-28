using System.Collections.Generic;
using System.Linq;
using Blamite.Blam.Localization;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.FourthGen.Localization
{
	/// <summary>
	///     Loads language data from MATG and PATG tags.
	/// </summary>
	public class FourthGenLanguageGlobals
	{
		public FourthGenLanguageGlobals(StructureValueCollection values, FileSegmenter segmenter,
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
		public List<FourthGenLanguage> Languages { get; private set; }

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

		private List<FourthGenLanguage> LoadLanguages(StructureValueCollection values, FileSegmenter segmenter,
			EngineDescription buildInfo)
		{
			StructureValueCollection[] languageSet = values.GetArray("languages");

            List<FourthGenLanguage> list = new List<FourthGenLanguage>();

            for(int i=0;i<languageSet.Length;i++) list.Add(new FourthGenLanguage((GameLanguage) i, languageSet[i], segmenter, LocaleArea, buildInfo));


			//IEnumerable<FourthGenLanguage> result = languageSet.Select((l, i) => new FourthGenLanguage((GameLanguage) i, l, segmenter, LocaleArea, buildInfo));
            //return result.ToList();
            return list;
		}
	}
}