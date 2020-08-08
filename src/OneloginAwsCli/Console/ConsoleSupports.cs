using System;
using System.Runtime.InteropServices;

namespace OneloginAwsCli.Console
{
    public static class ConsoleSupports
    {
        public static bool Emojis()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WT_SESSION")))
            {
                return false;
            }

            return true;
        }
    }
}
