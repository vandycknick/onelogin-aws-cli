using System.IO;

namespace OneloginAwsCli.Console
{
    public interface IConsole
    {
        IStandardStreamReader In { get; }
        IStandardStreamWriter Out { get; }
        IStandardStreamWriter Error { get; }

        bool IsInputRedirected { get; }
    }
}
