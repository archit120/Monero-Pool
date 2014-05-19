using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MoneroPool
{
    public static class NativeFunctions
    {
        [DllImport("CryptoNight", EntryPoint = "cn_slow_hash")]
        public static extern void cn_slow_hash(byte[] data, uint length, byte[] hash);

        [DllImport("CryptoNight", EntryPoint = "cn_fast_hash")]
        public static extern void cn_fast_hash(byte[] data, uint length, byte[] hash);


        public static bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }
    }
}
