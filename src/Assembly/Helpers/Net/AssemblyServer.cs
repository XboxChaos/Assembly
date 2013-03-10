using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Assembly.Helpers.Net
{
    /// <summary>
    /// The base class for server commands.
    /// </summary>
    [DataContract]
    public class ServerRequest
    {
        public ServerRequest(string action)
        {
            Action = action;
        }

        /// <summary>
        /// The action string to send to the server.
        /// </summary>
        [DataMember(Name = "action")]
        public string Action { get; set; }
    }

    /// <summary>
    /// The base class for server responses.
    /// </summary>
    [DataContract]
    public class ServerResponse
    {
        /// <summary>
        /// The time that the server sent the response.
        /// </summary>
        [DataMember(Name = "timestamp")]
        public long Timestamp { get; set; }

        /// <summary>
        /// The resulting error code, or -1 if the request was successful.
        /// </summary>
        [DataMember(Name = "error_code")]
        public int ErrorCode { get; set; }

        /// <summary>
        /// The message associated with the error code.
        /// </summary>
        [DataMember(Name = "error_description")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// True if the server request was successful.
        /// </summary>
        public bool Successful
        {
            get { return ErrorCode == -1; }
        }
    }

    public static class AssemblyServer
    {
        /// <summary>
        /// Sends a request object to the server and retrieves the response.
        /// </summary>
        /// <typeparam name="TRequestType">The type of the request object that should be sent. This must inherit from ServerRequest.</typeparam>
        /// <typeparam name="TResultType">The type of the result object that should be returned. This must inherit from ServerResponse.</typeparam>
        /// <param name="commandObj">The command object of type CommandType to send.</param>
        /// <returns>The server's response, or null if the request failed.</returns>
        public static TResultType SendRequest<TRequestType, TResultType>(TRequestType commandObj)
            where TRequestType : ServerRequest
            where TResultType : ServerResponse
        {
            // Serialize the request object to a MemoryStream
			var cmdSerializer = new DataContractJsonSerializer(typeof(TRequestType));
			var json = new MemoryStream();
            cmdSerializer.WriteObject(json, commandObj);

            // POST it to the server
			var request = (HttpWebRequest)WebRequest.Create(AssemblyEndpoint);
            request.Method = "POST";
            request.ContentType = "application/octet-stream";
            request.ContentLength = json.Length;
            Stream dataStream = null;
            try
            {
                dataStream = request.GetRequestStream();
                dataStream.Write(json.GetBuffer(), 0, (int)json.Length);
            }
            catch (WebException)
            {
                return null;
            }
            finally
            {
                if (dataStream != null)
                    dataStream.Close();
                json.Close();
            }

            // Retrieve the response and open a stream to it
            WebResponse response = null;
            try
            {
                response = request.GetResponse();
                dataStream = response.GetResponseStream();
            }
            catch (WebException)
            {
                if (response != null)
                    response.Close();
                return null;
            }
            if (dataStream == null)
                return null;

            // Deserialize the response
			var resultSerializer = new DataContractJsonSerializer(typeof(TResultType));
			var result = resultSerializer.ReadObject(dataStream) as TResultType;
	        dataStream.Close();
	        response.Close();
	        return result;
        }

        private const string AssemblyEndpoint = "https://www.xboxchaos.com/assembly/api/hub.php";
    }
}
