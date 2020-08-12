using System;
using OneloginAwsCli.Extensions;

namespace OneloginAwsCli.Console.Input
{
    public class ConsoleInput<T> : IDisposable where T : IConvertible
    {
        private static Z ConvertTo<Z>(string _key)
        {
            return (Z)ConvertTo(_key, typeof(Z));
        }

        private static object ConvertTo(string toParse, Type type)
        {
            var undertype = Nullable.GetUnderlyingType(type);
            var basetype = undertype ?? type;
            return Convert.ChangeType(toParse, basetype);
        }

        private readonly IConsole _console;
        private readonly ConsoleInputOptions _options;
        private readonly AnsiStringBuilder _stringBuilder;
        private bool _shownError = false;

        public ConsoleInput(IConsole console, ConsoleInputOptions options)
        {
            _console = console;
            _options = options;
            _stringBuilder = new AnsiStringBuilder();
        }

        public string ReadLine()
        {
            var isRunning = true;
            var line = string.Empty;
            var index = 0;

            while (isRunning)
            {
                var key = _console.In.ReadKey();

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        isRunning = false;
                        break;

                    case ConsoleKey.LeftArrow when index > 0:
                        index--;
                        _console.Write("\x1B[1D");
                        break;

                    case ConsoleKey.RightArrow when index < line.Length:
                        index++;
                        _console.Write("\x1b[1C");
                        break;

                    case ConsoleKey.Backspace when index > 0:
                        line = line.Remove(--index, 1);
                        _console.Write("\x1B[1D"); // Move the cursor one unit to the left
                        _console.Write("\x1B[1P"); //
                        break;

                    case ConsoleKey.A when key.Modifiers == ConsoleModifiers.Control && index > 0:
                        _console.Write($"\x1B[{index}D");
                        index = 0;
                        break;

                    case ConsoleKey.E when key.Modifiers == ConsoleModifiers.Control && index < line.Length:
                        _console.Write($"\x1B[{line.Length - index}C");
                        index = line.Length;
                        break;

                    default:
                        if (!char.IsControl(key.KeyChar))
                        {
                            line = line.Insert(index, key.KeyChar.ToString());
                            var cursor = line.Length - 1 - index;
                            _console.Write(
                                _stringBuilder
                                    .Clear()
                                    .Write(index == 0 ? "" : $"\x1b[{index}D")
                                    .Write(line)
                                    .Write(cursor == 0 ? "" : $"\x1B[{cursor}D")
                                    .ToString()
                            );
                            index++;
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
            return line ?? string.Empty;
        }

        public T GetValue()
        {
            _console.Write(
                _stringBuilder
                    .Clear()
                    .Green("? ").Write(_options.Message).Write(" ")
                    .Cyan()
                    .ToString()
            );

            var readLine = _console.In.IsTTY() ? (Func<string>)ReadLine : ReadLinePlain;
            while (true)
            {
                try
                {
                    var plain = readLine();
                    return ConvertTo<T>(plain);
                }
                catch (Exception ex)
                {
                    _shownError = true;
                    _console.Write(
                        _stringBuilder.Clear()
                            .ResetColor()
                            .EraseLines(2)
                            .Green("? ").Write(_options.Message).Write(" ")
                            .Cyan().SaveCursorPosition().WriteLine()
                            .Red(">> ").Write(ex.Message).RestoreCursorPosition()
                            .ToString()
                    );
                }
            }
        }

        public void Dispose()
        {
            _console.Write(
                _stringBuilder
                    .Clear()
                    .EraseLines(_shownError ? 1 : 0)
                    .ResetColor()
                    .ToString()
            );
            _stringBuilder.Dispose();
        }
    }
}
