using System;
using OneLoginAws.Console;

namespace OneLoginAws.Extensions
{
    public static class IConsoleExtensions
    {
        public static void Write(this IConsole console, string value) =>
            console.Out.Write(value);

        public static void WriteLine(this IConsole console) => console.Write(Environment.NewLine);

        public static void WriteLine(this IConsole console, string value)
        {
            console.Write(value);
            console.WriteLine();
        }
    }
}
