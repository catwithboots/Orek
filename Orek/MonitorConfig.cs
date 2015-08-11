using System;
using System.Collections.Generic;
using System.Linq;
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
            _monitorConfigThread = new Thread(MonitorConfig) { IsBackground = true };
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
            string clusterskey = Config.NodeAssignmentPrefix + ConsulClient.Agent.NodeName;
            while (!_shouldStop)
            {
                try
                {
                    MyLogger.Debug("Start monitoring KV {0} with index {1}", clusterskey, configIndex);
                    KVPair kv = MonitorKv(clusterskey, configIndex);
                    MyLogger.Debug("End monitoring KV {0} with index {1}", clusterskey, configIndex);
                    if ((kv != null) && (configIndex != kv.ModifyIndex))
                    {
                        configIndex = kv.ModifyIndex;
                        Config.ClusterAssignments =
                            JsonConvert.DeserializeObject<List<string>>(Encoding.UTF8.GetString(kv.Value, 0,
                                kv.Value.Length));
                        _configChanged =
                            !(AssignedClusters.OrderBy(s => s)
                                .SequenceEqual(Config.ClusterAssignments.OrderBy(s => s)));
                        MyLogger.Info("Assigned cluster configuration has changed (index={0}): {1}", configIndex,
                            _configChanged);
                    }
                    else if (kv == null)
                    {
                        Config.ClusterAssignments = new List<string>();
                        _configChanged =
                            !(AssignedClusters.OrderBy(s => s)
                                .SequenceEqual(Config.ClusterAssignments.OrderBy(s => s)));
                        ;
                        configIndex = 1;
                        MyLogger.Info("Assigned cluster configuration has changed (index={0})", configIndex);
                    }
                    Thread.Sleep(5000);
                }
                catch (ThreadAbortException)
                {
                    MyLogger.Debug("MonitorConfig Thread is aborting");
                }
                catch (ApplicationException ex)
                {
                    if (ex.InnerException.Message == "The operation has timed out")
                    {
                        MyLogger.Debug("MonitorKV timed out");
                    }
                }
                catch (Exception ex)
                {
                    MyLogger.Error("Some Error: {0}", ex.Message);
                    MyLogger.Debug(ex);
                }
            }
        }
    }
}
