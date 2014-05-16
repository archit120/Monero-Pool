using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Numerics;

namespace MoneroPool
{
    public static class Hash
    {
        public static byte[] CryptoNight(byte[] data)
        {
            byte[] crytoNightHash = new byte[32];
            //Dirty hack for increased stack size
            Thread t = new Thread(
                () => NativeFunctions.cn_slow_hash(data, (ulong)data.Length, crytoNightHash), 1024 * 1024 * 8);
            t.Start();
            t.Join();

            return crytoNightHash;
        }
    }

    public static class Statics
    {
        public static volatile JObject CurrentBlockTemplate;

        public static volatile int CurrentBlockHeight;

        public static volatile int ReserveSeed;

        public static volatile IniFile Config;

        public static volatile JsonRPC DaemonJson;

        public static volatile JsonRPC WalletJson;

        public static volatile List<PoolBlock> BlocksPendingSubmition;

        public static volatile List<PoolBlock> BlocksPendingPayment;


    }

    public static class Helpers
    {
        public static double GetHashRate(List<ulong> difficulty, ulong time)
        {
            //Thanks surfer43
            double difficultySum = difficulty.Sum(x=>(double)x);

            return difficultySum / time;
        }

        public static double GetWorkerHashRate(ConnectedWorker worker)
        {
            ulong time =
                (ulong)
                (worker.ShareDifficulty.Skip(worker.ShareDifficulty.Count - 4).First().Key -
                 worker.ShareDifficulty.Last().Key).Seconds;
            return GetHashRate(
                worker.ShareDifficulty.Skip(worker.ShareDifficulty.Count - 4)
                      .ToDictionary(x => x.Key, x => x.Value)
                      .Values.ToList(), time);

        }

        public static int WorkerVardiffDifficulty(ConnectedWorker worker)
        {
            //We calculate average of last 4 shares.

            int aTargetTime = (((worker.ShareDifficulty.AsQueryable().Last().Key.Seconds +
                                 worker.ShareDifficulty[worker.ShareDifficulty.Count - 1].Key.Seconds)/
                                2) - int.Parse(Statics.Config.IniReadValue("vardiff-targettime-seconds")));

            double deviance =
               100.0 -
                         ((double) (aTargetTime*100)/
                          int.Parse(Statics.Config.IniReadValue("vardiff-targettime-deviation-allowed")));

            if (Math.Abs(deviance) < int.Parse(Statics.Config.IniReadValue("vardiff-targettime-deviation-allowed")))
                return worker.CurrentDifficulty;
            if (deviance > 0  )
            {
                if (deviance > int.Parse(Statics.Config.IniReadValue("vardiff-targettime-maxdeviation")))
                    deviance = int.Parse(Statics.Config.IniReadValue("vardiff-targettime-maxdeviation"));
                return (int)((worker.CurrentDifficulty*(100 + deviance))/100);
            }
            else
            {
             if (Math.Abs(deviance) > int.Parse(Statics.Config.IniReadValue("vardiff-targettime-maxdeviation")))
                    deviance = -int.Parse(Statics.Config.IniReadValue("vardiff-targettime-maxdeviation"));
                return (int)((worker.CurrentDifficulty*(100 + deviance))/100);
            }
        }


        public static string GenerateUniqueWork()
        {
            byte[] work = StringToByteArray((string) Statics.CurrentBlockTemplate["blocktemplate_blob"]);

            Array.Copy(BitConverter.GetBytes(Statics.ReserveSeed++), 0, work, (int)Statics.CurrentBlockTemplate["reserve_size"], 4);

            return BitConverter.ToString(work).Replace("-", "");
        }

        public static uint GetTargetFromDifficulty(int difficulty)
        {
            BigInteger diff = new BigInteger(StringToByteArray("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00"));
            return BitConverter.ToUInt32((diff/difficulty).ToByteArray().Take(4).Reverse().ToArray(), 0);
        }

        public static string GetRequestBody(HttpListenerRequest request)
        {
            string documentContents;
            using (Stream receiveStream = request.InputStream)
            {
                using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    documentContents = readStream.ReadToEnd();
                }
            }
            return documentContents;
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }



    }
}
