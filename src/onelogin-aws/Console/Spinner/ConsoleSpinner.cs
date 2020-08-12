using System;
using System.Threading.Tasks;
using OneLoginAws.Extensions;

namespace OneLoginAws.Console.Spinner
{
    public class ConsoleSpinner : IDisposable
    {
        public static int Trap(int value, int min, int max)
        {
            if (value < min) return max;
            if (value > max) return min;
            return value;
        }

        private const string SpinnerFallback = "|/-\\";
        private const string SpinnerA = "⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏";

        private int _index = 0;
        private bool _spinning = false;
        private readonly IConsole _console;
        private readonly AnsiStringBuilder _stringBuilder;
        private readonly ConsoleSpinnerOptions _options;

        public ConsoleSpinner(IConsole console, ConsoleSpinnerOptions? options = default)
        {
            _console = console;
            _options = options ?? new ConsoleSpinnerOptions();
            _stringBuilder = new AnsiStringBuilder();

            if (_options.AutoStart) Start();
        }

        private async Task Render()
        {
            var spinner = ConsoleSupports.Emojis() ? SpinnerA : SpinnerFallback;

            _stringBuilder.HideCursor();

            while (_spinning)
            {
                _stringBuilder
                    .CursorLeft()
                    .Yellow(spinner[_index]);

                _console.Write(_stringBuilder.AsSpan().ToString());

                await Task.Delay(_options.Delay);
                _index = Trap(++_index, 0, spinner.Length - 1);
            }
        }

        public void Start()
        {
            _spinning = true;
            Task.Run(Render);
        }

        public void Stop()
        {
            if (!_spinning) return;

            _spinning = false;
            _console.Write(_stringBuilder.EraseLines(1).CursorLeft().ShowCursor().ToString());
        }

        public void Dispose()
        {
            Stop();

            _stringBuilder.Dispose();
        }
    }
}
