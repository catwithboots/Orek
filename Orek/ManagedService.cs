using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orek
{
    class ManagedService
    {
        public string WindowsServiceName { get; set; }
        public string ConsulServiceName { get; set; }
        public int Timeout { get; set; }
        public int Limit { get; set; }
    }
}
