using System;
using OneloginAwsCli.Extensions;

namespace OneloginAwsCli.Console.Input
{
    public class ConsolePassword : IDisposable
    {
        private readonly IConsole _console;
        private readonly ConsolePasswordOptions _options;
        private readonly AnsiStringBuilder _stringBuilder;

        public ConsolePassword(IConsole console, ConsolePasswordOptions options)
        {
            _console = console;
            _options = options;
            _stringBuilder = new AnsiStringBuilder();
        }

        public string ReadLine()
        {
            var isRunning = true;
            var line = string.Empty;

            while (isRunning)
            {
                var key = _console.In.ReadKey();

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        isRunning = false;
                        break;

                    case ConsoleKey.Backspace when line.Length > 0:
                        line = line.Remove(line.Length - 1, 1);
                        _console.Write("\x1B[1D"); // Move the cursor one unit to the left
                        _console.Write("\x1B[1P"); //
                        break;

                    default:
                        if (!char.IsControl(key.KeyChar))
                        {
                            line += key.KeyChar;
                            _console.Write("*");
                        }
                        break;
                }
            }

            _console.WriteLine();

            return line;
        }

        public string ReadLinePlain()
        {
            var line = _console.In.ReadLine();
            _console.Out.WriteLine();
            return line;
        }

        public string GetValue()
        {
            _console.Write(
                _stringBuilder
                    .Clear()
                    .Green("? ").Write(_options.Message).Write(" ")
                    .Cyan()
                    .ToString()
            );

            var readLine = _console.In.IsTTY() ? (Func<string>)ReadLine : ReadLinePlain;
            var value = readLine();

            if (_options.MaskAfterEnter)
            {
                _console.WriteLine(
                    _stringBuilder
                        .Clear()
                        .EraseLines(2)
                        .Green("? ").Write(_options.Message).Cyan(" [input is masked]")
                        .ToString()
                );
            }

            return value;
        }

        public void Dispose()
        {
            _console.Write(
                _stringBuilder
                    .Clear()
                    .EraseLines(0)
                    .ResetColor()
                    .ToString()
            );
            _stringBuilder.Dispose();
        }
    }
}
