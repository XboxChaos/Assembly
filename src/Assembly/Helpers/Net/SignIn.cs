using System.Runtime.Serialization;
using Assembly.Helpers.Cryptography;

namespace Assembly.Helpers.Net
{
    /// <summary>
    /// A server request to validate a user's login information.
    /// </summary>
    [DataContract]
    public class SignInRequest : ServerRequest
    {
        public SignInRequest()
            : base("sign_in")
        {
        }

        /// <summary>
        /// The username of the user attempting to log in.
        /// </summary>
        [DataMember(Name = "username")]
        public string Username { get; set; }

        /// <summary>
        /// The MD5 digest of the user's password. Must be in lowercase.
        /// </summary>
        [DataMember(Name = "password")]
        public string PasswordMd5 { get; set; }
    }

    /// <summary>
    /// The response from a SignInRequest.
    /// </summary>
    [DataContract]
    public class SignInResponse : ServerResponse
    {
        /// <summary>
        /// The member's ID.
        /// </summary>
        [DataMember(Name = "member_id")]
        public int MemberId { get; set; }

        /// <summary>
        /// The session ID which can be used in future requests.
        /// </summary>
        [DataMember(Name = "session_id")]
        public string SessionId { get; set; }

        /// <summary>
        /// The member's display name.
        /// </summary>
        [DataMember(Name = "display_name")]
        public string DisplayName { get; set; }

        /// <summary>
        /// The name the member uses to sign in with.
        /// </summary>
        [DataMember(Name = "signin_name")]
        public string SignInName { get; set; }

        /// <summary>
        /// The member's internal group ID.
        /// </summary>
        [DataMember(Name = "group_id")]
        public int GroupId { get; set; }

        /// <summary>
        /// The member's post count.
        /// </summary>
        [DataMember(Name = "post_count")]
        public int PostCount { get; set; }

        /// <summary>
        /// A URL to the member's avatar. Can be null, empty, or invalid.
        /// </summary>
        [DataMember(Name = "avatar_url")]
        public string AvatarUrl { get; set; }
    }

    public static class SignIn
    {
        /// <summary>
        /// Attempts to validate a user's login information.
        /// </summary>
        /// <param name="username">The username to log in with.</param>
        /// <param name="password">The password (in plain-text) to log in with.</param>
        /// <returns>The server's response. Use the response's Successful property to determine if the information is valid.</returns>
        public static SignInResponse AttemptSignIn(string username, string password)
        {
			var request = new SignInRequest
				              {
					              Username = username, 
								  PasswordMd5 = Md5Crypto.ComputeHashToString(password).ToLower()
				              };
	        return AssemblyServer.SendRequest<SignInRequest, SignInResponse>(request);
        }
    }
}
