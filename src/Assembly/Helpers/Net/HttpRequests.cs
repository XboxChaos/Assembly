using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Assembly.Helpers.Net
{
	public static class HttpRequests
	{
		public static Stream SendBasicGetRequest(Uri webUri)
		{
			try
			{
				var request = (HttpWebRequest)WebRequest.Create(webUri);
				request.Method = HttpMethod.Get;

				var response = request.GetResponse();
				return response != null ? response.GetResponseStream() : null;
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
