using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Consul;
using Newtonsoft.Json;
using NLog;

namespace Orek
{
    public class Configuration
    {
        private readonly OrekService _parent;
        private static readonly Logger MyLogger = Program.MyLogger;
        public string Name { get; set; }
        public int TimeOut { get; set; }
        public int HeartBeatTtl { get; set; }
        public string KvPrefix { get; set; }
        public string SemaPrefix { get; set; }
        public string ConfPrefix { get; set; }
        public string ConfigPrefix { get; set; }
        public string ClustersPrefix { get; set; }
        public string NodeAssignmentPrefix { get; set; }
        public List<string> ClusterAssignments { get; set; }
        public List<ServiceDef> ManagedServices { get; set; }
        public bool ValidOnlineConfig { get; set; }

        public Configuration(OrekService parent)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name); 
            _parent = parent;
            Name = "OREK";
            KvPrefix = "Services/" + Name + "/";
            ConfigPrefix = KvPrefix + "config/";
            ClustersPrefix = KvPrefix + "clusters/";
            NodeAssignmentPrefix = KvPrefix + "nodeassignment/";
            SemaPrefix = "semaphores/";
            HeartBeatTtl = 5000;
            TimeOut = 5000;
            ClusterAssignments=new List<string>();
            ValidOnlineConfig=GetOnlineConfiguration();
        }
        public bool GetOnlineConfiguration()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            bool hbresult = false;
            bool toresult = false;
            var cfg = _parent.ConsulClient.KV.List(ConfigPrefix);
            if (cfg.Response == null) return false;
            KVPair hbKvPair=cfg.Response.FirstOrDefault(kv => kv.Key == ConfigPrefix+"heartbeatttl");
            if (hbKvPair != null)
                try
                {
                    HeartBeatTtl = Convert.ToInt32(Encoding.UTF8.GetString(hbKvPair.Value, 0, hbKvPair.Value.Length));
                    MyLogger.Debug("HeartbeatTTL set to {0} from online config", HeartBeatTtl);
                    hbresult = true;
                }
                catch (Exception ex)
                {
                    MyLogger.Error("Error converting value {0} to int: {1}", ConfigPrefix + "heartbeatttl",ex.Message);
                    MyLogger.Debug(ex);
                }
            KVPair toKvPair = cfg.Response.FirstOrDefault(kv => kv.Key == ConfigPrefix + "timeout");
            if (toKvPair != null)
            try
            {
                TimeOut = Convert.ToInt32(Encoding.UTF8.GetString(toKvPair.Value, 0, toKvPair.Value.Length));
                MyLogger.Debug("TimeOut set to {0} from online config", TimeOut);
                toresult = true;
            }
            catch (Exception ex)
            {
                MyLogger.Error("Error converting value {0} to int: {1}", ConfigPrefix + "timeout", ex.Message);
                MyLogger.Debug(ex);
            }
            return hbresult&&toresult;
        }

        private List<string> GetClustersForNode()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            QueryOptions myQueryOptions = new QueryOptions();
            var qr = _parent.ConsulClient.KV.Get(NodeAssignmentPrefix + _parent.ConsulClient.Agent.NodeName, myQueryOptions);
            if (qr != null)
            {
                try
                {
                    var jsonstring = Encoding.UTF8.GetString(qr.Response.Value, 0, qr.Response.Value.Length);
                    var clusters =
                        JsonConvert.DeserializeObject<List<string>>(Encoding.UTF8.GetString(qr.Response.Value, 0,
                            qr.Response.Value.Length));
                    return clusters;
                }
                catch
                {
                    return new List<string>();
                }
            }
            return new List<string>();
        } 
    }

    public partial class OrekService
    {
       
        
    }
}
