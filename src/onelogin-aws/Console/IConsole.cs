using System.IO;

namespace OneLoginAws.Console
{
    public interface IConsole
    {
        IStandardStreamReader In { get; }
        IStandardStreamWriter Out { get; }
        IStandardStreamWriter Error { get; }

        bool IsInputRedirected { get; }
    }
}
