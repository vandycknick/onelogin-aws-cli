using System;
using OneloginAwsCli.Models;

namespace OneloginAwsCli.Exceptions
{
    public class MissingRequiredSettingsException : Exception
    {
        public MissingRequiredSettingsException() : base() { }
        public MissingRequiredSettingsException(string message) : base(message) { }
        public MissingRequiredSettingsException(string message, Exception inner) : base(message, inner) { }

        public Settings Settings { get; set; }

    }
}
