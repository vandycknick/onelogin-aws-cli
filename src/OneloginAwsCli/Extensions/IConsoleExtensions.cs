using System.CommandLine;

namespace OneloginAwsCli.Extensions
{
    public static class IConsoleExtensions
    {
        public static void Write(this IConsole console, string value) =>
            console.Out.Write(value);

        public static void WriteLine(this IConsole console, string value) =>
            console.Out.Write($"{value}\n");

        // Very cheeky way to log some of the intermediate responses for debugging purposes 😅
        public static void WriteLineIf(this IConsole console, string value, bool assertion)
        {
            if (assertion)
            {
                console.WriteLine(value);
            }
        }
    }
}
