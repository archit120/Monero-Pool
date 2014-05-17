using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MoneroPool
{
    public class BlockSubmitter
    {
        public BlockSubmitter(){}

        public async void Start()
        {
            await Task.Yield();
            while (true)
            {
                Thread.Sleep(1000);
                for (int i = 0; i < Statics.BlocksPendingSubmition.Count; i++)
                {
                    PoolBlock block = Statics.BlocksPendingSubmition[i];
                    if ((string) (await Statics.DaemonJson.InvokeMethodAsync("submitblock", block.BlockData))["result"]["status"] == "OK")
                    {
                        Statics.BlocksPendingPayment.Add(block);
                    }
                    else
                    {
                        Console.WriteLine("Block submittance failed!");
                    }
                    Statics.BlocksPendingSubmition.RemoveAt(i);
                    i--;
                }
            }
        }

       /* private static async void InitiatePayments()
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

            long reward = (long)blockHeader["result"]["blockheader"]["reward"];

            int fee = 100 + int.Parse(config.IniReadValue("pool-fee"));

            double rewardPerShare = (double)reward / ((double)(fee * totalShares) / 100);

            JObject param = new JObject();

            JArray destinations = new JArray();

            foreach (KeyValuePair<string, double> addressShare in sharePerAddress)
            {
                JObject destination = new JObject();
                destination["amount"] = (long)(addressShare.Value * rewardPerShare);
                destination["address"] = addressShare.Key;

                destinations.Add(destination);
            }

            param["destinations"] = destinations;

            param["fee"] = 0;
            param["mixin"] = 0;
            param["unlock_time"] = 0;

            JObject transfer = Walletjson.InvokeMethod("transfer", param);

            Console.WriteLine(transfer);
        }   */
    }
}
