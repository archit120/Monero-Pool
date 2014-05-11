using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using CryptoNight;
using System.Threading;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;


namespace MoneroPool
{
    internal struct worker
    {
        public string address;
        //public string guid;

        public JObject work;
        public DateTime last_heard;
    }

    internal class Program
    {
        public enum ShareProcess
        {
            ValidShare,
            ValidBlock,
            InvalidShare
        }

        private static JsonRPC json = new JsonRPC("http://127.0.0.1:18081");
        private static JsonRPC Walletjson = new JsonRPC("http://127.0.0.1:8082");

        private static Dictionary<string, worker> ConnectedClients = new Dictionary<string, worker>();
        private static ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");

        private static IDatabase redisDb;
        private static IniFile config = new IniFile("config.txt");

        public static byte[] CryptoNight(byte[] data)
        {
            byte[] test = new byte[32];
            Thread t = new Thread(() =>
                {
                    test = Crypto.CnSlowHash(data, data.Length);

                }, 1024*1024*8);
            t.Start();
            t.Join();
            return test;
        }

        private static void Main(string[] args)
        {
            json = new JsonRPC(config.IniReadValue("daemon-json-rpc"));
            Walletjson = new JsonRPC(config.IniReadValue("wallet-json-rpc"));

            redis = ConnectionMultiplexer.Connect(config.IniReadValue("redis-server"));

            redisDb = redis.GetDatabase();

            //var a = redisDb.HashGetAll("miners");
            // ConnectedClients.Add("a",new worker());

            JObject test2 = json.InvokeMethod("getblockcount");
            CurrentBlockHeight = (int) test2["result"]["count"];
            /*test2 = json.InvokeMethod("getblockheaderbyheight", CurrentBlockHeight);

                //, new JObject(new JProperty("reserve_size", 4), new JProperty("wallet_address", "41jhre5xFk92GYaJgxvHuzUC5uZtQ4UDU1APv3aRAc27DWBqKEzubC2WSvmnbxaswLdB1BsQnSfxfYXvEqkXPvcuS4go3aV")));
            Console.WriteLine(test2);
            test2 = json.InvokeMethod("getblocktemplate",
                                      new JObject(new JProperty("reserve_size", 4),
                                                  new JProperty("wallet_address",
                                                                "41jhre5xFk92GYaJgxvHuzUC5uZtQ4UDU1APv3aRAc27DWBqKEzubC2WSvmnbxaswLdB1BsQnSfxfYXvEqkXPvcuS4go3aV")));
            Console.WriteLine(test2);
                /*test2 = json.InvokeMethod("getblocktemplate", new JObject(new JProperty("reserve_size", 4), new JProperty("wallet_address", "41jhre5xFk92GYaJgxvHuzUC5uZtQ4UDU1APv3aRAc27DWBqKEzubC2WSvmnbxaswLdB1BsQnSfxfYXvEqkXPvcuS4go3aV")));
            Console.WriteLine(test2); test2 = json.InvokeMethod("getblocktemplate", new JObject(new JProperty("reserve_size", 4), new JProperty("wallet_address", "41jhre5xFk92GYaJgxvHuzUC5uZtQ4UDU1APv3aRAc27DWBqKEzubC2WSvmnbxaswLdB1BsQnSfxfYXvEqkXPvcuS4go3aV")));
            Console.WriteLine(test2);
            byte[] test = new byte[32];
            Thread t = new Thread(() =>
                {
                    test = Crypto.CnSlowHash(ASCIIEncoding.ASCII.GetBytes("hello"), 5);

                }, 1024*1024*8);
            t.Start();
            t.Join();
             */
            Console.WriteLine("beginning listen");

            StartListening();

            while (true)
            {

                Thread.Sleep(5000);
                test2 = json.InvokeMethod("getblockcount");
                CurrentBlockHeight = (int) test2["result"]["count"];

                /*ConnectedClients =
                    ConnectedClients.AsParallel()
                                    .Where(x => (DateTime.Now - x.Value.last_heard).Seconds < 60)
                                    .ToDictionary(x => x.Key, x => x.Value); */
                //, new JObject(new JProperty("reserve_size", 4), new JProperty("wallet_address", "41jhre5xFk92GYaJgxvHuzUC5uZtQ4UDU1APv3aRAc27DWBqKEzubC2WSvmnbxaswLdB1BsQnSfxfYXvEqkXPvcuS4go3aV")));

            }
        }

        private static int CurrentBlockHeight;

        private static async void StartListening()
        {
            // ip
            //  TcpListener listener = new TcpListener(IPAddress.Any, 7707);
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(config.IniReadValue("http-server"));
            //  listener.
            listener.Start();
            while (true)
            {
                HttpListenerContext client = await listener.GetContextAsync();
                AcceptClient(client);
            }

        }

