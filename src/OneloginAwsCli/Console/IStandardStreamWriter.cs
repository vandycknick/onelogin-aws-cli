namespace OneloginAwsCli.Console
{
    public interface IStandardStreamWriter : IStandardStream
    {
        void Write(string value);
    }
}
