using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Assembly.Backend.Net
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
        /// <typeparam name="RequestType">The type of the request object that should be sent. This must inherit from ServerRequest.</typeparam>
        /// <typeparam name="ResultType">The type of the result object that should be returned. This must inherit from ServerResponse.</typeparam>
        /// <param name="commandObj">The command object of type CommandType to send.</param>
        /// <returns>The server's response.</returns>
        public static ResultType SendRequest<RequestType, ResultType>(RequestType commandObj)
            where RequestType : ServerRequest
            where ResultType : ServerResponse
        {
            // Serialize the request object to a MemoryStream
            DataContractJsonSerializer cmdSerializer = new DataContractJsonSerializer(typeof(RequestType));
            MemoryStream json = new MemoryStream();
            cmdSerializer.WriteObject(json, commandObj);

            // POST it to the server
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(AssemblyEndpoint);
            request.Method = "POST";
            request.ContentType = "application/octet-stream";
            request.ContentLength = json.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(json.GetBuffer(), 0, (int)json.Length);
            dataStream.Close();
            json.Close();

            // Retrieve the response and open a stream to it
            WebResponse response = request.GetResponse();
            dataStream = response.GetResponseStream();

            // Deserialize the response
            DataContractJsonSerializer resultSerializer = new DataContractJsonSerializer(typeof(ResultType));
            ResultType result = resultSerializer.ReadObject(dataStream) as ResultType;

            dataStream.Close();
            response.Close();

            return result;
        }

        private const string AssemblyEndpoint = "http://assembly.xboxchaos.com/api/hub.php";
    }
}
