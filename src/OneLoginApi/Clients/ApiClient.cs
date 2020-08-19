using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OneLoginApi.Exceptions;
using OneLoginApi.Helpers;
using OneLoginApi.Models;

namespace OneLoginApi.Clients
{
    public class ApiClient
    {
        protected readonly JsonSerializerOptions _options;
        protected readonly JsonSerializerOptions _errorOptions;
        public ApiClient()
        {
            _options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = new SnakeCaseNamingPolicy()
            };

            _errorOptions = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
            };
        }

        protected virtual async Task EnsureApiRequestSuccess(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;

            var error = new ApiError
            {
                Name = "",
                StatusCode = 0,
                Message = "",
            };

            try
            {
                error = await response.ReadAsAsync<ApiError>(_errorOptions);
            }
            catch (Exception)
            {}

            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new AuthorizationException(error),
                HttpStatusCode.NotFound => new NotFoundException(error),
                _ => new ApiException(error),
            };
        }
    }
}
