using System;
using System.Collections.Generic;
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
        private Client _consulClient;

        private bool CreateConsulClient()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            string agentname;
            try
            {
                _consulClient = new Client();
                agentname = _consulClient.Agent.NodeName;
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
                _consulClient.Agent.ServiceRegister(svcreg);
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
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            try
            {
                _consulClient.Agent.ServiceDeregister(name);
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

        internal bool RegisterServiceRunningCheck(string name,int ttl)
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
                _consulClient.Agent.CheckRegister(cr);
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
                _consulClient.Agent.CheckRegister(cr);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.Error("Heartbeat Check registration failed: {0}", ex.Message);
                MyLogger.Debug(ex);
                return false;
            }
        }

        internal void RegisterSemaphore(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            if (svc.Semaphore != null) try { svc.Semaphore.Destroy(); }
                catch (SemaphoreInUseException) { }
            var _semaphoreOptions = new SemaphoreOptions(_config.KVPrefix + svc.ConsulServiceName + _config.SemaPrefix, svc.Limit) { SessionName = svc.ConsulServiceName + "_Session", SessionTTL = TimeSpan.FromSeconds(10) };
            svc.Semaphore = _consulClient.Semaphore(_semaphoreOptions);
        }

        internal void CleanUpSemaphore(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            MyLogger.Debug("Clean up Semaphore for {0}",svc.ConsulServiceName);
            if (svc.Semaphore != null)
                try
                {
                    if (svc.Semaphore.IsHeld)
                    {
                        MyLogger.Debug("Semaphore held, releasing");
                        svc.Semaphore.Release();
                    }
                    Thread.Sleep(1000);
                    MyLogger.Debug("Trying to destroy the semaphore");
                    svc.Semaphore.Destroy();
                    MyLogger.Debug("Trying to delete the semaphore tree");
                    _consulClient.KV.DeleteTree(_config.KVPrefix + svc.ConsulServiceName + _config.SemaPrefix);
                }
                catch (SemaphoreInUseException)
                {
                    MyLogger.Debug("Semaphore was still in use");
                }
        }

        private KVPair MonitorKV(string path, ulong index)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            QueryOptions myQueryOptions = new QueryOptions() { WaitIndex = index };
            var qr = _consulClient.KV.Get(path, myQueryOptions);
            if (qr != null) return qr.Response;
            return null;
        }
    }
}
