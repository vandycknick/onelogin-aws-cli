using System;
using System.Runtime.InteropServices;
using OneloginAwsCli.Console.Native;

namespace OneloginAwsCli.Console
{
    public static class StandardStreamHandle
    {
        public static int IN = 0;
        public static int OUT = 1;
        public static int ERROR = 2;

        public static bool IsTTY(int handle)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new NotSupportedException();
            }
            else
            {
                return Libc.isatty(handle) == 1;
            }
        }
    }
}
