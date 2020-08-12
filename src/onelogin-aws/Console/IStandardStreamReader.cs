using System;

namespace OneLoginAws.Console
{
    public interface IStandardStreamReader : IStandardStream
    {
        ConsoleKeyInfo ReadKey();
        int Read();
        string? ReadLine();
    }
}
