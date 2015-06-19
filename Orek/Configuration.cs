using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Consul;
using Newtonsoft.Json;

namespace Orek
{
    class Configuration
    {
        public string Name { get; set; }
        public int TimeOut { get; set; }
        public int HeartBeatTtl { get; set; }
        public string KvPrefix { get; set; }
        public string SemaPrefix { get; set; }
        public string ConfPrefix { get; set; }
        public bool PushInitialConfig { get; set; }
        //public List<ManagedService> ManagedServices { get; set; }
        public List<ServiceDef> ManagedServices { get; set; }
    }

    public partial class OrekService
    {
        private const string ConfigFile = "OrekConfiguration.json";

        static Configuration ReadConfigurationFile()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name); 
            StreamReader streamReader = new StreamReader(ConfigFile);
            var result = JsonConvert.DeserializeObject<Configuration>(streamReader.ReadToEnd());
            streamReader.Close();
            return result;
        }

        private bool CheckOnlineConfig(string root)
        {
            QueryResult<string[]> keyQuery = _consulClient.KV.Keys(root);
            string[] keys = keyQuery.Response;
            bool check = keys != null &&
                keys.Contains(root + "/config/") &&
                keys.Contains(root + "/clusters/") &&
                keys.Contains(root + "/nodeassignment/") &&
                keys.Contains(root + "/semaphores/");
            return check;
        }

        private void CreateOnlineConfig(string root)
        {
            _consulClient.KV.Put(new KVPair(root + "/config/"));
            _consulClient.KV.Put(new KVPair(root + "/clusters/"));
            _consulClient.KV.Put(new KVPair(root + "/nodeassignment/"));
            _consulClient.KV.Put(new KVPair(root + "/semaphores/"));

            _consulClient.KV.CAS(new KVPair(root + "/config/stoptimeout") { Value = Encoding.UTF8.GetBytes("5000") });
            _consulClient.KV.CAS(new KVPair(root + "/config/heartbeatttl") { Value = Encoding.UTF8.GetBytes("5000") });

            _consulClient.KV.CAS(new KVPair(root + "/clusters/examplecluster/"));
            _consulClient.KV.CAS(new KVPair(root + "/clusters/examplecluster/name") { Value = Encoding.UTF8.GetBytes("examplecluster") });
            _consulClient.KV.CAS(new KVPair(root + "/clusters/examplecluster/windowsservice") { Value = Encoding.UTF8.GetBytes("someservice") });
            _consulClient.KV.CAS(new KVPair(root + "/clusters/examplecluster/starttimeout") { Value = Encoding.UTF8.GetBytes("5000") });
            _consulClient.KV.CAS(new KVPair(root + "/clusters/examplecluster/stoptimeout") { Value = Encoding.UTF8.GetBytes("5000") });
            _consulClient.KV.CAS(new KVPair(root + "/clusters/examplecluster/limit") { Value = Encoding.UTF8.GetBytes("1") });
            _consulClient.KV.CAS(new KVPair(root + "/clusters/examplecluster/heartbeatttl") { Value = Encoding.UTF8.GetBytes("5000") });
            _consulClient.KV.CAS(new KVPair(root + "/clusters/examplecluster/nodes") { Value = Encoding.UTF8.GetBytes("[\"examplenode\",\"anothernode\"]") });

            _consulClient.KV.CAS(new KVPair(root + "/nodeassignment/examplenode") { Value = Encoding.UTF8.GetBytes("[\"examplecluster\",\"anothercluster\"]") });
        }

        private OnlineConfig GetOnlineConfig(string root)
        {
            OnlineConfig myconf = new OnlineConfig();
            QueryResult<KVPair[]> keyQuery = _consulClient.KV.List(root);
            if (keyQuery.Response == null) return myconf;


            myconf.OrekSettings = new OnlineConfig.Orek
            {
                StopTimeout = Convert.ToInt32((from kv in keyQuery.Response
                                               where kv.Key == root + "/config/stoptimeout"
                                               select Encoding.UTF8.GetString(kv.Value)).FirstOrDefault()),
                HeartBeatTtl = Convert.ToInt32((from kv in keyQuery.Response
                                                where kv.Key == root + "/config/heartbeatttl"
                                                select Encoding.UTF8.GetString(kv.Value)).FirstOrDefault())
            };

            var clusters = from kv in keyQuery.Response
                           where Regex.Match(kv.Key, root + @"/clusters/([A-Za-z]+)/$", RegexOptions.IgnoreCase).Success
                           select Regex.Match(kv.Key, root + @"/clusters/([A-Za-z]+)/$", RegexOptions.IgnoreCase).Groups[1].Value;
            foreach (var cluster in clusters)
            {
                OnlineConfig.Cluster objCluster = new OnlineConfig.Cluster
                {
                    Name = cluster,
                    WindowsService = (from kv in keyQuery.Response
                                      where kv.Key == root + "/clusters/" + cluster + "/windowsservice"
                                      select Encoding.UTF8.GetString(kv.Value)).FirstOrDefault(),
                    StopTimeout = Convert.ToInt32((from kv in keyQuery.Response
                                                   where kv.Key == root + "/clusters/" + cluster + "/stoptimeout"
                                                   select Encoding.UTF8.GetString(kv.Value)).FirstOrDefault()),
                    StartTimeout = Convert.ToInt32((from kv in keyQuery.Response
                                                    where kv.Key == root + "/clusters/" + cluster + "/starttimeout"
                                                    select Encoding.UTF8.GetString(kv.Value)).FirstOrDefault()),
                    Limit = Convert.ToInt32((from kv in keyQuery.Response
                                             where kv.Key == root + "/clusters/" + cluster + "/limit"
                                             select Encoding.UTF8.GetString(kv.Value)).FirstOrDefault()),
                    HeartBeatTtl = Convert.ToInt32((from kv in keyQuery.Response
                                                    where kv.Key == root + "/clusters/" + cluster + "/heartbeatttl"
                                                    select Encoding.UTF8.GetString(kv.Value)).FirstOrDefault()),
                    Nodes = JsonConvert.DeserializeObject<List<string>>((from kv in keyQuery.Response
                                                                         where kv.Key == root + "/clusters/" + cluster + "/nodes"
                                                                         select Encoding.UTF8.GetString(kv.Value)).FirstOrDefault())
                };
                myconf.Clusters.Add(objCluster);
            }
            return myconf;
        }
    }

    class OnlineConfig
    {
        public Orek OrekSettings { get; set; }
        public List<Cluster> Clusters { get; set; }

        public OnlineConfig()
        {
            OrekSettings = new Orek();
            Clusters = new List<Cluster>();
        }
        internal class Orek
        {
            public int StopTimeout { get; set; }
            public int HeartBeatTtl { get; set; }

        }
        internal class Cluster
        {
            public int StopTimeout { get; set; }
            public int StartTimeout { get; set; }
            public int Limit { get; set; }
            public string Name { get; set; }
            public string WindowsService { get; set; }
            public int HeartBeatTtl { get; set; }
            public List<string> Nodes { get; set; }
        }
    }
}
