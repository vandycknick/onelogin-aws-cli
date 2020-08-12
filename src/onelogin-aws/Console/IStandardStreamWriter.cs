namespace OneLoginAws.Console
{
    public interface IStandardStreamWriter : IStandardStream
    {
        void Write(string value);
    }
}
