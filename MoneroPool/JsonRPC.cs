using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoneroPool
{
    public class JsonRPC
    {
        public string Url { get; private set; }
        public JsonRPC(string URL)
        {
            Url = URL;
        }

        public int GetBlockCount()
        {
            return (int) InvokeMethod("getblockcount")["result"]["count"];
        }

        public JObject InvokeMethod(string a_sMethod, params object[] a_params)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Url +"/"+ "json_rpc");
            //webRequest.Credentials = Credentials;

            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";

            JObject joe = new JObject();
            joe["jsonrpc"] = "2.0";
            joe["id"] = "test";
            joe["method"] = a_sMethod;

            if (a_params != null)
            {
                if (a_params.Length > 0)
                {
                   /* JArray props = new JArray();
                    foreach (var p in a_params)
                    {
                        props.Add(p);
                    }   */
                    //temp fix
                    joe.Add(new JProperty("params", a_params[0]));
                }
            }

            string s = JsonConvert.SerializeObject(joe);
            //s=s.Substring(0, s.Length - 1);
            // serialize json for the request
            byte[] byteArray = Encoding.UTF8.GetBytes(s);
            webRequest.ContentLength = byteArray.Length;

            using (Stream dataStream = webRequest.GetRequestStream())
            {
                
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (Stream str = webResponse.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(str))
                    {
                        return JsonConvert.DeserializeObject<JObject>(sr.ReadToEnd());
                    }
                }
            }

        }

        public async Task<JObject> InvokeMethodAsync(string a_sMethod, params object[] a_params)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Url + "/" + "json_rpc");
            //webRequest.Credentials = Credentials;

            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";

            JObject joe = new JObject();
            joe["jsonrpc"] = "2.0";
            joe["id"] = "test";
            joe["method"] = a_sMethod;

            if (a_params != null)
            {
                if (a_params.Length > 0)
                {
                    /* JArray props = new JArray();
                     foreach (var p in a_params)
                     {
                         props.Add(p);
                     }   */
                    //temp fix
                    joe.Add(new JProperty("params", a_params[0]));
                }
            }

            string s = JsonConvert.SerializeObject(joe);
            //s=s.Substring(0, s.Length - 1);
            // serialize json for the request
            byte[] byteArray = Encoding.UTF8.GetBytes(s);
            webRequest.ContentLength = byteArray.Length;

            using (Stream dataStream = webRequest.GetRequestStream())
            {

                await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
            }

            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (Stream str = webResponse.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(str))
                    {
                        return JsonConvert.DeserializeObject<JObject>(await sr.ReadToEndAsync());
                    }
                }
            }

        }
    }
}
