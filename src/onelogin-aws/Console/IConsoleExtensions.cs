using System;
using System.Collections.Generic;
using OneLoginAws.Console.Input;
using OneLoginAws.Console.Select;
using OneLoginAws.Console.Spinner;

namespace OneLoginAws.Console
{
    public static class IConsoleExtensions
    {
        public static T Input<T>(this IConsole console, string message) where T : IConvertible
        {
            var options = new ConsoleInputOptions
            {
                Message = message,
            };
            using var input = new ConsoleInput<T>(console, options);
            return input.GetValue();
        }

        public static string Password(this IConsole console, string message, bool maskAfterEnter = false)
        {
            var options = new ConsolePasswordOptions
            {
                Message = message,
                MaskAfterEnter = maskAfterEnter,
            };
            using var pwd = new ConsolePassword(console, options);
            return pwd.GetValue();
        }

        public static T Select<T>(this IConsole console, string message, IReadOnlyList<T> items, Func<T, bool, string>? onRenderItem = null, int indent = 2) where T : class
        {
            var options = new ConsoleSelectOptions<T>
            {
                Message = message,
                Items = items,
                OnRenderItem = onRenderItem,
                Indent = indent,
            };
            using var select = new ConsoleSelect<T>(console, options);
            return select.GetValue();
        }

        public static ConsoleSpinner RenderSpinner(this IConsole console, bool autoStart = false)
        {
            var options = new ConsoleSpinnerOptions
            {
                AutoStart = autoStart,
            };
            return new ConsoleSpinner(console, options);
        }
    }
}
