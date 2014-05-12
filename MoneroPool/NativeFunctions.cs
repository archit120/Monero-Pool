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
        [DllImport("CryptoNight.dll", EntryPoint = "cn_slow_hash")]
        public static extern void cn_slow_hash_win_64(byte[] data, ulong length, byte[] hash);

        [DllImport("CryptoNight.dll", EntryPoint = "cn_slow_hash")]
        public static extern void cn_slow_hash_win_32(byte[] data, uint length, byte[] hash);

        public static bool IsLinux
        {
            get
            {
                int p = (int) Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }

        public static void cn_slow_hash(byte[] data, ulong length, byte[] hash)
        {
            if (!IsLinux)
            {
                switch (Environment.Is64BitOperatingSystem)
                {
                    case true:
                        cn_slow_hash_win_64(data, length, hash);
                        break;
                    case false:
                        cn_slow_hash_win_32(data,(uint)length, hash);
                        break;
                }
            }
            else
            {
                throw new Exception("Sorry no linux ATM");
            }
        }
    }
}
