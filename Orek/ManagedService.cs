using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NLog;

namespace Orek
{
    public class ServiceDef
    {
        //Config Values
        public string WindowsServiceName { get; set; }
        public string ConsulServiceName { get; set; }
        public int Timeout { get; set; }
        public int StartTimeout { get; set; }
        public int StopTimeout { get; set; }
        public int HeartBeatTtl { get; set; }
        public int Limit { get; set; }
        private static readonly Logger MyLogger = Program.MyLogger;

        public static ServiceDef GetServiceDefinition(OrekService parent,string cluster)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            ServiceDef result=new ServiceDef();
            var qr = parent.ConsulClient.KV.List(parent.Config.ClustersPrefix + cluster);
            if ((qr != null) && (qr.Response != null))
            {
                var clusterconfig = qr.Response;
                try
                {
                    var kvPair=clusterconfig.First(kv => kv.Key == parent.Config.ClustersPrefix + cluster + "/name");
                    result.ConsulServiceName = Encoding.UTF8.GetString(kvPair.Value, 0, kvPair.Value.Length);
                    kvPair = clusterconfig.First(kv => kv.Key == parent.Config.ClustersPrefix + cluster + "/windowsservice");
                    result.WindowsServiceName = Encoding.UTF8.GetString(kvPair.Value, 0, kvPair.Value.Length);
                    kvPair = clusterconfig.First(kv => kv.Key == parent.Config.ClustersPrefix + cluster + "/heartbeatttl");
                    result.HeartBeatTtl = Convert.ToInt32(Encoding.UTF8.GetString(kvPair.Value, 0, kvPair.Value.Length));
                    kvPair = clusterconfig.First(kv => kv.Key == parent.Config.ClustersPrefix + cluster + "/starttimeout");
                    result.StartTimeout = Convert.ToInt32(Encoding.UTF8.GetString(kvPair.Value, 0, kvPair.Value.Length));
                    kvPair = clusterconfig.First(kv => kv.Key == parent.Config.ClustersPrefix + cluster + "/stoptimeout");
                    result.StopTimeout = Convert.ToInt32(Encoding.UTF8.GetString(kvPair.Value, 0, kvPair.Value.Length));
                    kvPair = clusterconfig.First(kv => kv.Key == parent.Config.ClustersPrefix + cluster + "/limit");
                    result.Limit = Convert.ToInt32(Encoding.UTF8.GetString(kvPair.Value, 0, kvPair.Value.Length));
                }
                catch (Exception ex)
                {
                    MyLogger.Error("Error parsing service definition for cluster {0}: {1}", cluster, ex.Message);
                    MyLogger.Debug(ex);
                }
            }
            return result;
        }

        public ManagedService ToManagedService()
        {
            return new ManagedService
            {
                WindowsServiceName = WindowsServiceName,
                ConsulServiceName = ConsulServiceName,
                StartTimeout = StartTimeout,
                StopTimeout = StopTimeout,
                Timeout = Timeout,
                Limit = Limit,
                HeartBeatTtl = HeartBeatTtl,
                ShouldRun = true
            };
        }
    }   
    public class ManagedService:ServiceDef
    {
        //Running Values
        public bool CanRun { get; set; }
        public bool ShouldRun { get; set; }
        public Consul.Semaphore Semaphore { get; set; }
        public System.Threading.Thread MonitorThread { get; set; }
        public System.Threading.Thread RunThread { get; set; }
    }

    class ServiceComparer : IEqualityComparer<ServiceDef>
    {
        public bool Equals(ServiceDef x, ServiceDef y)
        {
            return x.WindowsServiceName == y.WindowsServiceName && x.ConsulServiceName == y.ConsulServiceName &&
                   x.Timeout == y.Timeout && x.HeartBeatTtl == y.HeartBeatTtl && x.Limit == y.Limit;
        }

        public int GetHashCode(ServiceDef obj)
        {
            return obj.WindowsServiceName.GetHashCode() ^ obj.ConsulServiceName.GetHashCode() ^
                   obj.Timeout.GetHashCode() ^ obj.HeartBeatTtl.GetHashCode() ^ obj.Limit.GetHashCode();
        }
    }
}
