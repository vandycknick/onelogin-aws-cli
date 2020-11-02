using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using IniParser;

namespace OneLoginAws
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
            System.Console.WriteLine("Not implemented yet!");
        }
    }
}
