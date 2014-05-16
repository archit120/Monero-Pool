using System;
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
        private static ConnectionMultiplexer redis;

        private static RedisPoolDatabase redisDb;
        private static IniFile config = new IniFile("config.txt");

        public static byte[] CryptoNight(byte[] data)
        {
            byte[] test = new byte[32];
            //byte[] test2 = new byte[32];
            Thread t = new Thread(
                () =>
                    { NativeFunctions.cn_slow_hash(data, (ulong) data.Length, test);                     
                      //  NativeFunctions.cn_slow_hash_win_32(data,(uint) data.Length, test2);

                    },
                1024*1024*8);
            t.Start();
            t.Join();

            /*for (int i = 0; i < 32; i++)
            {
                if(test[i]!=test2[i])
                    throw new Exception();
            }    */
            return test;
        }

        private static uint SetCompact(uint nCompact)
        {
            BigInteger val = new BigInteger();
            uint nSize = nCompact >> 24;
            bool fNegative = (nCompact & 0x00800000) != 0;
            uint nWord = nCompact & 0x007fffff;
            if (nSize <= 3)
            {
                nWord >>= (int) (8*(3 - nSize));
                val = new BigInteger(nWord);
            }
            else
            {
                val = new BigInteger(nWord);
                val <<= (int) (8*(nSize - 3));
            }
            if (fNegative)
                return 0;
            return BitConverter.ToUInt32(val.ToByteArray(),val.ToByteArray().Length - 4);
        }

        public static int BN_num_bytes(BigInteger number)
        {
            if (number == 0)
            {
                return 0;
            }
            return 1 + (int)Math.Floor(BigInteger.Log(BigInteger.Abs(number), 2)) / 8;
        }

        public static ulong GetCompact(uint Target)
        {
            uint nSize = (uint)BN_num_bytes(Target);
            ulong nCompact = 0;
            if (nSize <= 3)
                nCompact = Target << (int)(8 * (3 - nSize));
            else
            {
                BigInteger big;
                big = Target >> (int)(8 * (nSize - 3));
                nCompact = (ulong)big;
            }
            // The 0x00800000 bit denotes the sign.
            // Thus, if it is already set, divide the mantissa by 256 and increase the exponent.
            if ((nCompact & 0x00800000) == 1)
            {
                nCompact >>= 8;
                nSize++;
            }
            nCompact |= nSize << 24;
            nCompact |= (ulong)((Target & 0x00800000) != 0 ? 0x00800000 : 0);
            return nCompact;
        }
        public static ulong setCompact(uint nCompact)
        {
            uint nSize = nCompact >> 24;
            bool fNegative = (nCompact & 0x00800000) != 0;
            uint nWord = nCompact & 0x007fffff;
            byte[] hashTarget = new byte[32];
            if (nSize <= 3)
            {
                nWord >>= (int)(8 * (3 - nSize));
                hashTarget[0] = (byte)nWord;
            }
            else
            {
                hashTarget[0] = (byte)nWord;
                for (uint f = 0; f < (nSize - 3); f++)
                {
                    // shift by one byte
                    hashTarget[7] = (byte)((hashTarget[7] << 8) | (hashTarget[6] >> 24));
                    hashTarget[6] = (byte)((hashTarget[6] << 8) | (hashTarget[5] >> 24));
                    hashTarget[5] = (byte)((hashTarget[5] << 8) | (hashTarget[4] >> 24));
                    hashTarget[4] = (byte)((hashTarget[4] << 8) | (hashTarget[3] >> 24));
                    hashTarget[3] = (byte)((hashTarget[3] << 8) | (hashTarget[2] >> 24));
                    hashTarget[2] = (byte)((hashTarget[2] << 8) | (hashTarget[1] >> 24));
                    hashTarget[1] = (byte)((hashTarget[1] << 8) | (hashTarget[0] >> 24));
                    hashTarget[0] = (byte)((hashTarget[0] << 8));
                }
            }
            if (fNegative)
            {
                // if negative bit set, set zero hash
                for (uint i = 0; i < 8; i++)
                    hashTarget[i] = 0;
            }
            return BitConverter.ToUInt64(hashTarget, 23);
        }

        static uint swapEndianness(uint x)
        {
            return ((x & 0x000000ff) << 24) +  // First byte
                   ((x & 0x0000ff00) << 8) +   // Second byte
                   ((x & 0x00ff0000) >> 8) +   // Third byte
                   ((x & 0xff000000) >> 24);   // Fourth byte
        }

    private static void Main(string[] args)
    {



     
                     List<byte> test = new List<byte>(StringToByteArray(
          "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00"));
            //test.Add(0x00);

            BigInteger diff1 = new BigInteger(test.ToArray());

        for (uint i = 2; i < 1025; i++)
        {

              int shift = 29;

      /* convert diff to nBits */

      /*
	this currently is not exact reverse of the other conversions
	for larg diff
      */

      double ftarg = (double)0x0000ffff / i; 
      /* more accurate but not how bitcoin does it */
      /* new version of bitcoin should do this */

      while (ftarg < (double)0x00008000) {
	shift--;
	ftarg *= 256.0;
      }

      while (ftarg >= (double)0x00800000) {
	shift++;
	ftarg /= 256.0;
      }

      //    printf("normalized diff %g, shift %d\n", ftarg, shift);

      uint nBits = (uint)(ftarg + (shift << 24));

            Console.WriteLine(SetCompact(nBits));//target));

            
        }
     

            //byte[] testing = idk.ToByteArray();

            //idk = new BigInteger(testing.AsEnumerable().Reverse().ToArray());
            Walletjson = new JsonRPC(config.IniReadValue("wallet-json-rpc"));

        /*ConfigurationOptions config = new ConfigurationOptions();
        config.EndPoints.Add(new IPEndPoint(i));
        redis = ConnectionMultiplexer.Connect(new ConfigurationOptions());new Ipconfig.IniReadValue("redis-server"));    */

            redisDb = new RedisPoolDatabase(redis.GetDatabase(int.Parse(config.IniReadValue("redis-database"))));

        Statics.json = new JsonRPC(config.IniReadValue());
            Statics.Walletjson = new JsonRPC(config.IniReadValue("wallet-json-rpc"));

            JObject test2 = json.InvokeMethod("getblockcount");
            CurrentBlockHeight = (int) test2["result"]["count"];

            Console.WriteLine("beginning listen");

            StartListening();

            while (true)
            {
             /*   ConnectedClients =
                    ConnectedClients.AsParallel()
                                    .Where(
                                        x =>
                                        (DateTime.Now - x.Value.last_heard).Seconds <
                                        int.Parse(config.IniReadValue("client-timeout-seconds"))).ToDictionary(x=>x.Key,x=>x.Value);  */


                Thread.Sleep(5000);
                test2 = json.InvokeMethod("getblockcount");
                CurrentBlockHeight = (int) test2["result"]["count"];

             }
        }

        public static int CurrentBlockHeight;

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
            redisDb.UpdateLists();
            List<byte> test = new List<byte>(StringToByteArray(
          "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"));
            test.Add(0x00);
            BigInteger diff = new BigInteger(test.ToArray());

            BigInteger idk = diff/512;

            byte[] testing = idk.ToByteArray();

            idk = new BigInteger(testing.AsEnumerable().Reverse().ToArray());

            Block Block;
            Miner Miner;
            BlockReward BlockReward;

            try
            {
                if (!redisDb.Blocks.Any(block => block.BlockHeight == CurrentBlockHeight))
                {
                    Block = new Block(CurrentBlockHeight);
                    Console.WriteLine("New block with id:{0} and identifier:{1}", Block.BlockHeight, Block.Identifier);
                }
                else
                {
                    Block = redisDb.Blocks.First(block => block.BlockHeight == CurrentBlockHeight);
                    Console.WriteLine("Existing block with id:{0} and identifier:{1}", Block.BlockHeight, Block.Identifier);

                }

                if (!redisDb.Miners.Any(miner => miner.Address == address))
                {
                    Miner = new Miner(address, 0);
                    Console.WriteLine("New Miner with address:{0} and identifier:{1}", Miner.Address, Miner.Identifier);

                }
                else
                {
                    Miner = redisDb.Miners.First(miner => miner.Address == address);
                    Console.WriteLine("Existing Miner with address:{0} and identifier:{1}", Miner.Address, Miner.Identifier);

                }

                if (
                    !redisDb.BlockRewards.Any(
                        blockreward => blockreward.Block == Block.Identifier && blockreward.Miner == Miner.Identifier))
                {
                    BlockReward blockReward = new BlockReward(Miner.Identifier, Block.Identifier);
                    Share share = new Share(blockReward.Identifier, 1);

                    blockReward.Shares.Add(share.Identifier);

                    Miner.BlockReward.Add(blockReward.Identifier);

                    Block.BlockRewards.Add(blockReward.Identifier);

                    redisDb.SaveChanges(Block);
                    redisDb.SaveChanges(blockReward);
                    redisDb.SaveChanges(share);
                    redisDb.SaveChanges(Miner);

                }
                else
                {
                    BlockReward blockReward = redisDb.BlockRewards.First(
                        blockreward => blockreward.Block == Block.Identifier && blockreward.Miner == Miner.Identifier);

                    Share share = new Share(blockReward.Identifier, 1);

                    blockReward.Shares.Add(share.Identifier);

                    Miner.BlockReward.Add(blockReward.Identifier);

                    Block.BlockRewards.Add(blockReward.Identifier);

                    redisDb.SaveChanges(Block);
                    redisDb.SaveChanges(blockReward);
                    redisDb.SaveChanges(share);
                    redisDb.SaveChanges(Miner);
                }
            }
            catch (Exception e)
            {
                
            }
        }

        public static ShareProcess ProcessShare(byte[] blockHash, int blockDifficulty, int shareTarget)
        {

            List<byte> test = new List<byte>(StringToByteArray(
                "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"));
            test.Add(0x00);
            BigInteger diff = new BigInteger(test.ToArray());
            List<byte> test2 = blockHash.Reverse().ToList();
            test2.Add(0x00);
            BigInteger block = new BigInteger(test2.ToArray());
            BigInteger blockDiff = diff / block;
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

           // Console.WriteLine(abcdef);
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

            try
            {
                if ((string) request["method"] == "login" ||
                    ConnectedClients.ContainsKey(guid) && (string) request["method"] == "getjob")
                {
                    JObject result = new JObject();
                    if ((string) request["method"] == "login")
                    {
                        Console.WriteLine("login request from {0}", guid);
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
                            Console.WriteLine("getjob request from {0}", guid);

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

                    //Console.WriteLine(((string)request["params"]["result"]).ToUpper() + " vs " + BitConverter.ToString(blockHash).Replace("-", "") +" for "+BitConverter.ToString(blockdata).Replace("-"));
                    if (((string) request["params"]["result"]).ToUpper() !=
                        BitConverter.ToString(blockHash).Replace("-", ""))
                    {
                        Console.WriteLine("Something's wrong");
                        
                    //    throw new Exception();
                    }


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
            catch(Exception e)
            {
                ConnectedClients.Remove(guid);
                Console.WriteLine("Exception : {0}",e.Message);
            }

        }

        private static async void InitiatePayments()
        {

            Dictionary<string, double> sharePerAddress = new Dictionary<string, double>();
            int lastPaidBlock = 0;

            long totalShares = 0;

            try
            {
                lastPaidBlock = int.Parse(redisDb.RedisDb.StringGet("lastpaidblock"));
                redisDb.RedisDb.StringSet("lastpaidblock", CurrentBlockHeight);
            }
            catch
            {
                lastPaidBlock = 0;
                redisDb.RedisDb.StringSet("lastpaidblock", CurrentBlockHeight);
            }

            redisDb.UpdateLists();

            foreach (var miner in redisDb.Miners)
            {
                sharePerAddress.Add(miner.Address, 0);
                foreach (
                    var blockreward in
                        redisDb.BlockRewards.Where(
                            x =>
                            x.Miner == miner.Identifier &&
                            redisDb.Blocks.First(x2 => x2.Identifier == x.Block).BlockHeight > lastPaidBlock))
                {
                    double shares = 0;
                    foreach (var share in redisDb.Shares)
                    {
                        shares += share.Value;
                    }

                    sharePerAddress[miner.Address] = shares;

                }
            }

            JObject blockHeader = json.InvokeMethod("getblockheaderbyheight", CurrentBlockHeight);

            long reward = (long) blockHeader["result"]["blockheader"]["reward"];

            int fee = 100 + int.Parse(config.IniReadValue("pool-fee"));

            double rewardPerShare = (double) reward/((double) (fee*totalShares)/100);

            JObject param = new JObject();

            JArray destinations = new JArray();

            foreach (KeyValuePair<string, double> addressShare in sharePerAddress)
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
