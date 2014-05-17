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

        public async void Start()
        {
            await Task.Yield();

            while (true)
            {
                Thread.Sleep(1000);
                for (int i = 0; i < Statics.BlocksPendingPayment.Count; i++)
                {
                    string hash = Statics.BlocksPendingPayment[i].BlockHash;
                    JObject block = (JObject) (await Statics.DaemonJson.InvokeMethodAsync("getblockheaderbyhash"))["result"]["block_header"];
                    int confirms = (int) block["depth"];
                    if (!(bool) block["orphan_status"] &&
                        confirms >= int.Parse(Statics.Config.IniReadValue("block-confirms")))
                    {
                        //Do payments
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
