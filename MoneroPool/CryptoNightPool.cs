using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace MoneroPool
{
    public class CryptoNightPool
    {
       public CryptoNightPool()
        {

        }

       public async void Start()
       {
           Logger.Log(Logger.LogLevel.General, "Beginning Listen!");

           HttpListener listener = new HttpListener();
           listener.Prefixes.Add(Statics.Config.IniReadValue("http-server"));
           listener.Start();
           while (true)
           {
               HttpListenerContext client = await listener.GetContextAsync();
               AcceptClient(client);
           }   
       }

        public void IncreaseShareCount(string guid)
        {
            ConnectedWorker worker = Statics.ConnectedClients[guid];
            double shareValue = (double) worker.CurrentDifficulty/
                                int.Parse(Statics.Config.IniReadValue("base-difficulty"));

            Block block;
            if (Statics.RedisDb.Blocks.Any(x => x.BlockHeight == Statics.CurrentBlockHeight))
            {
                block = Statics.RedisDb.Blocks.First(x => x.BlockHeight == Statics.CurrentBlockHeight);
            }
            else
            {
                block = Statics.RedisDb.Blocks.First(x => x.BlockHeight == Statics.CurrentBlockHeight);   
            }
            Miner miner;
            if (Statics.RedisDb.Miners.Any(x => x.Address == worker.Address))
            {
                miner = Statics.RedisDb.Miners.First(x => x.Address == worker.Address);
            }
            else
            {
                miner = new Miner(worker.Address, 0);
            }
            foreach (var fBlockReward in Statics.RedisDb.BlockRewards)
            {
                if (fBlockReward.Block == block.Identifier && fBlockReward.Miner == miner.Identifier)
                {
                    Share fShare = new Share(fBlockReward.Identifier, shareValue);
                    fBlockReward.Shares.Add(fShare.Identifier);

                    Statics.RedisDb.SaveChanges(fBlockReward);
                    Statics.RedisDb.SaveChanges(fShare);
                    Statics.RedisDb.SaveChanges(miner);
                    Statics.RedisDb.SaveChanges(block);
                    return;
                }    
            }

            BlockReward blockReward = new BlockReward(miner.Identifier,block.Identifier);
            Share share = new Share(blockReward.Identifier, shareValue);
            blockReward.Shares.Add(share.Identifier);

            Statics.RedisDb.SaveChanges(blockReward);
            Statics.RedisDb.SaveChanges(share);
            Statics.RedisDb.SaveChanges(miner);
            Statics.RedisDb.SaveChanges(block);
        }

        public void GenerateSubmitResponse(ref JObject response, string guid, byte[] nonce, string resultHash)
       {
           ConnectedWorker worker = Statics.ConnectedClients[guid];

           byte[] prevJobBlock = Helpers.GenerateShareWork(worker.JobSeed);

           Array.Copy(nonce, 0, prevJobBlock, 39, nonce.Length);
           JObject result = new JObject();
           byte[] blockHash = Hash.CryptoNight(prevJobBlock);


           if (resultHash.ToUpper() != BitConverter.ToString(blockHash).Replace("-", ""))
           {
               Console.WriteLine("Hash mismatch from {0}", guid);

               result["status"] = "Not ok?";
               //    throw new Exception();
           }
           else
           {

               ShareProcess shareProcess =
                   Helpers.ProcessShare(blockHash, (int) Statics.CurrentBlockTemplate["difficulty"], worker.CurrentDifficulty);

               string address = Statics.ConnectedClients[guid].Address;

               if (shareProcess == ShareProcess.ValidShare || shareProcess == ShareProcess.ValidBlock)
               {
                   try
                   {
                       IncreaseShareCount(guid);
                   }
                   catch (Exception e)
                   {
                       Logger.Log(Logger.LogLevel.Error, e.ToString());
                       throw;
                   }
                   if (shareProcess == ShareProcess.ValidBlock)
                   {
                       Statics.BlocksPendingSubmition.Add(new PoolBlock(prevJobBlock, Statics.CurrentBlockHeight, BitConverter.ToString(blockHash)));
                   }
                   result["status"] = "OK";
               }
               else
                   result["status"] = "NOTOK";

           }
           response["result"] = result;

       }

       public void GenerateGetWorkResponse(ref JObject response, string guid)
       {
           JObject result = new JObject();
           JObject job = new JObject();

           ConnectedWorker worker = Statics.ConnectedClients.First(x => x.Key == guid).Value;
           worker.LastSeen = DateTime.Now;
           worker.CurrentDifficulty =
               Helpers.GetTargetFromDifficulty(uint.Parse(Statics.Config.IniReadValue("miner-start-difficulty")));


           Logger.Log(Logger.LogLevel.General, "Getwork request from {0}", guid);

           result["id"] = guid;

           int seed = 0;

           job["blob"] = Helpers.GenerateUniqueWork(ref seed);
           worker.JobSeed = seed;

           job["job_id"] = Guid.NewGuid().ToString();
           job["target"] = BitConverter.ToString(BitConverter.GetBytes(Helpers.GetTargetFromDifficulty(worker.CurrentDifficulty))).Replace("-", "");

           result["job"] = job;

           response["result"] = result;

           worker.NewJobRequest();

           Statics.ConnectedClients[guid] = worker;          

       }

       public void GenerateLoginResponse(ref JObject response, string guid, string address)
        {
            JObject result = new JObject();
            JObject job = new JObject();


            ConnectedWorker worker = new ConnectedWorker();
            worker.Address = address;
            worker.LastSeen = DateTime.Now;
            worker.CurrentDifficulty =
                Helpers.GetTargetFromDifficulty(uint.Parse(Statics.Config.IniReadValue("miner-start-difficulty")));


            Logger.Log(Logger.LogLevel.General, "Adding {0} to connected clients", guid);

            result["id"] = guid;

            int seed = 0;

            job["blob"] = Helpers.GenerateUniqueWork(ref seed);
            worker.JobSeed = seed;

            job["job_id"] = Guid.NewGuid().ToString();
            job["target"] =BitConverter.ToString(BitConverter.GetBytes(Helpers.GetTargetFromDifficulty(worker.CurrentDifficulty))).Replace("-","");

            result["job"] = job;
            result["status"] = "OK";

            response["result"] = result; 

            worker.NewJobRequest();
            
            Statics.ConnectedClients.Add(guid,worker);

            //Initialize new client in DB
            if (Statics.RedisDb.Miners.Any(x => x.Address == worker.Address))
            {
                Miner miner = Statics.RedisDb.Miners.First(x => x.Address == worker.Address);
                miner.MinersWorker.Add("DEMO");
                Statics.RedisDb.SaveChanges(miner);
            }
            else
            {
                Miner miner = new Miner(worker.Address,0);
                miner.MinersWorker.Add("DEMO");
                Statics.RedisDb.SaveChanges(miner);
            }

        }

       public async void AcceptClient(HttpListenerContext client)
        {
            try
            {
                string sRequest = Helpers.GetRequestBody(client.Request);

                if (sRequest == "")
                    return;

                JObject request = JObject.Parse(sRequest);

                client.Response.ContentType = "application/json";

                JObject response = new JObject();

                response["id"] = 0;
                response["jsonrpc"] = "2.0";

                string guid;

                if ((string) request["method"] == "login")
                    guid = Guid.NewGuid().ToString();
                else
                    guid = (string) request["params"]["id"];

                switch ((string) request["method"])
                {
                    case "login":
                        GenerateLoginResponse(ref response, guid,(string) request["params"]["login"]);
                        break;
                    case "getwork":
                        GenerateGetWorkResponse(ref response, guid);
                        break;
                    case "submit":
                        GenerateSubmitResponse(ref response, guid, Helpers.StringToByteArray((string)request["params"]["nonce"]), (string) request["params"]["result"]);
                        break;
                }

                string s = JsonConvert.SerializeObject(response);
                byte[] byteArray = Encoding.UTF8.GetBytes(s);

                client.Response.ContentLength64 = byteArray.Length;
                client.Response.OutputStream.Write(byteArray, 0, byteArray.Length);

                client.Response.OutputStream.Close();

            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogLevel.Error, e.ToString());
            }
        }
    }
}
