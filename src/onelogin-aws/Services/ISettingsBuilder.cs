using OneLoginAws.Console;
using OneLoginAws.Models;

namespace OneLoginAws.Services
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
