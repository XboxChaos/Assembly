using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Assembly.Helpers.Net
{
	public static class Imgur
	{
		private const string ImgurClientId = "ad80516b4f78d12";
		private const string ImgurClientSecret = "bc73c000b63fcb4dde1bed88d3d48c3f0dbd4cdd";
		private const int MaxUriLength = 32766;

		private static string DownloadString(Uri uri, string stringData)
		{
			try
			{
				var request = (HttpWebRequest) WebRequest.Create(uri);
				request.Method = HttpMethod.Post;
				request.Headers["Authorization"] = "Client-ID " + ImgurClientId;
				request.ContentType = "application/x-www-form-urlencoded";

				var streamW = new StreamWriter(request.GetRequestStream());
				streamW.Write(stringData);

				WebResponse response = request.GetResponse();
				if (response != null)
					using (var sr = new StreamReader(response.GetResponseStream()))
					{
						return sr.ReadToEnd();
					}
				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static string UploadToImgur(byte[] imageData)
		{
			string base64Img = Convert.ToBase64String(imageData);

			var sb = new StringBuilder();

			for (int i = 0; i < base64Img.Length; i += MaxUriLength)
			{
				sb.Append(Uri.EscapeDataString(base64Img.Substring(i, Math.Min(MaxUriLength, base64Img.Length - i))));
			}

			string base64 = sb.ToString();

			string uploadRequestString = "image=" + base64 +
			                             "&type=base64";

			string response = DownloadString(new Uri("https://api.imgur.com/3/image", UriKind.Absolute), uploadRequestString);
			if (response == null || response.Contains("\"error\":"))
				return null;
			// yay, :D
			var imgurResponse = JsonConvert.DeserializeObject<ImgurResponse>(response);
			return imgurResponse.data.id;
		}

		public class ImgurResponse
		{
			public Data data { get; set; }
			public bool success { get; set; }
			public string status { get; set; }

			public class Data
			{
				public string id { get; set; }
			}
		}
	}
}