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

           Mono.Net.HttpListener listener = new Mono.Net.HttpListener();
           listener.Prefixes.Add(Statics.Config.IniReadValue("http-server"));
           listener.Start();
           while (true)
           {
               Mono.Net.HttpListenerContext client = await listener.GetContextAsync();
               if (Statics.RedisDb.Bans.Any(x => x.IpBan == client.Request.UserHostAddress))
               {
                   Ban ban = Statics.RedisDb.Bans.First(x => x.IpBan == client.Request.UserHostAddress);
                   if((DateTime.Now - ban.Begin).Minutes > ban.Minutes)
                   {
                       AcceptClient(client);
                   }
                   else
                   {
                       Logger.Log(Logger.LogLevel.General, "Reject ban client from ip {0}",client.Request.UserHostAddress);
                   }
               }
               else
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
                block = new Block(Statics.CurrentBlockHeight);
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

            miner.BlockReward.Add(blockReward.Identifier);
            
            block.BlockRewards.Add(blockReward.Identifier);

            Statics.RedisDb.SaveChanges(blockReward);
            Statics.RedisDb.SaveChanges(share);
            Statics.RedisDb.SaveChanges(miner);
            Statics.RedisDb.SaveChanges(block);     
        }

        public bool GenerateSubmitResponse(ref JObject response, string guid, byte[] nonce, string resultHash, Mono.Net.HttpListenerContext client)
       {
           ConnectedWorker worker = Statics.ConnectedClients[guid];

            Statics.TotalShares++;


            worker.ShareRequest(worker.CurrentDifficulty);
            Statics.RedisDb.MinerWorkers.First(x=>x.Identifier==guid).ShareRequest(worker.CurrentDifficulty);
           byte[] prevJobBlock = Helpers.GenerateShareWork(worker.JobSeed);

           Array.Copy(nonce, 0, prevJobBlock, 39, nonce.Length);
           JObject result = new JObject();
           byte[] blockHash = Hash.CryptoNight(prevJobBlock);

            Statics.ConnectedClients[guid].LastSeen = DateTime.Now;

           if (resultHash.ToUpper() != BitConverter.ToString(blockHash).Replace("-", ""))
           {
               Logger.Log(Logger.LogLevel.General, "Hash mismatch from {0}", guid);

               result["status"] = "Hash mismatch ";
               //    throw new Exception();
           }
           else
           {

               ShareProcess shareProcess =
                   Helpers.ProcessShare(blockHash, (int) Statics.CurrentBlockTemplate["difficulty"], worker.CurrentDifficulty);

               string address = Statics.ConnectedClients[guid].Address;

               worker.TotalShares++;

               if (shareProcess == ShareProcess.ValidShare || shareProcess == ShareProcess.ValidBlock)
               {
                   Statics.HashRate.Difficulty += worker.CurrentDifficulty;
                   Statics.HashRate.Time = (ulong) ((DateTime.Now - Statics.HashRate.Begin).TotalSeconds);
                   try
                   {
                       IncreaseShareCount(guid);
                   }
                   catch (Exception e)
                   {
                       Logger.Log(Logger.LogLevel.Error, e.ToString());
                       throw;
                   }
                   if (shareProcess == ShareProcess.ValidBlock &&
                       !Statics.BlocksPendingSubmition.Any(x => x.BlockHeight == worker.CurrentBlock))
                   {
                       Statics.BlocksPendingSubmition.Add(new PoolBlock(prevJobBlock, worker.CurrentBlock, "",
                                                                        Statics.ConnectedClients[guid].Address));
                   }
                   result["status"] = "OK";
               }
               else
               {
                   result["status"] = "Share failed valdiation!";
                   worker.RejectedShares++;
                   if ((double) worker.RejectedShares/worker.TotalShares >
                       int.Parse(Statics.Config.IniReadValue("ban-reject-percentage")) && worker.TotalShares > int.Parse(Statics.Config.IniReadValue("ban-after-shares")))
                   {
                       result["status"] = "You're banished!";
                       int minutes = int.Parse(Statics.Config.IniReadValue("ban-time-minutes"));
                       Logger.Log(Logger.LogLevel.General, "Client with address {0} ip {1} banned for {2} minutes for having a reject rate of {3}", worker.Address, client.Request.UserHostAddress,minutes,(double)worker.RejectedShares/worker.TotalShares );
                       Ban ban = new Ban();
                       ban.AddressBan = worker.Address;
                       ban.IpBan = client.Request.UserHostAddress;
                       ban.Begin = DateTime.Now;
                       ban.Minutes = minutes;
                       Statics.RedisDb.SaveChanges(ban);
                       response["result"] = result;
                       return true;

                   }
               }

           }
           response["result"] = result;

            return false;
       }

       public void GenerateGetJobResponse(ref JObject response, string guid)
       {
           JObject job = new JObject();

           ConnectedWorker worker = Statics.ConnectedClients.First(x => x.Key == guid).Value;
           worker.LastSeen = DateTime.Now;
           if(worker.ShareDifficulty.Count>=4)
             worker.CurrentDifficulty = Helpers.WorkerVardiffDifficulty(worker);
           worker.CurrentBlock = Statics.CurrentBlockHeight;


           Logger.Log(Logger.LogLevel.General, "Getwork request from {0}", guid);

           //result["id"] = guid;

           int seed = 0;

           job["blob"] = Helpers.GenerateUniqueWork(ref seed);
           worker.JobSeed = seed;

           job["job_id"] = Guid.NewGuid().ToString();
           job["target"] = BitConverter.ToString(BitConverter.GetBytes(Helpers.GetTargetFromDifficulty(worker.CurrentDifficulty))).Replace("-", "");


           response["result"] = job;

           MinerWorker minerWorker = Statics.RedisDb.MinerWorkers.First(x => x.Identifier == guid);
           minerWorker.NewJobRequest();
           Statics.RedisDb.SaveChanges(minerWorker);
           Statics.ConnectedClients[guid] = worker;          

       }

       public void GenerateLoginResponse(ref JObject response, string guid, string address)
        {
            JObject result = new JObject();
            JObject job = new JObject();

           if (!Helpers.IsValidAddress(address, uint.Parse(Statics.Config.IniReadValue("base58-prefix"))))
           {
               result["status"] = "Invalid Address";
               return;
           }

            ConnectedWorker worker = new ConnectedWorker();
            worker.Address = address;
            worker.LastSeen = DateTime.Now;
            worker.CurrentDifficulty =uint.Parse(Statics.Config.IniReadValue("miner-start-difficulty"));
           worker.CurrentBlock = Statics.CurrentBlockHeight;

            Logger.Log(Logger.LogLevel.General, "Adding {0} to connected clients", guid);

            result["id"] = guid;

            int seed = 0;

            job["blob"] = Helpers.GenerateUniqueWork(ref seed);
            worker.JobSeed = seed;

            job["job_id"] = Guid.NewGuid().ToString();
            job["target"] =BitConverter.ToString(BitConverter.GetBytes(Helpers.GetTargetFromDifficulty(worker.CurrentDifficulty))).Replace("-","");

            Logger.Log(Logger.LogLevel.General, "Sending new work with target {0}", (string)job["target"]);

            result["job"] = job;
            result["status"] = "OK";

            response["result"] = result; 

            worker.NewJobRequest();

            Statics.ConnectedClients.Add(guid,worker);

            //Initialize new client in DB
            if (Statics.RedisDb.Miners.Any(x => x.Address == worker.Address))
            {
                Miner miner = Statics.RedisDb.Miners.First(x => x.Address == worker.Address);
                MinerWorker minerWorker = new MinerWorker(guid, miner.Identifier,0);
                minerWorker.NewJobRequest();
                miner.MinersWorker.Add(guid);
                Statics.RedisDb.SaveChanges(miner);
                Statics.RedisDb.SaveChanges(minerWorker);
            }
            else
            {
                Miner miner = new Miner(worker.Address,0);
                MinerWorker minerWorker = new MinerWorker(guid, miner.Identifier, 0);
                minerWorker.NewJobRequest();
                miner.MinersWorker.Add(guid);
                Statics.RedisDb.SaveChanges(miner); 
                Statics.RedisDb.SaveChanges(minerWorker);
            }

        }

       public async void AcceptClient(Mono.Net.HttpListenerContext client)
        {
            try
            {
                string sRequest = Helpers.GetRequestBody(client.Request);
                //sRequest = Helpers.GetRequestBody(client.Request);
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

                bool close = false;
                switch ((string) request["method"])
                {
                    case "login":
                        GenerateLoginResponse(ref response, guid,(string) request["params"]["login"]);
                        break;
                    case "getjob":
                        GenerateGetJobResponse(ref response, guid);
                        break;
                    case "submit":
                        close = GenerateSubmitResponse(ref response, guid, Helpers.StringToByteArray((string)request["params"]["nonce"]), (string) request["params"]["result"], client);
                        break;
                }

                string s = JsonConvert.SerializeObject(response);
                byte[] byteArray = Encoding.UTF8.GetBytes(s);

                client.Response.ContentLength64 = byteArray.Length;
                client.Response.OutputStream.Write(byteArray, 0, byteArray.Length);

               /* foreach (var b in client.connections)
                {
                    
                } */  
                //client.
                client.Response.OutputStream.Close();
                if(close)
                    client.Response.Close();

            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogLevel.Error, e.ToString());
            }
        }
    }
}
