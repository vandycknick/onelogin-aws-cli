using OneloginAwsCli.Console;
using OneloginAwsCli.Models;

namespace OneloginAwsCli.Services
{
    public interface ISettingsBuilder
    {
        SettingsBuilder UseDefaults();

        SettingsBuilder UseCommandLineOverrides(string profile, string userName, string region);

        SettingsBuilder UseConfigName(string name);

        SettingsBuilder UseFromEnvironment();

        SettingsBuilder UseFromJsonInput(IStandardStreamReader reader);

        Settings Build();
    }
}
