using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Policy;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using NLog;

namespace Orek
{
    public partial class Service : ServiceBase
    {
        private Client _consulClient;
        private bool _consulEnabled;

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

        private bool RegisterSvcInConsul(string name)
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

        private bool DeRegisterSvcInConsul(string name)
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

        private void OrekHeartbeat()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            while (true)
            {
                try
                {
                    _consulClient.Agent.PassTTL(_config.Name + "_Running", "is running");
                }
                catch (Exception ex)
                {
                    MyLogger.Error("Error sending heartbeat: {0}", ex.Message);
                    MyLogger.Debug(ex);
                }
                Thread.Sleep(2500);
            }
        }

        internal bool RegisterServiceRunningCheck(string name)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            AgentCheckRegistration cr = new AgentCheckRegistration
            {
                ID = name + "_Running",
                //Name = name + "_Running",
                Name = "Run Status",
                TTL = TimeSpan.FromSeconds(5),
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
            var _semaphoreOptions = new SemaphoreOptions(svc.ConsulServiceName + "/Semaphore", svc.Limit) { SessionName = svc.ConsulServiceName + "_Session", SessionTTL = TimeSpan.FromSeconds(10) };
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
                    _consulClient.KV.DeleteTree(svc.ConsulServiceName + "/Semaphore");
                }
                catch (SemaphoreInUseException)
                {
                    MyLogger.Debug("Semaphore was still in use");
                }
        }
    }
}
