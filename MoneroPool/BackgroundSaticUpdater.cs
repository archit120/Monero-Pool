using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MoneroPool
{
    public class BackgroundSaticUpdater
    {
        public BackgroundSaticUpdater()
        {
            
        }

        public async void Start()
        {
            await Task.Yield();
            Statics.CurrentBlockTemplate = (JObject)
                                           (await
                                            Statics.DaemonJson.InvokeMethodAsync("getblocktemplate",
                                                                                 new JObject(
                                                                                     new JProperty(
                                                                                         "reserve_size", 4),
                                                                                     new JProperty(
                                                                                         "wallet_address",
                                                                                         Statics.Config
                                                                                                .IniReadValue(
                                                                                                    "wallet-address")))))
                                               ["result"];
            while (true)
            {
                int newBlockHeight =
                    (int) (await Statics.DaemonJson.InvokeMethodAsync("getblockcount"))["result"]["count"];
                if (newBlockHeight != Statics.CurrentBlockHeight)
                {
                    Statics.CurrentBlockTemplate = (JObject)
                                                   (await
                                                    Statics.DaemonJson.InvokeMethodAsync("getblocktemplate",
                                                                                         new JObject(
                                                                                             new JProperty(
                                                                                                 "reserve_size", 4),
                                                                                             new JProperty(
                                                                                                 "wallet_address",
                                                                                                 Statics.Config
                                                                                                        .IniReadValue(
                                                                                                            "wallet-address")))))
                                                       ["result"];

                    Statics.CurrentBlockHeight = newBlockHeight;
                }
                System.Threading.Thread.Sleep(5000);
            }
        }
    }
}
