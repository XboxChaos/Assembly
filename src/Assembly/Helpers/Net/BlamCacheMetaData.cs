using System;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization;
using Assembly.Helpers.Caching;
namespace Assembly.Helpers.Net
{
	/// <summary>
	/// A server request to request new cached data
	/// </summary>
	[DataContract]
	public class CacheDataRequest : ServerRequest
	{
		public CacheDataRequest()
			: base("cache_data")
		{
		}

		/// <summary>
		/// The current timestamp of the cached data
		/// </summary>
		[DataMember(Name = "timestamp")]
		public ulong UnixTimestamp { get; set; }

		/// <summary>
		/// The type of cached data to pull from the server
		/// </summary>
		[DataMember(Name = "type")]
		public string Type { get; set; }
	}

	[DataContract]
	public class MetaContentResponse : ServerResponse
	{
		[DataMember(Name = "update_cache")]
		public bool UpdateCache { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	[DataContract]
	public class MetaContentModel
	{
		[DataMember(Name = "Type")]
		public string Type { get; set; }

		[DataMember(Name = "GeneratedTypestamp")]
		public ulong GeneratedTimestamp { get; set; }

		[DataMember(Name = "Games")]
		public GameEntry[] Games { get; set; }


		[DataContract]
		public class GameEntry
		{
			[DataMember(Name = "Targets")]
			public string Targets { get; set; }

			[DataMember(Name = "MetaData")]
			public MetaDataEntry[] MetaData { get; set; }


			[DataContract]
			public class MetaDataEntry
			{
				[DataMember(Name = "English_Name")]
				public string EnglishName { get; set; }

				[DataMember(Name = "English_Desc")]
				public string EnglishDesc { get; set; }

				[DataMember(Name = "InternalName")]
				public string InternalName { get; set; }

				[DataMember(Name = "PhysicalName")]
				public string PhysicalName { get; set; }

				[DataMember(Name = "MapId")]
				public int MapId { get; set; }

				[DataMember(Name = "ImageMetaData")]
				public ImageMetaDataEntry ImageMetaData { get; set; }


				[DataContract]
				public class ImageMetaDataEntry
				{
					[DataMember(Name = "Large")]
					public string Large { get; set; }

					[DataMember(Name = "Small")]
					public string Small { get; set; }
				}
			}
		}
	}

	public class BlamCacheMetaData
	{
		public static async void BeginCachingData()
		{
			var blamcacheFolderPath = VariousFunctions.GetApplicationLocation() + "Meta\\BlamCache\\";
			var blamcacheFilePath = blamcacheFolderPath + "content.aidf";

			try
			{

				// Look for current cached data
				ulong timestamp = 0;
				var type = "cache_meta_content";

				#region Get Current Cached Data
				if (!Directory.Exists(blamcacheFolderPath))
					Directory.CreateDirectory(blamcacheFolderPath);

				if (File.Exists(blamcacheFilePath))
				{
					var cachedData = JsonConvert.DeserializeObject<MetaContentModel>(File.ReadAllText(blamcacheFilePath));
					timestamp = cachedData.GeneratedTimestamp;
					type = cachedData.Type;
				}

				#endregion
				var request = new CacheDataRequest
								  {
									  UnixTimestamp = timestamp,
									  Type = type
								  };
				var response = AssemblyServer.SendRequest<CacheDataRequest, MetaContentResponse>(request);

				if (response != null && response.UpdateCache)
				{
					var blam_cache =
						await
						HttpRequests.SendBasicGetRequest(new Uri("http://assembly.xboxchaos.com/api/assets/cache_meta_content" + ".aidf"));

					// Write new Data
					File.WriteAllText(blamcacheFilePath, new StreamReader(blam_cache).ReadToEnd());
				}

				// Store cache in application
				CachingManager.BlamCacheMetaData =
					JsonConvert.DeserializeObject<MetaContentModel>(File.ReadAllText(blamcacheFilePath));
			}
			catch { }

			if (CachingManager.BlamCacheMetaData == null || CachingManager.BlamCacheMetaData.Games == null) return;
			// Start background image downloading
			foreach (var metadataEntry in CachingManager.BlamCacheMetaData.Games.SelectMany(game => game.MetaData))
			{
				try
				{
					var downloadLarge = false;
					var downloadSmall = false;

					var serverPath = "";
					var serverPathSmall = "";
					var localPath = "";
					var localPathSmall = "";

					if (!File.Exists(blamcacheFolderPath + metadataEntry.ImageMetaData.Large))
					{
						downloadLarge = true;
						serverPath = string.Format("http://assembly.xboxchaos.com/api/assets/{0}",
						                           metadataEntry.ImageMetaData.Large.Replace("\\", "/"));
						localPath = string.Format("{0}\\{1}", blamcacheFolderPath, metadataEntry.ImageMetaData.Large);
					}
					if (!File.Exists(blamcacheFolderPath + metadataEntry.ImageMetaData.Small))
					{
						downloadSmall = true;
						serverPathSmall = string.Format("http://assembly.xboxchaos.com/api/assets/{0}",
						                                metadataEntry.ImageMetaData.Small.Replace("\\", "/"));
						localPathSmall = string.Format("{0}\\{1}", blamcacheFolderPath, metadataEntry.ImageMetaData.Small);
					}

					if (!downloadLarge && !downloadSmall) continue;

					var imageDirectory = Path.GetDirectoryName(localPath == "" ? localPathSmall : localPath);
					if (imageDirectory == null) continue;

					if (!Directory.Exists(imageDirectory))
						Directory.CreateDirectory(imageDirectory);

					Stream imageStream;
					byte[] imageByteArray;

					// Large
					if (downloadLarge)
					{
						imageStream = await HttpRequests.SendBasicGetRequest(new Uri(serverPath));
						imageByteArray = VariousFunctions.StreamToByteArray(imageStream);
						File.WriteAllBytes(localPath, imageByteArray);
					}

					// Small
					if (downloadSmall)
					{
						imageStream = await HttpRequests.SendBasicGetRequest(new Uri(serverPathSmall));
						imageByteArray = VariousFunctions.StreamToByteArray(imageStream);
						File.WriteAllBytes(localPathSmall, imageByteArray);
					}
				}
				catch { }
			}
		}
	}
}