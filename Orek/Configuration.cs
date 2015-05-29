using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Orek
{
    class Configuration
    {
        public string Name { get; set; }
        public int TimeOut { get; set; }
        public List<ManagedService> ManagedServices { get; set; }
    }

    public partial class Service : ServiceBase
    {
        private const string ConfigFile = "OrekConfiguration.json";

        static Configuration ReadConfiguration()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name); 
            StreamReader streamReader = new StreamReader(ConfigFile);
            var result = JsonConvert.DeserializeObject<Configuration>(streamReader.ReadToEnd());
            streamReader.Close();
            return result;
        }
    }
}
