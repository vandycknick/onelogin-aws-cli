using System;

namespace OneloginAwsCli.Console
{
    public interface IStandardStreamReader : IStandardStream
    {
        ConsoleKeyInfo ReadKey();
        int Read();
        string ReadLine();
    }
}
