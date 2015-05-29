using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
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
        private void Action()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            foreach (ManagedService managedService in _config.ManagedServices)
            {
                DefineService(managedService);
            }
            while (_doAction)
            {
                foreach (ManagedService managedService in _config.ManagedServices)
                {
                    CheckService(managedService);
                }
                Thread.Sleep(1000);
            }
            foreach (ManagedService managedService in _config.ManagedServices) CleanupService(managedService);            
        }

        private void StopAction(TimeSpan timeOut)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            Stopwatch sw=new Stopwatch();
            _doAction = false;
            sw.Start();
            while ((sw.ElapsedMilliseconds < timeOut.TotalMilliseconds) && (_actionThread.IsAlive))
            {
                Thread.Sleep(100);
            }
            _actionThread.Abort();
        }

        private void DefineService(ManagedService managedService)
        {
            RegisterSvcInConsul(managedService.ConsulServiceName);
            RegisterServiceCheck(managedService.ConsulServiceName);  
        }

        private void CleanupService(ManagedService managedService)
        {
            DeRegisterSvcInConsul(managedService.ConsulServiceName);
        }

        private static string GetServiceStatus(string serviceName)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            ServiceController sc = new ServiceController(serviceName);

            switch (sc.Status)
            {
                case ServiceControllerStatus.Running:
                    return "Running";
                case ServiceControllerStatus.Stopped:
                    return "Stopped";
                case ServiceControllerStatus.Paused:
                    return "Paused";
                case ServiceControllerStatus.StopPending:
                    return "Stopping";
                case ServiceControllerStatus.StartPending:
                    return "Starting";
                default:
                    return "Status Changing";
            }
        }

        private void CheckService(ManagedService managedService)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            string stat = GetServiceStatus(managedService.WindowsServiceName);
            if (stat == "Running")
            {
                _consulClient.Agent.PassTTL(managedService.ConsulServiceName + "_Running", stat);
            }
            else
            {
                _consulClient.Agent.FailTTL(managedService.ConsulServiceName + "_Running", stat);
            }
        }
    }
}