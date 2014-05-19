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
using StackExchange.Redis;

namespace MoneroPool
{
    public static class Hash
    {
        public static byte[] CryptoNight(byte[] data)
        {
            byte[] crytoNightHash = new byte[32];
            //Dirty hack for increased stack size
            Thread t = new Thread(
                () => NativeFunctions.cn_slow_hash(data, (uint)data.Length, crytoNightHash), 1024 * 1024 * 8);
            t.Start();
            t.Join();

            return crytoNightHash;
        }
        public static byte[] CryptoNightFastHash(byte[] data)
        {
            byte[] crytoNightHash = new byte[32];
            //Dirty hack for increased stack size
            Thread t = new Thread(
                () => NativeFunctions.cn_fast_hash(data, (uint)data.Length, crytoNightHash), 1024 * 1024 * 8);
            t.Start();
            t.Join();

            return crytoNightHash;
        }
    }

    public enum ShareProcess
    {
        ValidShare,
        ValidBlock,
        InvalidShare
    }

    public enum StaticsLock
    {
        LockedByPool = 2,
        LockedByBackGroundUpdater = 1,
        NoLock = 0
    }

    public class PoolHashRateCalculation
    {
        public uint Difficulty;
        public ulong Time;
        public DateTime Begin;

        public PoolHashRateCalculation()
        {
        }
    }

    public static class Statics
    {
        public static volatile StaticsLock Lock;

        public static volatile JObject CurrentBlockTemplate;

        public static volatile int CurrentBlockHeight;

        public static volatile int ReserveSeed;

        public static volatile IniFile Config;

        public static volatile JsonRPC DaemonJson;

        public static volatile JsonRPC WalletJson;

        public static volatile List<PoolBlock> BlocksPendingSubmition;

        public static volatile List<PoolBlock> BlocksPendingPayment;

        public static volatile Dictionary<string, ConnectedWorker> ConnectedClients = new Dictionary<string, ConnectedWorker>();

        public static volatile RedisPoolDatabase RedisDb;

        public static volatile PoolHashRateCalculation HashRate;
    }

    public static class Helpers
    {
        public static double GetHashRate(List<uint> difficulty, ulong time)
        {
            //Thanks surfer43
            double difficultySum = difficulty.Sum(x=>(double)x);

            return GetHashRate(difficultySum, time);
        }
        public static double GetHashRate(double difficulty, ulong time)
        {
            //Thanks surfer43

            return difficulty / time;
        }

        public static double GetWorkerHashRate(ConnectedWorker worker)
        {
            ulong time =
                (ulong)
                (worker.ShareDifficulty.Skip(worker.ShareDifficulty.Count - 4).First().Key -
                 worker.ShareDifficulty.Last().Key).Seconds;
            return GetHashRate(
                worker.ShareDifficulty.Skip(worker.ShareDifficulty.Count - 4)
                      .ToDictionary(x =>x.Key, x => (uint)x.Value)
                      .Values.ToList(), time);

        }

        public static uint WorkerVardiffDifficulty(ConnectedWorker worker)
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
                return (uint)((worker.CurrentDifficulty*(100 + deviance))/100);
            }
            else
            {
             if (Math.Abs(deviance) > int.Parse(Statics.Config.IniReadValue("vardiff-targettime-maxdeviation")))
                    deviance = -int.Parse(Statics.Config.IniReadValue("vardiff-targettime-maxdeviation"));
                return (uint)((worker.CurrentDifficulty*(100 + deviance))/100);
            }
        }

        public static string GenerateUniqueWork(ref int seed)
        {
            seed = Statics.ReserveSeed++;
            byte[] work = StringToByteArray((string) Statics.CurrentBlockTemplate["blocktemplate_blob"]);

            Array.Copy(BitConverter.GetBytes(seed), 0, work, (int)Statics.CurrentBlockTemplate["reserved_offset"], 4);

            return BitConverter.ToString(work).Replace("-", "");
        }

        public static byte[] GenerateShareWork(int seed)
        {
            byte[] work = StringToByteArray((string)Statics.CurrentBlockTemplate["blocktemplate_blob"]);

            Array.Copy(BitConverter.GetBytes(seed), 0, work, (int)Statics.CurrentBlockTemplate["reserved_offset"], 4);

            return work;
        }

        public static uint SwapEndianness(uint x)
        {
            return ((x & 0x000000ff) << 24) +  // First byte
                   ((x & 0x0000ff00) << 8) +   // Second byte
                   ((x & 0x00ff0000) >> 8) +   // Third byte
                   ((x & 0xff000000) >> 24);   // Fourth byte
        }

        public static uint GetTargetFromDifficulty(uint difficulty)
        {
            return uint.MaxValue/difficulty;
        }

        public static string GetRequestBody(Mono.Net.HttpListenerRequest request)
        {
            string documentContents;
            StreamReader readStream = new StreamReader(request.InputStream, Encoding.UTF8);

            documentContents = readStream.ReadToEnd();

            //readStream.Dispose();

            return documentContents;
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static ShareProcess ProcessShare(byte[] blockHash, int blockDifficulty, uint shareDifficulty)
        {
            BigInteger diff = new BigInteger(StringToByteArray("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00"));

            List<byte> blockList = blockHash.ToList();
            blockList.Add(0x00);
            BigInteger block = new BigInteger(blockList.ToArray());

            BigInteger blockDiff = diff / block;
            if (blockDiff >= blockDifficulty)
            {
                Logger.Log(Logger.LogLevel.General ,"Block found with hash:{0}", BitConverter.ToString(blockHash).Replace("-", ""));
                return ShareProcess.ValidBlock;

            }
            else if (blockDiff < shareDifficulty)
            {
               Logger.Log(Logger.LogLevel.General, "Invalid share found with hash:{0}", BitConverter.ToString(blockHash).Replace("-", ""));
                return ShareProcess.InvalidShare;
            }
           Logger.Log(Logger.LogLevel.General, "Valid share found with hash:{0}", BitConverter.ToString(blockHash).Replace("-", ""));
            return ShareProcess.ValidShare;
        }

    }
}