        private static string GetRequestBody(HttpListenerRequest Request)
        {
            string documentContents;
            using (Stream receiveStream = Request.InputStream)
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
                             .Where(x => x%2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static void IncreaseShareCount(string address)
        {
            RedisValue[] blocks = redisDb.SetMembers("blocks");
            if (blocks.Count(x => x == CurrentBlockHeight) == 0)
            {
                redisDb.SetAdd("blocks", CurrentBlockHeight);
            }

            RedisValue[] miners = redisDb.SetMembers("miners");
            if (miners.Count(x => x == address) == 0)
            {
                redisDb.SetAdd("miners", address);
            }


            RedisValue[] blockrewards = redisDb.SetMembers("blockreward");
            bool exists = false;
            blockrewards.ToList().ForEach(x =>
                {
                    HashEntry[] blockreward = redisDb.HashGetAll((string) x);
                    exists =
                        blockreward.Count(x2 => x2.Name == "address" && x2.Value == address) > 0 &&
                        blockreward.Count(x2 => x2.Name == "block" && x2.Value == CurrentBlockHeight) > 0;
                    if (exists)
                        redisDb.HashSet((string) x, "shares", blockreward.First(x2 => x2.Name == "shares").Value + 1);
                });

            if (!exists)
            {
                RedisValue blockreward = Guid.NewGuid().ToString();
                HashEntry[] blockrewardob = new HashEntry[3];
                blockrewardob[0] = new HashEntry("address", address);
                blockrewardob[1] = new HashEntry("block", CurrentBlockHeight);
                blockrewardob[2] = new HashEntry("shares", 1);

                redisDb.SetAdd("blockreward", blockreward);
                redisDb.HashSet((string) blockreward, blockrewardob);
            }
        }

        public static ShareProcess ProcessShare(byte[] blockHash, int blockDifficulty, int shareTarget)
        {

            BigInteger blockDiff =
                new BigInteger(
                    StringToByteArray(
                        "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"))/
                new BigInteger(blockHash.Reverse().ToArray());

            if (blockDiff >= blockDifficulty)
            {
                Console.WriteLine("Block found with hash:{0}", BitConverter.ToString(blockHash).Replace("-", ""));
                return ShareProcess.ValidBlock;

            }
            else if (BitConverter.ToInt32(blockHash, 28) < shareTarget)
            {
                Console.WriteLine("Valid share found with hash:{0}", BitConverter.ToString(blockHash).Replace("-", ""));
                return ShareProcess.ValidShare;
            }
            Console.WriteLine("Invalid share found with hash:{0}", BitConverter.ToString(blockHash).Replace("-", ""));
            return ShareProcess.InvalidShare;
        }

        public static void ProcessBlock(byte[] blockData)
        {
            if ((string) json.InvokeMethod("submitblock", blockData)["result"]["status"] == "OK")
            {
                InitiatePayments();
            }
            else
            {
                Console.WriteLine("Block submittance failed!");
            }
        }

        private static async void AcceptClient(HttpListenerContext client)
        {
            //  client.AcceptWebSocketAsync()
            string abcdef= GetRequestBody(client.Request);

            if(abcdef=="")
                return;

            Console.WriteLine(abcdef);
            JObject request = JObject.Parse(abcdef);

            client.Response.ContentType = "application/json";

            JObject response = new JObject();

            response["id"] = 0;
            response["jsonrpc"] = "2.0";

            string guid;

            if ((string) request["method"] == "login")
                guid = Guid.NewGuid().ToString();
            else
                guid = (string) request["params"]["id"];

            if ((string) request["method"] == "login" ||
                ConnectedClients.ContainsKey(guid) && (string) request["method"] == "getjob")
            {
                JObject result = new JObject();
                if ((string) request["method"] == "login")
                {
                    result["id"] = guid;
                }
                JObject job = new JObject();

                JObject blob = json.InvokeMethod("getblocktemplate",
                                                 new JObject(new JProperty("reserve_size", 4),
                                                             new JProperty("wallet_address",
                                                                           config.IniReadValue("wallet-address"))));

                if ((string) request["method"] == "login" ||
                    (int) blob["result"]["height"] >
                    (int) ConnectedClients[guid].work["result"]["height"])
                {
                    if ((string) request["method"] == "getjob")
                    {
                        worker abc = ConnectedClients[guid];
                        abc.work = blob;
                        ConnectedClients[guid] = abc; //.work = blob;
                    }
                    else
                    {
                        worker worker = new worker();
                        //worker.guid = guid;
                        worker.address = (string) request["params"]["login"];
                        worker.work = blob;
                        Console.WriteLine("Adding {0} to connected clients", guid);
                        try
                        {
                            ConnectedClients.Add(guid, worker);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                    }
                    job["blob"] = (string) blob["result"]["blocktemplate_blob"];
                    job["job_id"] = Guid.NewGuid().ToString();
                    job["target"] = config.IniReadValue("miner-target-hex");

                }
                else
                {
                    job["blob"] = "";
                    job["job_id"] = "";
                    job["target"] = "";
                }
                result["job"] = job;
                if ((string) request["method"] == "login")
                {
                    result["status"] = "OK";
                }
                response["result"] = result;

            }
            else if (ConnectedClients.ContainsKey(guid) &&
                     (string) request["method"] == "submit")
            {
                JObject prevjob =
                    ConnectedClients[guid].work;

                byte[] nonce = StringToByteArray((string) request["params"]["nonce"]);

                byte[] blockdata = StringToByteArray((string) prevjob["result"]["blocktemplate_blob"]);

                Array.Copy(nonce, 0, blockdata, 39, nonce.Length);
                JObject result = new JObject();
                byte[] blockHash = CryptoNight(blockdata);

                //Console.WriteLine(BitConverter.ToString(test).Replace("-", ""));

                if (((string) request["params"]["result"]).ToUpper() !=
                    BitConverter.ToString(blockHash).Replace("-", ""))
                    throw new Exception();


                ShareProcess shareProcess =
                    ProcessShare(blockHash, (int) prevjob["result"]["difficulty"],
                                 BitConverter.ToInt32(StringToByteArray(config.IniReadValue("miner-target-hex")), 0));

                string address = ConnectedClients[guid].address;

                if (shareProcess == ShareProcess.ValidShare || shareProcess == ShareProcess.ValidBlock)
                {
                    try
                    {
                        IncreaseShareCount(address);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        throw;
                    }
                    if (shareProcess == ShareProcess.ValidBlock)
                    {
                        ProcessBlock(blockdata);
                    }
                    result["status"] = "OK";
                }
                else
                    result["status"] = "NOTOK";

                response["result"] = result;

                // Console.WriteLine(response.ToString());

            }

            worker abc2 = ConnectedClients[guid];
            abc2.last_heard = DateTime.Now;
            ConnectedClients[guid] = abc2;
            string s = JsonConvert.SerializeObject(response);

            // Console.WriteLine(s);

            byte[] byteArray = Encoding.UTF8.GetBytes(s);

            client.Response.ContentLength64 = byteArray.Length;
            client.Response.OutputStream.Write(byteArray, 0, byteArray.Length);

            client.Response.OutputStream.Close();

            //client.Response.Close();

        }

        private static async void InitiatePayments()
        {

            Dictionary<string, long> sharePerAddress = new Dictionary<string, long>();
            int lastPaidBlock = 0;

            long totalShares = 0;

            try
            {
                lastPaidBlock = int.Parse(redisDb.StringGet("lastpaidblock"));
                redisDb.StringSet("lastpaidblock", CurrentBlockHeight);
            }
            catch
            {
                lastPaidBlock = 0;
                redisDb.StringSet("lastpaidblock", CurrentBlockHeight);
            }

            RedisValue[] blockrewards = redisDb.SetMembers("blockreward");
            blockrewards.ToList().ForEach(x =>
                {
                    HashEntry[] blockreward = redisDb.HashGetAll((string) x);
                    if (blockreward[1].Value != CurrentBlockHeight.ToString())
                    {
                    }
                    else
                    {
                        if (!sharePerAddress.ContainsKey(blockreward[0].Value))
                        {
                            sharePerAddress.Add(blockreward[0].Value, 0);
                        }
                        sharePerAddress[blockreward[0].Value] += int.Parse(blockreward[2].Value);
                    }
                });

            JObject blockHeader = json.InvokeMethod("getblockheaderbyheight", CurrentBlockHeight);

            long reward = (long) blockHeader["result"]["blockheader"]["reward"];

            double rewardPerShare = (double) reward/((double) (102*totalShares)/100);

            JObject param = new JObject();

            JArray destinations = new JArray();

            foreach (KeyValuePair<string, long> addressShare in sharePerAddress)
            {
                JObject destination = new JObject();
                destination["amount"] = (long) (addressShare.Value*rewardPerShare);
                destination["address"] = addressShare.Key;

                destinations.Add(destination);
            }

            param["destinations"] = destinations;

            param["fee"] = 0;
            param["mixin"] = 0;
            param["unlock_time"] = 0;

            JObject transfer = Walletjson.InvokeMethod("transfer", param);

            Console.WriteLine(transfer);
        }
    }
}
