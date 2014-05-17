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
                Thread.Sleep(5000);
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

    }
}
