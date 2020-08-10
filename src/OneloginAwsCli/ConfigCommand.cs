using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using IniParser;

namespace OneloginAwsCli
{
    public class ConfigCommand
    {
        public static Command Create()
        {
            var command = new Command("configure")
            {
                Handler = CommandHandler.Create(() =>
                {
                    var config = new ConfigCommand();
                    return config.InvokeAsync();
                })
            };
            return command;
        }

        public async Task InvokeAsync()
        {
            await Task.Delay(1);

            var parser = new FileIniDataParser();
            var config = parser.ReadFile(
                filePath: Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".onelogin-aws.config")
            );


            var test = config["default"]["does_not_exit"];
            System.Console.WriteLine(test == null);
        }
    }
}
