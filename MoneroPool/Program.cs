using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;


namespace MoneroPool
{
    internal class Program
    {
        private static IniFile config = new IniFile("config.txt");

 

        private static void Main(string[] args)
        {

            ConfigurationOptions configR = new ConfigurationOptions();
            configR.ResolveDns = true;

            string host = config.IniReadValue("redis-server");
            int port = 6379;

            if (host.Split(':').Length == 2)
                port = int.Parse(host.Split(':')[1]);

            host = host.Split(':')[0];

            configR.EndPoints.Add(Dns.GetHostAddresses(host)[0], port);

            Logger.Log(Logger.LogLevel.General, "Starting up!");

            Statics.HashRate = new PoolHashRateCalculation();
            try
            {

                Statics.RedisDb =
                    new RedisPoolDatabase(
                        ConnectionMultiplexer.Connect(configR).GetDatabase(int.Parse(config.IniReadValue("redis-database"))));
            }
            catch (StackExchange.Redis.RedisConnectionException e)
            {
                if (NativeFunctions.IsLinux)
                {
                    Logger.Log(Logger.LogLevel.Error, "Redis connection failed.Retrying after 3 seconds");
                    Thread.Sleep(3 * 1000);
                    while (true)
                    {
                        try
                        {
                            Statics.RedisDb =
                     new RedisPoolDatabase(
                         ConnectionMultiplexer.Connect(configR).GetDatabase(int.Parse(config.IniReadValue("redis-database"))));
                            break;
                        }
                        catch 
                        {
                        }
                        Logger.Log(Logger.LogLevel.Error, "Redis connection failed.Retrying after 3 seconds");
                        Thread.Sleep(3 * 1000);
                    }
                }
                else
                {
                    Logger.Log(Logger.LogLevel.Error, "Redis connection failed. Shutting down");
                    System.Diagnostics.Process.GetCurrentProcess().Close();
                }
            }
            Statics.BlocksPendingPayment = new List<PoolBlock>();
            Statics.BlocksPendingSubmition = new List<PoolBlock>();
            Statics.Config = new IniFile("config.txt");
            Statics.ConnectedClients = new Dictionary<string, ConnectedWorker>();
            Statics.DaemonJson = new JsonRPC(config.IniReadValue("daemon-json-rpc"));
            Statics.WalletJson = new JsonRPC(config.IniReadValue("wallet-json-rpc"));


            BackgroundStaticUpdater backgroundSaticUpdater = new BackgroundStaticUpdater();
            backgroundSaticUpdater.Start();

            BlockPayment blockPayment = new BlockPayment();
            blockPayment.Start();

            BlockSubmitter blockSubmitter = new BlockSubmitter();
            blockSubmitter.Start();

            CryptoNightPool cryptoNightPool = new CryptoNightPool();
            cryptoNightPool.Start();

            while (true)
            {
                Thread.Sleep(Timeout.Infinite);
            }

        }
    }
}
