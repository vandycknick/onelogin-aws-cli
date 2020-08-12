using System;

namespace OneloginAwsCli.Exceptions
{
    public class ConfigFileNotFoundException : Exception
    {
        public ConfigFileNotFoundException(string filePath) : base($"Config file not found: {filePath}", null)
        {
            FilePath = filePath;
        }

        public string FilePath { get; set; }
    }
}
