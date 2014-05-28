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

            Logger.AppLogLevel = Logger.LogLevel.Debug;
            Logger.Log(Logger.LogLevel.General, "Starting up!");

            Statics.HashRate = new PoolHashRateCalculation();

            Logger.Log(Logger.LogLevel.Debug, "Initialized PoolHashRateCalculation");
            try
            {

                Statics.RedisDb =
                    new RedisPoolDatabase(
                        ConnectionMultiplexer.Connect(configR)
                                             .GetDatabase(int.Parse(config.IniReadValue("redis-database"))));
                Logger.Log(Logger.LogLevel.Debug, "Initialized RedisDb");

            }
            catch (StackExchange.Redis.RedisConnectionException)
            {
                if (NativeFunctions.IsLinux)
                {
                    Logger.Log(Logger.LogLevel.Error, "Redis connection failed.Retrying after 3 seconds");
                    Thread.Sleep(3*1000);
                    while (true)
                    {
                        try
                        {
                            Statics.RedisDb =
                                new RedisPoolDatabase(
                                    ConnectionMultiplexer.Connect(configR)
                                                         .GetDatabase(int.Parse(config.IniReadValue("redis-database"))));
                            break;
                        }
                        catch
                        {
                        }
                        Logger.Log(Logger.LogLevel.Error, "Redis connection failed.Retrying after 3 seconds");
                        Thread.Sleep(3*1000);
                    }
                }
                else
                {
                    Logger.Log(Logger.LogLevel.Error, "Redis connection failed. Shutting down");
                    Environment.Exit(-1);
                }
            }
            Statics.BlocksPendingPayment = new List<PoolBlock>();
            Logger.Log(Logger.LogLevel.Debug, "Initialized BlocksPendingPayment");

            Statics.BlocksPendingSubmition = new List<PoolBlock>();
            Logger.Log(Logger.LogLevel.Debug, "Initialized BlocksPendingSubmition");

            Statics.Config = new IniFile("config.txt");
            Logger.Log(Logger.LogLevel.Debug, "Initialized Config");

            Statics.ConnectedClients = new Dictionary<string, ConnectedWorker>();
            Logger.Log(Logger.LogLevel.Debug, "Initialized ConnectedClients");

            Statics.DaemonJson = new JsonRPC(config.IniReadValue("daemon-json-rpc"));
            Logger.Log(Logger.LogLevel.Debug, "Initialized DaemonJson");

            Statics.WalletJson = new JsonRPC(config.IniReadValue("wallet-json-rpc"));
            Logger.Log(Logger.LogLevel.Debug, "Initialized WalletJson");

            Logger.Log(Logger.LogLevel.General, "Initialized Statics, initializing classes");



            BackgroundStaticUpdater backgroundSaticUpdater = new BackgroundStaticUpdater();
            backgroundSaticUpdater.Start();
            Logger.Log(Logger.LogLevel.Debug, "Initialized backgroundSaticUpdater");

            BlockPayment blockPayment = new BlockPayment();
            blockPayment.Start();
            Logger.Log(Logger.LogLevel.Debug, "Initialized BlockPayment");

            BlockSubmitter blockSubmitter = new BlockSubmitter();
            blockSubmitter.Start();
            Logger.Log(Logger.LogLevel.Debug, "Initialized BlockSubmitter");

            DifficultyRetargeter difficultyRetargeter = new DifficultyRetargeter();
            difficultyRetargeter.Start();
            Logger.Log(Logger.LogLevel.Debug, "Initialized DifficultyRetargeter");


            CryptoNightPool cryptoNightPool = new CryptoNightPool();
            cryptoNightPool.Start();
            Logger.Log(Logger.LogLevel.Debug, "Initialized CryptoNightPool");

            Logger.Log(Logger.LogLevel.General, "Initialized Classes");

            while (true)
            {
                Logger.Log(Logger.LogLevel.Debug, "Put the main thread into sleep...");
                Thread.Sleep(Timeout.Infinite);

            }

        }
    }
}
