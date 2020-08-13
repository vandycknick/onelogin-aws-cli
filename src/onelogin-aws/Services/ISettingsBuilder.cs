using OneLoginAws.Console;
using OneLoginAws.Models;

namespace OneLoginAws.Services
{
    public interface ISettingsBuilder
    {
        SettingsBuilder UseDefaults();

        SettingsBuilder UseConfigName(string name);

        SettingsBuilder UseFromEnvironment();

        SettingsBuilder UseFromJson(string? reader);

        Settings Build();
    }
}
