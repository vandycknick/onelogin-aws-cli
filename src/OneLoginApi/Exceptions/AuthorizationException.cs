using System;
using System.Net;
using OneLoginApi.Models;

namespace OneLoginApi.Exceptions
{
    /// <summary>
    /// Represents a HTTP 401 - Unauthorized response returned from the API.
    /// </summary>
    public class AuthorizationException : ApiException
    {
        /// <summary>
        /// Constructs an instance of AuthorizationException
        /// </summary>
        /// <param name="error">The error returned from the api</param>
        public AuthorizationException(ApiError error) : this(error.Message)
        {
            Error = error;
        }

        /// <summary>
        /// Constructs an instance of AuthorizationException
        /// </summary>
        /// <param name="message">Error response from the server</param>
        public AuthorizationException(string message): this(message, null)
        {
        }

        /// <summary>
        /// Constructs an instance of AuthorizationException
        /// </summary>
        /// <param name="message">Error response from the server</param>
        /// <param name="innerException">The inner exception</param>
        public AuthorizationException(string message, Exception? innerException) : base(message, HttpStatusCode.Unauthorized, innerException)
        {
        }
    }
}
