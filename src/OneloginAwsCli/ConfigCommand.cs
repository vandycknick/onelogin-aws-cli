using System.CommandLine;

namespace OneloginAwsCli
{
    public class ConfigCommand
    {
        public static Command Create()
        {
            var command = new Command("configure");
            return command;
        }
    }
}
