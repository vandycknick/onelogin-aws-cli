namespace OneLoginApi.Authentication
{
    public class Credentials
    {
        public Credentials(string login, string password) : this(login, password, AuthenticationType.Basic)
        {

        }

        public Credentials(string login, string password, AuthenticationType authType)
        {
            Login = login;
            Password = password;
            AuthenticationType = authType;
        }

        public Credentials(string token) : this(token, AuthenticationType.OAuth)
        {

        }

        public Credentials(string token, AuthenticationType authType)
        {
            Login = null;
            Password = token;
            AuthenticationType = authType;
        }

        public string? Login
        {
            get;
            private set;
        }

        public string Password
        {
            get;
            private set;
        }

        public AuthenticationType AuthenticationType
        {
            get;
            private set;
        }
    }
}
