using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using Consul;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Orek
{
    partial class OrekService
    {
        /// <summary>
        /// Starts the monitorConfig thread.
        /// </summary>
        private void StartMonitorConfig()
        {
            _monitorConfigThread = new Thread(MonitorConfig) {IsBackground = true};
            _monitorConfigThread.Start();
        }

        /// <summary>
        /// Waits for the monitorConfig thread to stop gracefully within the timeout milliseconds
        /// Otherwise aborts the thread
        /// </summary>
        private void StopMonitorConfig()
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
            string managedServicesKey = string.Format("{0}{1}/Nodes/{2}", _config.KvPrefix, _config.Name, _consulClient.Agent.NodeName);

            while (!_shouldStop)
            {
                try
                {
                    MyLogger.Debug("Start monitoring KV {0} with index {1}",managedServicesKey,configIndex);
                    KVPair kv = MonitorKv(managedServicesKey, configIndex);
                    MyLogger.Debug("End monitoring KV {0} with index {1}", managedServicesKey, configIndex);
                    
                    // TODO GET list of managedservices and managedservice config separately!!!

                    if ((kv != null)&&(configIndex!=kv.ModifyIndex))
                    {
                        configIndex = kv.ModifyIndex;
                        dynamic responseobject =
                            JsonConvert.DeserializeObject(Encoding.UTF8.GetString(kv.Value, 0, kv.Value.Length));
                        JArray myarray = responseobject.ManagedServices;
                        _newManagedServices = myarray.ToObject<List<ServiceDef>>();
                        _configChanged = true;
                        MyLogger.Info("ManagedService configuration has changed (index={0})", configIndex);
                    }
                    else if (kv == null)
                    {
                        _newManagedServices = new List<ServiceDef>();
                        if (configIndex != 1)
                        {
                            _configChanged = true;
                            configIndex = 1;
                            MyLogger.Info("ManagedService configuration has changed (index={0})", configIndex);
                        }
                    }
                    Thread.Sleep(5000);
                }
                catch (ThreadAbortException)
                {
                    MyLogger.Trace("Thread is aborting");
                }
                catch (Exception ex)
                {
                    MyLogger.Trace("Some Error: {0}", ex.Message);
                    MyLogger.Trace(ex);
                    //if ((ex.InnerException != null) &&
                    //    (ex.InnerException.Message == "Unable to connect to the remote server"))
                    //{
                        MyLogger.Error("Error communicating with consul, stopping");
                        _shouldStop = true;
                    //}
                }

            }
        }
    }
}
