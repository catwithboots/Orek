using System.Collections;
using System.Collections.Generic;

namespace Orek
{
    class ServiceDef
    {
        //Config Values
        public string WindowsServiceName { get; set; }
        public string ConsulServiceName { get; set; }
        public int Timeout { get; set; }
        public int HeartBeatTtl { get; set; }
        public int Limit { get; set; }

        
    }   
    class ManagedService:ServiceDef
    {
        //Running Values
        public bool CanRun { get; set; }
        public bool ShouldRun { get; set; }
        public Consul.Semaphore Semaphore { get; set; }
        public System.Threading.Thread MonitorThread { get; set; }
        public System.Threading.Thread GetLockThread { get; set; }
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
