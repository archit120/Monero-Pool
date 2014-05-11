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

namespace MoneroPool
{
    internal struct worker
    {
        public string address;
        public string guid;
    }

    internal class Program
    {
        public enum ShareProcess{ValidShare, ValidBlock, InvalidShare}

        private static JsonRPC json = new JsonRPC("http://127.0.0.1:18081");

        private static Dictionary<worker, JObject> ConnectedClients = new Dictionary<worker, JObject>();

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

            JObject test2 = json.InvokeMethod("getblockcount");
            CurrentBlockHeight = (int) test2["result"]["count"];
                //, new JObject(new JProperty("reserve_size", 4), new JProperty("wallet_address", "41jhre5xFk92GYaJgxvHuzUC5uZtQ4UDU1APv3aRAc27DWBqKEzubC2WSvmnbxaswLdB1BsQnSfxfYXvEqkXPvcuS4go3aV")));
            Console.WriteLine(test2);
            test2 = json.InvokeMethod("getblocktemplate",
                                      new JObject(new JProperty("reserve_size", 1),
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

                Thread.Sleep(1000);
                test2 = json.InvokeMethod("getblockcount");
                CurrentBlockHeight = (int) test2["result"]["count"];
                    //, new JObject(new JProperty("reserve_size", 4), new JProperty("wallet_address", "41jhre5xFk92GYaJgxvHuzUC5uZtQ4UDU1APv3aRAc27DWBqKEzubC2WSvmnbxaswLdB1BsQnSfxfYXvEqkXPvcuS4go3aV")));

            }
        }

        private static int CurrentBlockHeight;

        private static async void StartListening()
        {
            // ip
            //  TcpListener listener = new TcpListener(IPAddress.Any, 7707);
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(string.Format("http://{0}:{1}/", "127.0.0.1", "7707"));
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
            using (minersEntities entities = new minersEntities())
            {
                bool exist = false;
                try
                {
                    exist = entities.BlockRewards.Count(x => x.Miner.Address == address) > 0;
                }
                catch (Exception e)
                {
                }
                if (exist)
                {
                    entities.BlockRewards.FirstOrDefault(x => x.Miner.Address == address).Shares++;
                }
                else
                {
                    Block block;
                    BlockReward blockReward = new BlockReward();
                    if (entities.Blocks.Count(x => x.Id == CurrentBlockHeight) == 0)
                    {
                        block = new Block();
                        block.Id = CurrentBlockHeight;
                        entities.Blocks.Add(block);
                    }
                    else
                    {
                        block = entities.Blocks.FirstOrDefault(x => x.Id == CurrentBlockHeight);
                    }
                    blockReward.Block = block;
                    blockReward.Shares++;
                    Miner miner = new Miner();
                    miner.Address = address;

                    entities.BlockRewards.Add(blockReward);
                    blockReward.Miner = miner;
                    entities.Miners.Add(miner);

                }
                entities.SaveChanges();
            }
        }

        public static ShareProcess ProcessShare(byte[] blockHash, int blockDifficulty, int shareTarget)
        {

            BigInteger blockDiff =
                new BigInteger(
                    StringToByteArray(
                        "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"))/new BigInteger(blockHash.Reverse().ToArray());

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
            if ((string)json.InvokeMethod("submitblock", blockData)["result"]["status"] == "OK")
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
            JObject request = JObject.Parse(GetRequestBody(client.Request));

            client.Response.ContentType = "application/json";

            JObject response = new JObject();

            response["id"] = 0;
            response["jsonrpc"] = "2.0";
            if ((string) request["method"] == "login" ||
                ConnectedClients.Count(x => x.Key.guid == (string) request["params"]["id"]) > 0 &&
                (string) request["method"] == "getjob")
            {
                JObject result = new JObject();
                string guid = Guid.NewGuid().ToString();
                if ((string) request["method"] == "login")
                {
                    result["id"] = guid;

                }
                JObject job = new JObject();

                JObject blob = json.InvokeMethod("getblocktemplate",
                                                 new JObject(new JProperty("reserve_size", 4),
                                                             new JProperty("wallet_address",
                                                                           "41jhre5xFk92GYaJgxvHuzUC5uZtQ4UDU1APv3aRAc27DWBqKEzubC2WSvmnbxaswLdB1BsQnSfxfYXvEqkXPvcuS4go3aV")));

                if ((string) request["method"] == "login" ||
                    (int) blob["result"]["height"] >
                    (int)
                    ConnectedClients.First(x => x.Key.guid == (string) request["params"]["id"]).Value["result"]["height"
                        ])
                {
                    if ((string) request["method"] == "getjob")
                    {
                        ConnectedClients[ConnectedClients.First(x => x.Key.guid == (string) request["params"]["id"]).Key
                            ] = blob;
                    }
                    else
                    {
                        worker worker = new worker();
                        worker.guid = guid;
                        worker.address = (string) request["params"]["login"];
                        ConnectedClients.Add(worker, blob);
                    }
                    job["blob"] = (string) blob["result"]["blocktemplate_blob"];
                    job["job_id"] = Guid.NewGuid().ToString();
                    job["target"] = "33333303";

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
            else if (ConnectedClients.Count(x => x.Key.guid == (string) request["params"]["id"]) > 0 &&
                     (string) request["method"] == "submit")
            {
                JObject prevjob =
                    ConnectedClients[ConnectedClients.First(x => x.Key.guid == (string) request["params"]["id"]).Key
                        ];

                byte[] nonce = StringToByteArray((string) request["params"]["nonce"]);

                byte[] blockdata = StringToByteArray((string) prevjob["result"]["blocktemplate_blob"]);

                Array.Copy(nonce, 0, blockdata, 39, nonce.Length);
                JObject result = new JObject();
                byte[] blockHash = CryptoNight(blockdata);

                //Console.WriteLine(BitConverter.ToString(test).Replace("-", ""));

                if (((string)request["params"]["result"]).ToUpper() != BitConverter.ToString(blockHash).Replace("-", ""))
                    throw new Exception();


                ShareProcess shareProcess =
                    ProcessShare(blockHash, (int)prevjob["result"]["difficulty"],
                                 BitConverter.ToInt32(StringToByteArray("33333303"), 0));

                string address = ConnectedClients.First(
                       x => x.Key.guid ==
                            (string)request["params"]["id"]).Key.address;

                if (shareProcess == ShareProcess.ValidShare || shareProcess == ShareProcess.ValidBlock)
                {
                    IncreaseShareCount(address);  
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
            string s = JsonConvert.SerializeObject(response);

            // Console.WriteLine(s);

            byte[] byteArray = Encoding.UTF8.GetBytes(s);

            client.Response.ContentLength64 = byteArray.Length;
            client.Response.OutputStream.Write(byteArray, 0, byteArray.Length);

            client.Response.OutputStream.Close();

            client.Response.Close();

        }

        private static async void InitiatePayments()
        {
            using (minersEntities minersEntities = new minersEntities())
            {
                Dictionary<string, long> sharePerAddress = new Dictionary<string, long>();

               // int lastPaidBlock = minersEntities.

                foreach (Miner miner in minersEntities.Miners)
                {
                    
                }
            }
        }
    }
}