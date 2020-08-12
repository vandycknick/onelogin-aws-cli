using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace OneLoginAws.Console
{
    public class AnsiStringBuilder : IDisposable
    {
        private const string HIDE_CURSOR = "\x1b[?25l";
        private const string SHOW_CURSOR = "\x1b[?25h";
        private const string ERASE_LINE = "\x1b[2K";
        private const string RED_COLOR = "\u001b[31m";
        private const string GREEN_COLOR = "\x1b[32m";
        private const string YELLOW_COLOR = "\x1b[33m";
        private const string BLUE_COLOR = "\x1b[34m";
        private const string CYAN_COLOR = "\x1b[36m";
        private const string RESET_COLOR = "\x1b[0m";
        private const string ESCAPE = "\x1b[";
        private const int DEFAULT_BUFFER_SIZE = 32768; // use 32K default buffer.

        private static readonly char newLine1;
        private static readonly char newLine2;
        private static readonly bool crlf;

        static AnsiStringBuilder()
        {
            var newLine = Environment.NewLine.ToCharArray();
            if (newLine.Length == 1)
            {
                // cr or lf
                newLine1 = newLine[0];
                crlf = false;
            }
            else
            {
                // crlf(windows)
                newLine1 = newLine[0];
                newLine2 = newLine[1];
                crlf = true;
            }
        }

        private char[]? _buffer;
        private int _index;

        public AnsiStringBuilder()
        {
            _buffer = ArrayPool<char>.Shared.Rent(DEFAULT_BUFFER_SIZE);
            _index = 0;
        }

        private void Grow(int sizeHint)
        {
            ThrowDisposedIf(_buffer is null);

            var nextSize = _buffer.Length * 2;
            if (sizeHint != 0)
            {
                nextSize = Math.Max(nextSize, _index + sizeHint);
            }

            var newBuffer = ArrayPool<char>.Shared.Rent(nextSize);

            _buffer.CopyTo(newBuffer, 0);
            ArrayPool<char>.Shared.Return(_buffer);

            _buffer = newBuffer;
        }

        public AnsiStringBuilder Write(char value)
        {
            ThrowDisposedIf(_buffer is null);

            if (_buffer.Length - _index < 1)
            {
                Grow(1);
            }

            _buffer[_index++] = value;
            return this;
        }

        public AnsiStringBuilder Write(ReadOnlySpan<char> value)
        {
            ThrowDisposedIf(_buffer is null);

            if (_buffer.Length - _index < value.Length)
            {
                Grow(value.Length);
            }

            var buffer = _buffer.AsSpan(_index, value.Length);
            if (value.TryCopyTo(buffer))
            {
                _index += value.Length;
            }

            return this;
        }

        public AnsiStringBuilder Write(string value)
        {
            ThrowDisposedIf(_buffer is null);

            if (_buffer.Length - _index < value.Length)
            {
                Grow(value.Length);
            }

            value.CopyTo(0, _buffer, _index, value.Length);
            _index += value.Length;
            return this;
        }

        public AnsiStringBuilder WriteLine(string value)
        {
            Write(value);
            WriteLine();
            return this;
        }


        public AnsiStringBuilder WriteLine()
        {
            ThrowDisposedIf(_buffer is null);

            if (crlf)
            {
                if (_buffer.Length - _index < 2) Grow(2);
                _buffer[_index] = newLine1;
                _buffer[_index + 1] = newLine2;
                _index += 2;
            }
            else
            {
                if (_buffer.Length - _index < 1) Grow(1);
                _buffer[_index] = newLine1;
                _index += 1;
            }
            return this;
        }

        public AnsiStringBuilder Red(string value) => Red().Write(value).ResetColor();

        public AnsiStringBuilder Red(char value) => Red().Write(value).ResetColor();

        public AnsiStringBuilder Red() => Write(RED_COLOR);

        public AnsiStringBuilder Green(string value) => Green().Write(value).Write(RESET_COLOR);

        public AnsiStringBuilder Green(char value) => Green().Write(value).Write(RESET_COLOR);

        public AnsiStringBuilder Green() => Write(GREEN_COLOR);

        public AnsiStringBuilder Yellow(string value) => Yellow().Write(value).ResetColor();

        public AnsiStringBuilder Yellow(char value) => Yellow().Write(value).ResetColor();

        public AnsiStringBuilder Yellow() => Write(YELLOW_COLOR);

        public AnsiStringBuilder Blue(string value) => Blue().Write(value).ResetColor();

        public AnsiStringBuilder Blue(char value) => Blue().Write(value).ResetColor();

        public AnsiStringBuilder Blue() => Write(BLUE_COLOR);

        public AnsiStringBuilder Cyan(string value) => Cyan().Write(value).ResetColor();

        public AnsiStringBuilder Cyan(char value) => Cyan().Write(value).ResetColor();

        public AnsiStringBuilder Cyan() => Write(CYAN_COLOR);

        public AnsiStringBuilder ResetColor() => Write(RESET_COLOR);

        public AnsiStringBuilder EraseLines(int count)
        {
            for (var i = 0; i < count; i++)
            {
                Write(ERASE_LINE);

                if (i < count -1) CursorUp();
            }

            if (count > 0)
            {
                CursorLeft();
            }

            return this;
        }

        public AnsiStringBuilder CursorUp(int count = 1) => Write(ESCAPE).Write($"{count}A");

        public AnsiStringBuilder CursorLeft() => Write(ESCAPE).Write('G');

        public AnsiStringBuilder SaveCursorPosition() => Write(ESCAPE).Write('s');

        public AnsiStringBuilder RestoreCursorPosition() => Write(ESCAPE).Write('u');

        public AnsiStringBuilder HideCursor() => Write(HIDE_CURSOR);

        public AnsiStringBuilder ShowCursor() => Write(SHOW_CURSOR);

        public AnsiStringBuilder Clear()
        {
            _index = 0;
            return this;
        }

        public ReadOnlySpan<char> AsSpan() => _buffer.AsSpan(0, _index);

        public override string ToString() => AsSpan().ToString();

        public void ThrowDisposedIf([DoesNotReturnIf(true)] bool condition)
        {
            if (condition) ThrowObjectDisposed();
        }

        [DoesNotReturn]
        public void ThrowObjectDisposed() =>
                throw new ObjectDisposedException(nameof(AnsiStringBuilder));

        public void Dispose()
        {
            // Return buffer
            if (_buffer != null)
            {
                ArrayPool<char>.Shared.Return(_buffer);
            }

            // Reset
            _buffer = null;
            _index = 0;
        }
    }
}
