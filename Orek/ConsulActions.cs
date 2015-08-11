using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Consul;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Orek
{
    public partial class OrekService
    {
        internal Client ConsulClient;

        private bool CreateConsulClient()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            string agentname;
            try
            {
                ConsulClient = new Client();
                agentname = ConsulClient.Agent.NodeName;
            }
            catch (Exception ex)
            {
                MyLogger.Error("Could not create Consul Client: {0}", ex.Message);
                MyLogger.Debug(ex);
                return false;
            }
            MyLogger.Debug("ConsulClient created with agent {0}", agentname);
            return true;
        }

        private bool RegisterService(string name)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            try
            {
                AgentServiceRegistration svcreg = new AgentServiceRegistration
                {
                    Name = name
                };
                ConsulClient.Agent.ServiceRegister(svcreg);
                MyLogger.Debug("{0} registered in Consul Services", name);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.Error("Error in registring {0} in Consul Services: {1}", name, ex.Message);
                MyLogger.Debug(ex);
                return false;
            }
        }

        private bool DeRegisterService(string name)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, name);
            try
            {
                ConsulClient.Agent.ServiceDeregister(name);
                MyLogger.Debug("{0} deregistered from Consul Services", name);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.Error("Error in deregistring {0} from Consul Services: {1}", name, ex.Message);
                MyLogger.Debug(ex);
                return false;
            }
        }

        internal bool RegisterServiceRunningCheck(string name, int ttl)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            AgentCheckRegistration cr = new AgentCheckRegistration
            {
                ID = name + "_Running",
                //Name = name + "_Running",
                Name = "Run Status",
                TTL = TimeSpan.FromMilliseconds(ttl),
                Notes = "Status of service " + name,
                ServiceID = name
            };
            try
            {
                ConsulClient.Agent.CheckRegister(cr);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.Error("Heartbeat Check registration failed: {0}", ex.Message);
                MyLogger.Debug(ex);
                return false;
            }
        }

        internal bool RegisterServiceReadyCheck(string name)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            AgentCheckRegistration cr = new AgentCheckRegistration
            {
                ID = name + "_Ready",
                Name = "Ready Status",
                TTL = TimeSpan.FromSeconds(5),
                Notes = "Is service " + name + " ready to run",
                ServiceID = name
            };
            try
            {
                ConsulClient.Agent.CheckRegister(cr);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.Error("Heartbeat Check registration failed: {0}", ex.Message);
                MyLogger.Debug(ex);
                return false;
            }
        }


        internal void CleanUpSemaphore(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            MyLogger.Debug("Clean up Semaphore for {0}", svc.ConsulServiceName);

            var qr = ConsulClient.KV.List(Config.KvPrefix + Config.SemaPrefix + svc.ConsulServiceName);
            if (qr.Response != null)
            {
                KVPair[] sessions = qr.Response.Where(kv=>!kv.Key.EndsWith("/") && !kv.Key.EndsWith(".lock")).ToArray();
                foreach (var kv in sessions)
                {
                    var wr = ConsulClient.KV.Delete(kv.Key);
                }
            }
        }

        private KVPair MonitorKv(string path, ulong index)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            QueryOptions myQueryOptions = new QueryOptions() { WaitIndex = index };
            var qr = ConsulClient.KV.Get(path, myQueryOptions);
            if (qr != null) return qr.Response;
            return null;
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Sends the passTTL to consul, catching possible exceptions.
        /// </summary>
        /// <param name="checkid">The checkid.</param>
        /// <param name="note">The note.</param>
        /// <returns>bool indicating success</returns>
        private bool SendPassTTL(string checkid, string note)
        {
            try
            {
                ConsulClient.Agent.PassTTL(checkid, note);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.Error("Error sending PassTTL to consul check {0}: {1}", checkid, ex.Message);
                MyLogger.Debug(ex);
                return false;
            }
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Sends the failTTL to consul, catching possible exceptions.
        /// </summary>
        /// <param name="checkid">The checkid.</param>
        /// <param name="note">The note.</param>
        /// <returns>bool indicating success</returns>
        private bool SendFailTTL(string checkid, string note)
        {
            try
            {
                ConsulClient.Agent.FailTTL(checkid, note);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.Error("Error sending FailTTL to consul check {0}: {1}", checkid, ex.Message);
                MyLogger.Debug(ex);
                return false;
            }
        }
    }
}
