using System;
using System.Collections.Generic;
using OneloginAwsCli.Extensions;

namespace OneloginAwsCli.Console.Select
{
    public class ConsoleSelect<T> : IDisposable where T : class
    {
        public static int Trap(int value, int min, int max)
        {
            if (value < min) return max;
            if (value > max) return min;
            return value;
        }

        private readonly IConsole _console;
        private readonly ConsoleSelectOptions<T> _options;
        private readonly IReadOnlyList<T> _items;
        private readonly Func<T, bool, string> _onRenderItem;
        private bool _initialRender = true;
        private AnsiStringBuilder _stringBuilder;
        private int _selectedItem = 0;

        public ConsoleSelect(IConsole console, ConsoleSelectOptions<T> options)
        {
            _console = console;
            _options = options;
            _items = options.Items ?? new List<T>();
            _onRenderItem = options.OnRenderItem ?? DefaultOnRenderItem;
            _selectedItem = options.DefaultSelectedItem;

            _stringBuilder = new AnsiStringBuilder();
        }

        private string DefaultOnRenderItem(T item, bool _) => item.ToString();

        private void RenderInteractive()
        {
            _stringBuilder.Clear();

            if (_initialRender)
            {
                _stringBuilder.HideCursor();
                _initialRender = false;
            }
            else
            {
                _stringBuilder.EraseLines(_items.Count + 2);
            }

            var selectedItem = _items[_selectedItem];
            var padding = string.Empty.PadLeft(_options.Indent);
            foreach (var (item, index) in _items.WithIndex())
            {
                var isSelected = item == selectedItem;
                var value = _onRenderItem(item, isSelected);
                var newline = index != _items.Count ? Environment.NewLine : "";

                _stringBuilder.Write(padding);

                var line = $"{index + 1}) {value} {newline}";
                if (isSelected)
                {
                    _stringBuilder.Cyan(line);
                }
                else
                {
                    _stringBuilder.Write(line);
                }
            }

            _console.Write(_stringBuilder.ToString());
        }

        private void RenderPlain()
        {
            _stringBuilder.Clear();

            var selectedItem = _items[_selectedItem];
            var padding = string.Empty.PadLeft(_options.Indent);
            foreach (var (item, index) in _items.WithIndex())
            {
                var isSelected = item == selectedItem;
                var value = _onRenderItem(item, isSelected);
                var newline = index != _items.Count ? Environment.NewLine : "";

                _stringBuilder
                    .Write(padding)
                    .Write($"{index + 1}) {value} {newline}");
            }

            _console.Write(_stringBuilder.ToString());
        }

        private void ReadInteractiveLoop()
        {
            var running = true;
            while (running)
            {
                RenderInteractive();
                System.Console.TreatControlCAsInput = true;

                var key = _console.In.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        _selectedItem = Trap(--_selectedItem, 0, _items.Count - 1);
                        break;

                    case ConsoleKey.DownArrow:
                        _selectedItem = Trap(++_selectedItem, 0, _items.Count - 1);
                        break;

                    case ConsoleKey.Enter:
                        running = false;
                        break;

                    case ConsoleKey.Escape:
                    case ConsoleKey.C when key.Modifiers == ConsoleModifiers.Control:
                        running = false;
                        break;

                    default:
                        break;
                }

                _console.WriteLine();
                System.Console.TreatControlCAsInput = true;
            }
        }

        private void ReadPlainLoop()
        {
            var valid = false;
            while (!valid)
            {
                RenderPlain();
                _console.Write("? ");

                var answer = _console.In.ReadLine();
                valid = int.TryParse(answer, out var selected);

                if (selected > 0 && selected <= _items.Count)
                {
                    _selectedItem = selected - 1;
                }
                else
                {
                    valid = false;
                }
            }
        }

        private void CleanUp() =>
            _console.WriteLine(
                _stringBuilder
                    .Clear()
                    .EraseLines(_items.Count + 3)
                    .Green("? ").Write(_options.Message).Cyan($" {_onRenderItem(_items[_selectedItem], false)}")
                    .ShowCursor()
                    .ToString()
            );

        public T GetValue()
        {
            var readLoop = _console.In.IsTTY() ? (Action)ReadInteractiveLoop : ReadPlainLoop;

            _console.WriteLine(
                _stringBuilder.Clear()
                    .Green("? ").Write(_options.Message)
                    .ToString()
            );

            readLoop();
            CleanUp();

            return _items[_selectedItem];
        }

        public void Dispose()
        {
            _stringBuilder.Clear();
            _stringBuilder.Dispose();
        }
    }
}
