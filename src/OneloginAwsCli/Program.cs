using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace OneloginAwsCli
{
    static partial class Program
    {
        static Task Main(string[] args)
        {
            var command = new RootCommand("OneLogin AWS cli");

            var parser = new CommandLineBuilder(command)
                .AddCommand(LoginCommand.Create())
                .AddCommand(ConfigCommand.Create())
                .UseDefaults()
                .Build();

            return parser.InvokeAsync(args);
        }
    }
}
