using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneloginAwsCli.Extensions
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response, JsonSerializerOptions options = default)
        {
            if (response.Content is object && response.Content.Headers.ContentType.MediaType == "application/json")
            {
                var contentStream = await response.Content.ReadAsStreamAsync();

                return await JsonSerializer.DeserializeAsync<T>(contentStream, options);
            }
            else
            {
                throw new Exception("HTTP Response was invalid and cannot be deserialised.");
            }
        }
    }
}