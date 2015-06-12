using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        //public List<ManagedService> ManagedServices { get; set; }
        public List<ServiceDef> ManagedServices { get; set; }
    }

    public partial class OrekService
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
