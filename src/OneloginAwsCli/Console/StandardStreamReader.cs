using System;
using System.IO;

namespace OneloginAwsCli.Console
{
    public static class StandardStreamReader
    {
        public static IStandardStreamReader Create(TextReader reader, Func<bool, ConsoleKeyInfo> readKey, int handle)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            return new AnonymousStandardStreamReader(reader.Read, reader.ReadLine, readKey, handle);
        }

        private class AnonymousStandardStreamReader : IStandardStreamReader
        {
            private readonly Func<int> _read;
            private readonly Func<string> _readLine;
            private readonly Func<bool, ConsoleKeyInfo> _readKey;
            private readonly int _handle;
            public AnonymousStandardStreamReader(Func<int> read, Func<string> readLine, Func<bool, ConsoleKeyInfo> readKey, int handle)
            {
                _read = read;
                _readLine = readLine;
                _readKey = readKey;
                _handle = handle;
            }

            public bool IsTTY() => StandardStreamHandle.IsTTY(_handle);
            public int Read() => _read();
            public ConsoleKeyInfo ReadKey() => _readKey(true);
            public string ReadLine() => _readLine();
        }
    }
}
