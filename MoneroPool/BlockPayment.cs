using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MoneroPool
{
    public class BlockPayment
    {
        public BlockPayment()
        {
        }


         private static async void InitiatePayments(ulong reward, PoolBlock block)
         {

             Dictionary<string, double> sharePerAddress = new Dictionary<string, double>();
             int lastPaidBlock = 0;

             long totalShares = 0;

             try
             {
                 lastPaidBlock = Statics.RedisDb.Information.LastPaidBlock;
             }
             catch
             {
                 lastPaidBlock = 0;
             }

             

             foreach (var miner in Statics.RedisDb.Miners)
             {
                 sharePerAddress.Add(miner.Address, 0);
                 double shares = 0;

                 Block[] blocks =
                     Statics.RedisDb.Blocks.Where(
                         x => x.BlockHeight > lastPaidBlock && x.BlockHeight <= block.BlockHeight).ToArray();

                 foreach (
                     var blockReward in
                         Statics.RedisDb.BlockRewards.Where(
                             x =>
                             x.Miner == miner.Identifier &&
                             blocks.Any(x2 => x2.Identifier == x.Block)))
                 {
                     foreach (var share in Statics.RedisDb.Shares.Where(x => blockReward.Shares.Contains(x.Identifier)))
                     {
                         shares += share.Value;
                     }

                 }

                 sharePerAddress[miner.Address] = shares;

             }

             int fee = 100 + int.Parse(Statics.Config.IniReadValue("pool-fee"));

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

             JObject transfer = Statics.WalletJson.InvokeMethod("transfer", param);

             Statics.RedisDb.Information.LastPaidBlock = block.BlockHeight;
             Statics.RedisDb.SaveChanges(Statics.RedisDb.Information);

             Console.WriteLine(transfer);
         }

        public async void Start()
        {
            await Task.Yield();
            Logger.Log(Logger.LogLevel.General, "Beginning Block Payment thread!");

            while (true)
            {
                Thread.Sleep(5000);
                for (int i = 0; i < Statics.BlocksPendingPayment.Count; i++)
                {
                    PoolBlock pBlock = Statics.BlocksPendingPayment[i];
                    string hash = pBlock.BlockHash;
                    JObject param = new JObject();
                    param["hash"] = hash;
                    JObject block = (JObject) (await Statics.DaemonJson.InvokeMethodAsync("getblockheaderbyhash", new JObject()))["result"]["block_header"];
                    int confirms = (int) block["depth"];
                    if (!(bool) block["orphan_status"] &&
                        confirms >= int.Parse(Statics.Config.IniReadValue("block-confirms")))
                    {
                        //Do payments
                        InitiatePayments((ulong)block["reward"], pBlock);
                        Statics.BlocksPendingPayment.RemoveAt(i);
                        i--;
                    }
                    if ((bool) block["orphan_status"])
                    {
                        //Orphaned
                        Statics.BlocksPendingPayment.RemoveAt(i);
                        i--;
                    }
                }

            }
        }
    }
}
