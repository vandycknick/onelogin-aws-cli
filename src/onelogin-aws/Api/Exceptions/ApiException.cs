using System;
using System.Net;
using System.Text.Json;
using OneLoginAws.Api.Models;

namespace OneLoginAws.Api.Exceptions
{
    /// <summary>
    /// Represents errors that occur from the OneLogin API.
    /// </summary>
    public class ApiException : Exception
    {
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
            : this(GetApiErrorFromExceptionMessage(message), code, innerException)
        {

        }

        protected ApiException(ApiError apiError, HttpStatusCode statusCode, Exception? innerException)
            : base(null, innerException)
        {
            ApiError = apiError ?? throw new ArgumentNullException(nameof(apiError));
            StatusCode = statusCode;
        }

        /// <summary>
        /// The HTTP status code associated with the response
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }

        /// <summary>
        /// The raw exception payload from the response
        /// </summary>
        public ApiError ApiError { get; private set; }

        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            IgnoreNullValues = true,
            PropertyNameCaseInsensitive = true,
        };

        private static ApiError GetApiErrorFromExceptionMessage(string responseContent)
        {
            try
            {
                if (!string.IsNullOrEmpty(responseContent))
                {
                    return JsonSerializer.Deserialize<ApiError>(responseContent, _options) ?? new ApiError
                    {
                        Name = "",
                        StatusCode = 0,
                        Message = responseContent,
                    };
                }
            }
            catch (Exception)
            {
            }

            return new ApiError
            {
                Name = "",
                StatusCode = 0,
                Message = responseContent,
            };
        }
    }
}
