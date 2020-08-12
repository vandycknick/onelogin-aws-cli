using System.Runtime.InteropServices;

namespace OneLoginAws.Console.Native
{
    #pragma warning disable IDE1006
    public static class Libc
    {
        [DllImport("libc", SetLastError = true)]
        public static extern int isatty(int fd);
    }
    #pragma warning restore IDE1006
}
