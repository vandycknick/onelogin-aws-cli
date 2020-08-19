using System;
using System.Net;
using OneLoginApi.Models;

namespace OneLoginApi.Exceptions
{
    /// <summary>
    /// Represents errors that occur from the OneLogin API.
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>
        /// Constructs an instance of ApiException
        /// </summary>
        /// <param name="error">The error returned from the api</param>
        public ApiException(ApiError error) : this(error.Message, (HttpStatusCode)error.StatusCode)
        {
            Error = error;
        }

        /// <summary>
        /// Constructs an instance of ApiException
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="httpStatusCode">The HTTP status code from the response</param>
        public ApiException(string message, HttpStatusCode code)
            : this(message, code, null)
        {

        }

        /// <summary>
        /// Constructs an instance of ApiException
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="httpStatusCode">The HTTP status code from the response</param>
        /// <param name="innerException">The inner exception</param>
        public ApiException(string message, HttpStatusCode code, Exception? innerException)
            : base(message, innerException)
        {
            StatusCode = code;
        }

        /// <summary>
        /// The HTTP status code associated with the response
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }

        /// <summary>
        /// The error returned from the api
        /// </summary>
        public ApiError? Error { get; set; }
    }
}
