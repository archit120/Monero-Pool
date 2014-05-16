using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace MoneroPool
{
    public class CryptoNightPool
    {
       

        private static Dictionary<string, worker> ConnectedClients = new Dictionary<string, worker>();
        private static ConnectionMultiplexer redis;

        private static RedisPoolDatabase redisDb;
        private static IniFile config = new IniFile("config.txt");

        public CryptoNightPool()
        {

            redis = ConnectionMultiplexer.Connect(config.IniReadValue("redis-server"));

            redisDb = new RedisPoolDatabase(redis.GetDatabase(int.Parse(config.IniReadValue("redis-database"))));
        }

       public async void BeginListening()
       {
           Logger.Log(Logger.LogLevel.General, "Beginning Listen!");

           HttpListener listener = new HttpListener();
           listener.Prefixes.Add(config.IniReadValue("http-server"));
           listener.Start();
           while (true)
           {
               HttpListenerContext client = await listener.GetContextAsync();
               AcceptClient(client);
           }   
       }

        public void GenerateLoginResponse(ref JObject response, string guid, string address)
        {
            JObject result = new JObject();
            JObject job = new JObject();

            worker worker = new worker();
            worker.address = address;

            Logger.Log(Logger.LogLevel.General, "Adding {0} to connected clients", guid);

            result["id"] = guid;

            job["blob"] = Helpers.GenerateUniqueWork();
            job["job_id"] = Guid.NewGuid().ToString();
            job["target"] = config.IniReadValue("miner-target-hex");

            result["job"] = job;

            response["result"] = result; 
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


                }

            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogLevel.Error, e.ToString());
            }
        }
    }
}
