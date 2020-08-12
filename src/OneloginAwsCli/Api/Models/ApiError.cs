namespace OneloginAwsCli.Api.Models
{
    public class ApiError
    {
        public string Name { get; set; } = null!;
        public int StatusCode { get; set; }
        public string Message { get; set; } = null!;
    }
}
