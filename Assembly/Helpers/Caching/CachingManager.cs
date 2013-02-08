using System.Linq;
using Assembly.Helpers.Net;

namespace Assembly.Helpers.Caching
{
	public class CachingManager
	{
		public static MetaContentModel BlamCacheMetaData = null;

		public static MetaContentModel.GameEntry.MetaDataEntry GetMapMetaData(string gameIdentifier, 
			string mapInternalName)
		{
			if (BlamCacheMetaData == null || BlamCacheMetaData.Games.Length == 0) return null;

			// Get Target Game
			var selectedGameEntry = (from game in BlamCacheMetaData.Games 
									 from target in game.Targets.Split('|') 
									 where target == gameIdentifier 
									 select game).FirstOrDefault();
			if (selectedGameEntry == null) return null;

			// Get Target Metadata
			var selectedMetaData = (from metadata in selectedGameEntry.MetaData
			                        where metadata.InternalName == mapInternalName
			                        select metadata).FirstOrDefault();
			return selectedMetaData;
		}
	}
}
