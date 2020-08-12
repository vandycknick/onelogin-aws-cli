using System.IO;

namespace OneLoginAws.Console
{
    public class SystemConsole : IConsole
    {
        public SystemConsole()
        {
            In = StandardStreamReader.Create(System.Console.In, System.Console.ReadKey, StandardStreamHandle.IN);
            Out = StandardStreamWriter.Create(System.Console.Out, StandardStreamHandle.OUT);
            Error = StandardStreamWriter.Create(System.Console.Error, StandardStreamHandle.ERROR);
        }

        public IStandardStreamReader In { get; }
        public IStandardStreamWriter Out { get; }
        public IStandardStreamWriter Error { get; }
        public bool IsInputRedirected => System.Console.IsInputRedirected;
    }
}
