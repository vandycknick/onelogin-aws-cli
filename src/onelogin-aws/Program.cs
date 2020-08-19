using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using OneLoginApi.Exceptions;
using OneLoginAws.Exceptions;

namespace OneLoginAws
{
    public static class Program
    {
        static Task Main(string[] args)
        {
            var command = new RootCommand("OneLogin AWS cli");

            var parser = new CommandLineBuilder(command)
                .AddCommand(LoginCommand.Create())
                .AddCommand(ConfigCommand.Create())
                .UseDefaults()
                .UseExceptionHandler(HandleException)
                .Build();

            return parser.InvokeAsync(args);
        }

        private static void HandleException(Exception exception, InvocationContext context)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;

            if (exception is TargetInvocationException tie &&
                tie.InnerException is object)
            {
                exception = tie.InnerException;
            }

            if (exception is MissingRequiredSettingsException required)
            {
                context.Console.Error.WriteLine("Missing required setting!");
                context.Console.Error.WriteLine();

                if (string.IsNullOrEmpty(required.BaseUri))
                {
                    context.Console.Error.WriteLine("No base_uri provided, please provide a valid base uri for the OneLogin Api.");
                }
                if (string.IsNullOrEmpty(required.Profile))
                {
                    context.Console.Error.WriteLine("No profile provided, please specify a profile either via the config file an environment variable ONELOGIN_AWS_CLI_PROFILE or as a command line flag --profile.");
                }
                else if (string.IsNullOrEmpty(required.ClientId) || string.IsNullOrEmpty(required.ClientSecret))
                {
                    context.Console.Error.WriteLine("A valid client_id and client_secret are required! Please add them to your config.");
                }
                else if (string.IsNullOrEmpty(required.AwsAppId))
                {
                    context.Console.Error.WriteLine("A valid aws_app_id is required! Please add it to your config file.");
                }
                else if (string.IsNullOrEmpty(required.Subdomain))
                {
                    context.Console.Error.WriteLine("A valid subdomain is required! Please add it to your config file.");
                }
            }
            else if (exception is ConfigFileNotFoundException configNotFound)
            {
                context.Console.Error.WriteLine("Config file not found:");
                context.Console.Error.WriteLine($"Make sure a valid config file is available at the following filepath `{configNotFound.FilePath}`");
            }
            else if (exception is AuthorizationException auth)
            {
                context.Console.Error.WriteLine(auth.Message);
            }
            else if (exception is NotFoundException notFound)
            {
                context.Console.Error.WriteLine($"OneLogin Error ({notFound.StatusCode}): {notFound.Message}");
            }
            else if (exception is ApiException api)
            {
                context.Console.Error.WriteLine($"Oh no, a OneLogin api exception ({api.StatusCode}):");
                context.Console.Error.WriteLine($"ApiError: {JsonSerializer.Serialize(api.Error)}");
                context.Console.Error.WriteLine();
                context.Console.Error.WriteLine($"Exception:");
                context.Console.Error.WriteLine(api.ToString());
            }
            else
            {
                context.Console.Error.WriteLine("An unhandled exception has occurred, how unseemly: ");
                context.Console.Error.WriteLine(exception.ToString());
            }

            System.Console.ResetColor();
            context.ResultCode = 1;
        }
    }
}
