using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orek
{
    class ManagedService
    {
        //Config Values
        public string WindowsServiceName { get; set; }
        public string ConsulServiceName { get; set; }
        public int Timeout { get; set; }
        public int Limit { get; set; }

        //Running Values
        public bool CanRun { get; set; }
        public bool ShouldRun { get; set; }
        public Consul.Semaphore Semaphore { get; set; }
        public Thread MonitorThread { get; set; }
        public Thread GetLockThread { get; set; }  
        public Thread RunThread { get; set; }  
    }    
}
