using System;
using System.Net;
using OneLoginApi.Models;

namespace OneLoginApi.Exceptions
{
    /// <summary>
    /// Represents a HTTP 404 - Not Found response returned from the API.
    /// </summary>
    public class NotFoundException : ApiException
    {
        /// <summary>
        /// Constructs an instance of NotFoundException
        /// </summary>
        /// <param name="error">The error returned from the api</param>
        public NotFoundException(ApiError error) : this(error.Message)
        {
            Error = error;
        }

        /// <summary>
        /// Constructs an instance of NotFoundException
        /// </summary>
        /// <param name="message">Error response from the server</param>
        public NotFoundException(string message) : this(message, null)
        {
        }

        /// <summary>
        /// Constructs an instance of NotFoundException
        /// </summary>
        /// <param name="message">Error response from the server</param>
        /// <param name="innerException">The inner exception</param>
        public NotFoundException(string message, Exception? innerException) : base(message, HttpStatusCode.NotFound, innerException)
        {

        }
    }
}
