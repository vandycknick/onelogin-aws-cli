using System.Runtime.InteropServices;

namespace OneloginAwsCli.Console.Native
{
    public static class Libc
    {
        [DllImport("libc", SetLastError = true)]
        public static extern int isatty(int fd);
    }
}
