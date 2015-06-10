using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using Consul;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Orek
{
    class Configuration
    {
        public string Name { get; set; }
        public int TimeOut { get; set; }
        public int HeartBeatTTL { get; set; }
        public string KVPrefix { get; set; }
        public string SemaPrefix { get; set; }
        public string ConfPrefix { get; set; }
        public List<ManagedService> ManagedServices { get; set; }
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

        /// <summary>
        /// Starts the monitorConfig thread.
        /// </summary>
        private void StartMonitorConfig()
        {
            _monitorConfigThread = new Thread(MonitorConfig);
            _monitorConfigThread.IsBackground = true;
            _monitorConfigThread.Start();
        }

        /// <summary>
        /// Waits for the monitorConfig thread to stop gracefully within the timeout milliseconds
        /// Otherwise aborts the thread
        /// </summary>
        /// <param name="timeout">The timeout in milliseconds.</param>
        private void StopMonitorConfig(int timeout)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            _monitorConfigThread.Abort();
            //_monitorConfigThread.Join(TimeSpan.FromMilliseconds(timeout));   
        }

        /// <summary>
        /// Monitors the configuration KV.
        /// This method will be called when the monitorConfig thread is started.
        /// </summary>
        private void MonitorConfig()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            ulong configIndex = 1;
            string managedServicesKey = string.Format("{0}{1}/Nodes/{2}", _config.KVPrefix, _config.Name, _consulClient.Agent.NodeName);            

            while (!_shouldStop)
            {
                try
                {
                    KVPair kv = MonitorKV(managedServicesKey, configIndex);


                    // TODO GET list of managedservices and managedservice config separately!!!

                    if (kv != null)
                    {
                        configIndex = kv.ModifyIndex;
                        dynamic responseobject =
                            JsonConvert.DeserializeObject(Encoding.UTF8.GetString(kv.Value, 0, kv.Value.Length));
                        JArray myarray = responseobject.ManagedServices;
                        _newManagedServices = myarray.ToObject<List<ManagedService>>();
                        _configChanged = true;
                        MyLogger.Info("ManagedService configuration has changed (index={0})", configIndex);
                    }
                    else
                    {
                        _newManagedServices = new List<ManagedService>();
                        if (configIndex != 1)
                        {
                            _configChanged = true;
                            configIndex = 1;
                            MyLogger.Info("ManagedService configuration has changed (index={0})",configIndex);
                        }
                        else Thread.Sleep(5000);
                    }
                }
                catch (ThreadAbortException)
                {
                    MyLogger.Trace("Thread is aborting");
                }
                catch (Exception ex)
                {
                    MyLogger.Error("Some Error: {0}",ex.Message);
                    MyLogger.Debug(ex);
                }
                
            }
        }
  

    }
}
