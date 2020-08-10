using System;

namespace OneloginAwsCli.Exceptions
{
    public class ConfigFileNotFoundException : Exception
    {
        public ConfigFileNotFoundException() : base() { }
        public ConfigFileNotFoundException(string message) : base(message) { }
        public ConfigFileNotFoundException(string message, Exception inner) : base(message, inner) { }
        public string FilePath { get; set; }
    }
}
