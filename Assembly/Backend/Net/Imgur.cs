using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace Assembly.Backend.Net
{
    public static class Imgur
    {
        /// <summary>
        /// Uploads an image file to Imgur.
        /// </summary>
        /// <param name="filePath">The path of the file to upload.</param>
        /// <returns></returns>
        public static string PostImage(string filePath)
        {
            try
            {
                FileStream fileStream = File.OpenRead(filePath);
                byte[] imageData = new byte[fileStream.Length];
                fileStream.Read(imageData, 0, imageData.Length);
                fileStream.Close();

                const int MAX_URI_LENGTH = 32766;
                string base64img = System.Convert.ToBase64String(imageData);
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < base64img.Length; i += MAX_URI_LENGTH)
                {
                    sb.Append(Uri.EscapeDataString(base64img.Substring(i, Math.Min(MAX_URI_LENGTH, base64img.Length - i))));
                }

                string uploadRequestString = "image=" + sb.ToString() + "&key=" + IMGUR_DEVLEOPER_API_KEY;

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("http://api.imgur.com/2/upload.json");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ServicePoint.Expect100Continue = false;

                StreamWriter streamWriter = new StreamWriter(webRequest.GetRequestStream());
                streamWriter.Write(uploadRequestString);
                streamWriter.Close();

                WebResponse response = webRequest.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader responseReader = new StreamReader(responseStream);

                string responseString = responseReader.ReadToEnd();

                JavaScriptSerializer jss = new JavaScriptSerializer();
                if (responseString.Contains("\"error\":"))
                {
                    // Error
                    ImgurErrorResponse imgurResponse = jss.Deserialize<ImgurErrorResponse>(responseString);

                    return imgurResponse.error.message;
                }
                else
                {
                    // Successful
                    ImgurResponse imgurResponse = jss.Deserialize<ImgurResponse>(responseString);

                    return imgurResponse.upload.image.hash;
                }
            }
            catch
            {
                return null;
            }
        }

        class ImgurErrorResponse
        {
            public Error error = new Error();

            public class Error
            {
                public string message { get; set; }
                public string request { get; set; }
                public string method { get; set; }
                public string format { get; set; }
                public string parameters { get; set; }
            }
        }

        class ImgurResponse
        {
            public Upload upload = new Upload();

            public class Upload
            {
                public Image image = new Image();
                public Links links = new Links();

                public class Image
                {
                    public string name { get; set; }
                    public string title { get; set; }
                    public string caption { get; set; }
                    public string hash { get; set; }
                    public string deletehash { get; set; }
                    public string datetime { get; set; }
                    public string type { get; set; }
                    public string animated { get; set; }
                    public int width { get; set; }
                    public int height { get; set; }
                    public int size { get; set; }
                    public int views { get; set; }
                    public int bandwidth { get; set; }
                }
                public class Links
                {
                    public string original { get; set; }
                    public string imgur_page { get; set; }
                    public string delete_page { get; set; }
                    public string small_square { get; set; }
                    public string large_thumbnail { get; set; }
                }
            }
        }

        private const string IMGUR_DEVLEOPER_API_KEY = "94354bed4dbb51b33c8bc1eb38007680";
    }
}
