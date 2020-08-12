using System;
using System.IO;

namespace OneLoginAws.Console
{
    public static class StandardStreamWriter
    {
        public static IStandardStreamWriter Create(TextWriter writer, int handle)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            return new AnonymousStandardStreamWriter(writer.Write, handle);
        }

        public static void WriteLine(this IStandardStreamWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.Write(Environment.NewLine);
        }

        public static void WriteLine(this IStandardStreamWriter writer, string value)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.Write(value);
            writer.Write(Environment.NewLine);
        }

        private class AnonymousStandardStreamWriter : IStandardStreamWriter
        {
            private readonly Action<string> _write;
            private readonly int _handle;

            public AnonymousStandardStreamWriter(Action<string> write, int handle)
            {
                _write = write;
                _handle = handle;
            }

            public bool IsTTY() => StandardStreamHandle.IsTTY(_handle);
            public void Write(string value) => _write(value);
        }
    }
}
