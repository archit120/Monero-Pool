using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
            Logger.Log(Logger.LogLevel.Debug, "CryptoNightPool declared");

        }

        public async Task HttpServer()
        {
            Mono.Net.HttpListener listener = new Mono.Net.HttpListener();
            listener.Prefixes.Add(Statics.Config.IniReadValue("http-server"));
            listener.Start();
            while (true)
            {
                Mono.Net.HttpListenerContext client = await listener.GetContextAsync();
                if (Statics.RedisDb.Bans.Any(x => x.IpBan == client.Request.UserHostAddress))
                {
                    Ban ban = Statics.RedisDb.Bans.First(x => x.IpBan == client.Request.UserHostAddress);
                    if ((DateTime.Now - ban.Begin).Minutes > ban.Minutes)
                    {
                        AcceptHttpClient(client);
                        Statics.RedisDb.Remove(ban);
                    }
                    else
                    {
                        Logger.Log(Logger.LogLevel.General, "Reject ban client from ip {0}",
                                   client.Request.UserHostAddress);
                    }
                }
                else
                    AcceptHttpClient(client);
            }
        }

        public async Task TcpServer()
        {

            TcpListener listener = new TcpListener(IPAddress.Any,
                                                   int.Parse(Statics.Config.IniReadValue("stratum-server-port")));
            listener.Start();
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                if (
                    Statics.RedisDb.Bans.Any(
                        x => x.IpBan == ((IPEndPoint) client.Client.RemoteEndPoint).Address.ToString()))
                {
                    Ban ban =
                        Statics.RedisDb.Bans.First(
                            x => x.IpBan == ((IPEndPoint) client.Client.RemoteEndPoint).Address.ToString());
                    if ((DateTime.Now - ban.Begin).Minutes > ban.Minutes)
                    {
                        AcceptTcpClient(client);
                        Statics.RedisDb.Remove(ban);
                    }
                    else
                    {
                        Logger.Log(Logger.LogLevel.General, "Reject ban client from ip {0}",
                                   ((IPEndPoint) client.Client.RemoteEndPoint).Address.ToString());
                    }
                }
                else
                    AcceptTcpClient(client);
            }
        }

        public async void Start()
        {
            Logger.Log(Logger.LogLevel.General, "Beginning Listen!");

            HttpServer();

            TcpServer();
        }

        public void IncreaseShareCount(string guid, uint difficulty)
        {
            ConnectedWorker worker = Statics.ConnectedClients[guid];
            double shareValue = (double) difficulty/
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

            BlockReward blockReward = new BlockReward(miner.Identifier, block.Identifier);
            Share share = new Share(blockReward.Identifier, shareValue);
            blockReward.Shares.Add(share.Identifier);

            miner.BlockReward.Add(blockReward.Identifier);

            block.BlockRewards.Add(blockReward.Identifier);

            Statics.RedisDb.SaveChanges(blockReward);
            Statics.RedisDb.SaveChanges(share);
            Statics.RedisDb.SaveChanges(miner);
            Statics.RedisDb.SaveChanges(block);
        }

        public bool GenerateSubmitResponse(ref JObject response, string jobId, string guid, byte[] nonce,
                                           string resultHash, string ipAddress)
        {
            ConnectedWorker worker = Statics.ConnectedClients[guid];

            Statics.TotalShares++;
            JObject result = new JObject();

            if (nonce == null || nonce.Length == 0)
            {
                response["error"] = "Invalid arguments!";
                return false;
            }

            try
            {
                ShareJob shareJob = worker.JobSeed.First(x => x.Key == jobId).Value;
                if (!shareJob.SubmittedShares.Contains(BitConverter.ToInt32(nonce, 0)))
                    shareJob.SubmittedShares.Add(BitConverter.ToInt32(nonce, 0));
                else
                {
                    response["error"] = "Duplicate share";
                    return false;
                }
                int jobSeed = shareJob.Seed;
                worker.ShareRequest(shareJob.CurrentDifficulty);
                Statics.RedisDb.MinerWorkers.First(x => x.Identifier == guid).ShareRequest(shareJob.CurrentDifficulty);
                byte[] prevJobBlock = Helpers.GenerateShareWork(jobSeed, true);

                Array.Copy(nonce, 0, prevJobBlock, 39, nonce.Length);
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
                        Helpers.ProcessShare(blockHash, (int) Statics.CurrentBlockTemplate["difficulty"],
                                             (uint) shareJob.CurrentDifficulty);

                    string address = Statics.ConnectedClients[guid].Address;

                    worker.TotalShares++;

                    if (shareProcess == ShareProcess.ValidShare || shareProcess == ShareProcess.ValidBlock)
                    {
                        Statics.HashRate.Difficulty += (uint) shareJob.CurrentDifficulty;
                        Statics.HashRate.Time = (ulong) ((DateTime.Now - Statics.HashRate.Begin).TotalSeconds);
                        try
                        {
                            IncreaseShareCount(guid, (uint) shareJob.CurrentDifficulty);
                        }
                        catch (Exception e)
                        {
                            Logger.Log(Logger.LogLevel.Error, e.ToString());
                            throw;
                        }
                        if (shareProcess == ShareProcess.ValidBlock &&
                            !Statics.BlocksPendingSubmition.Any(x => x.BlockHeight == worker.CurrentBlock))
                        {
                            Logger.Log(Logger.LogLevel.Special, "Block found by {0}", guid);
                            byte[] shareWork = Helpers.GenerateShareWork(jobSeed, false);
                            Array.Copy(nonce, 0, shareWork, 39, nonce.Length);

                            Statics.BlocksPendingSubmition.Add(new PoolBlock(shareWork, worker.CurrentBlock, "",
                                                                             Statics.ConnectedClients[guid].Address));
                        }
                        result["status"] = "OK";
                    }
                    else
                    {
                        result["status"] = "Share failed valdiation!";
                        worker.RejectedShares++;
                        if ((double) worker.RejectedShares/worker.TotalShares >
                            int.Parse(Statics.Config.IniReadValue("ban-reject-percentage")) &&
                            worker.TotalShares > int.Parse(Statics.Config.IniReadValue("ban-after-shares")))
                        {
                            result["status"] = "You're banished!";
                            int minutes = int.Parse(Statics.Config.IniReadValue("ban-time-minutes"));
                            Logger.Log(Logger.LogLevel.General,
                                       "Client with address {0} ip {1} banned for {2} minutes for having a reject rate of {3}",
                                       worker.Address, ipAddress, minutes,
                                       (double) worker.RejectedShares/worker.TotalShares);
                            Ban ban = new Ban();
                            ban.AddressBan = worker.Address;
                            ban.IpBan = ipAddress;
                            ban.Begin = DateTime.Now;
                            ban.Minutes = minutes;
                            Statics.RedisDb.SaveChanges(ban);
                            response["result"] = result;
                            return true;

                        }
                    }

                }
            }
            catch
            {
                result["error"] = "Invalid job id";
            }
            response["result"] = result;

            return false;
        }

        public static void GenerateGetJobResponse(ref JObject response, string guid)
        {
            JObject job = new JObject();

            ConnectedWorker worker = Statics.ConnectedClients.First(x => x.Key == guid).Value;
            worker.LastSeen = DateTime.Now;
            /*if (worker.ShareDifficulty.Count >= 4)
                worker.LastDifficulty = Helpers.WorkerVardiffDifficulty(worker);  */


            Logger.Log(Logger.LogLevel.General, "Getwork request from {0}", guid);

            //result["id"] = guid;

            int seed = 0;
            if (worker.PendingDifficulty != worker.LastDifficulty || worker.CurrentBlock != Statics.CurrentBlockHeight)
            {
                worker.CurrentBlock = Statics.CurrentBlockHeight;
                worker.LastDifficulty = worker.PendingDifficulty;
                job["blob"] = Helpers.GenerateUniqueWork(ref seed);

                job["job_id"] = Guid.NewGuid().ToString();
                ShareJob shareJob = new ShareJob();
                shareJob.CurrentDifficulty = worker.LastDifficulty;
                shareJob.Seed = seed;
                worker.JobSeed.Add(new KeyValuePair<string, ShareJob>((string) job["job_id"], shareJob));


                if (worker.JobSeed.Count > int.Parse(Statics.Config.IniReadValue("max-concurrent-works")))
                    worker.JobSeed.RemoveAt(0);

                job["target"] =
                    BitConverter.ToString(
                        BitConverter.GetBytes(Helpers.GetTargetFromDifficulty((uint) shareJob.CurrentDifficulty)))
                                .Replace("-", "");

            }
            else
            {
                job["blob"] = "";
                job["job_id"] = "";
                job["target"] = "";
            }
            response["result"] = job;

            MinerWorker minerWorker = Statics.RedisDb.MinerWorkers.First(x => x.Identifier == guid);
            minerWorker.NewJobRequest();
            Statics.RedisDb.SaveChanges(minerWorker);
            Statics.ConnectedClients[guid] = worker;
            Logger.Log(Logger.LogLevel.Verbose, "Finsihed getjob response");

        }

        public void GenerateLoginResponse(ref JObject response, string guid, string address)
        {

            JObject result = new JObject();
            JObject job = new JObject();

            if (!Helpers.IsValidAddress(address, uint.Parse(Statics.Config.IniReadValue("base58-prefix"))))
            {
                result["error"] = "Invalid Address";
                return;
            }

            ConnectedWorker worker = new ConnectedWorker();
            worker.Address = address;
            worker.LastSeen = DateTime.Now;
            worker.LastDifficulty = uint.Parse(Statics.Config.IniReadValue("base-difficulty"));
            worker.CurrentBlock = Statics.CurrentBlockHeight;

            Logger.Log(Logger.LogLevel.General, "Adding {0} to connected clients", guid);

            result["id"] = guid;

            int seed = 0;

            job["blob"] = Helpers.GenerateUniqueWork(ref seed);

            job["job_id"] = Guid.NewGuid().ToString();

            ShareJob shareJob = new ShareJob();
            shareJob.CurrentDifficulty = worker.LastDifficulty;
            shareJob.Seed = seed;
            worker.JobSeed.Add(new KeyValuePair<string, ShareJob>((string) job["job_id"], shareJob));

            job["target"] =
                BitConverter.ToString(
                    BitConverter.GetBytes(Helpers.GetTargetFromDifficulty((uint) shareJob.CurrentDifficulty)))
                            .Replace("-", "");

            Logger.Log(Logger.LogLevel.General, "Sending new work with target {0}", (string) job["target"]);

            result["job"] = job;
            result["status"] = "OK";

            response["result"] = result;

            worker.NewJobRequest();

            Statics.ConnectedClients.Add(guid, worker);

            //Initialize new client in DB
            if (Statics.RedisDb.Miners.Any(x => x.Address == worker.Address))
            {
                Miner miner = Statics.RedisDb.Miners.First(x => x.Address == worker.Address);
                MinerWorker minerWorker = new MinerWorker(guid, miner.Identifier, 0);
                minerWorker.NewJobRequest();
                miner.MinersWorker.Add(guid);
                Statics.RedisDb.SaveChanges(miner);
                Statics.RedisDb.SaveChanges(minerWorker);
            }
            else
            {
                Miner miner = new Miner(worker.Address, 0);
                MinerWorker minerWorker = new MinerWorker(guid, miner.Identifier, 0);
                minerWorker.NewJobRequest();
                miner.MinersWorker.Add(guid);
                Statics.RedisDb.SaveChanges(miner);
                Statics.RedisDb.SaveChanges(minerWorker);
            }
            Logger.Log(Logger.LogLevel.Verbose, "Finished login response");

        }

        public async void AcceptTcpClient(TcpClient client)
        {
            while (true)
            {
                string sRequest = "";
                bool abort = false;
                while (client.GetStream().DataAvailable)
                {
                    byte[] b = new byte[1];
                    await client.GetStream().ReadAsync(b, 0, 1);
                    abort = true;
                    if (b[0] == 0x0a)
                    {
                        abort = false;
                        break;
                    }
                    sRequest += ASCIIEncoding.ASCII.GetString(b);
                    if (sRequest.Length > 1024*10 || (sRequest.Length - sRequest.Trim().Length) > 128)
                        break;
                }
                if (abort)
                {
                    client.Close();
                    break;
                }
                if(sRequest == "")
                    continue;
                string guid = "";
                string s = AcceptClient(sRequest, ((IPEndPoint) client.Client.RemoteEndPoint).Address.ToString(),
                                        ref abort, ref guid);
                if (abort)
                {
                    client.Close();
                    break;
                }
                s += "\n";
                byte[] byteArray = Encoding.UTF8.GetBytes(s);
                await client.GetStream().WriteAsync(byteArray, 0, byteArray.Length);

                if (Statics.ConnectedClients[guid].TcpClient == null)
                    Statics.ConnectedClients[guid].TcpClient = client;

            }
        }


        public async void AcceptHttpClient(
            Mono.Net.HttpListenerContext client)
        {

            string sRequest = Helpers.GetRequestBody(client.Request);
            //sRequest = Helpers.GetRequestBody(client.Request);
            if (sRequest == "")
                return;

            bool close = false;

            string guid = "";
            string s = AcceptClient(sRequest, client.Request.UserHostAddress, ref close, ref guid);
            byte[] byteArray = Encoding.UTF8.GetBytes(s);

            client.Response.ContentType = "application/json";

            client.Response.ContentLength64 = byteArray.Length;
            client.Response.OutputStream.Write(byteArray, 0, byteArray.Length);

            /* foreach (var b in client.connections)
             {
                                                                                                               
             } */
            //client.
            client.Response.OutputStream.Close();
          //  if (close)
                client.Response.Close();

            if(close)
                client.Request.InputStream.Dispose();
        }

        public string AcceptClient(string sRequest, string ipAddress, ref bool abort, ref string guid)
        {
            try
            {
              
                JObject request = JObject.Parse(sRequest);
                JObject response = new JObject();

                response["id"] = 0;
                response["jsonrpc"] = "2.0";

                response["error"] = null;
                if ((string) request["method"] == "login")
                {
                    guid = Guid.NewGuid().ToString();
                    
                }

                else
                {
                    guid = (string) request["params"]["id"];

                    if (!Statics.ConnectedClients.ContainsKey(guid))
                    {
                        response["error"] = "Not authenticated yet!";
                        return JsonConvert.SerializeObject(response);
                    }

                }
                switch ((string) request["method"])
                {
                    case "login":
                        GenerateLoginResponse(ref response, guid, (string) request["params"]["login"]);
                        break;
                    case "getjob":
                        GenerateGetJobResponse(ref response, guid);
                        break;
                    case "submit":
                        abort = GenerateSubmitResponse(ref response, (string)request["params"]["job_id"], guid,
                                                       Helpers.StringToByteArray((string) request["params"]["nonce"]),
                                                       (string) request["params"]["result"], ipAddress);
                        break;
                }

                return JsonConvert.SerializeObject(response);
            }
            
            catch (Exception e)
            {
                Logger.Log(Logger.LogLevel.Error, e.ToString());
                abort = true;
                return "";
            }
        }
    }
}
