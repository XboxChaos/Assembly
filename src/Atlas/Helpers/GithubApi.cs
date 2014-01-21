using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Atlas.Models.Github;
using Newtonsoft.Json;

namespace Atlas.Helpers
{
	public class GithubApi
	{
		private const string ApiV3Url = "https://api.github.com/repos/{0}/{1}/{2}";

		public static async Task<PluginRevisionHistory[]> GetRepoFileCommitHistory(string filePath, string owner, string repo)
		{
			filePath = filePath.Replace("\\", "/");

			var url = String.Format("{0}?path={1}", String.Format(ApiV3Url, owner, repo, "commits"),
				HttpUtility.UrlDecode(filePath));

			var jsonString = await GetResponse(url);
			return await JsonConvert.DeserializeObjectAsync<PluginRevisionHistory[]>(jsonString);
		}

		private static async Task<string> GetResponse(string url)
		{
			var httpClient = new HttpClient();
			var request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Add("KeepAlive", "true");
			request.Headers.Add("User-Agent", "Assembly-App");

			var response = await httpClient.SendAsync(request);
			return await response.Content.ReadAsStringAsync();
		}
	}
}
