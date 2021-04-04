using Spectre.Console;

namespace OneLoginAws.Extensions
{
    public static class AnsiConsoleExtensions
    {
        private const string ESCAPE = "\x1b[";
        private const string ERASE_LINE = "2K";

        public static void CursorUp(this IAnsiConsole console, int count = 1) =>
            console.Write($"{ESCAPE}{count}A");

        public static void EraseLine(this IAnsiConsole console) =>
            console.Write($"{ESCAPE}{ERASE_LINE}");
    }
}
