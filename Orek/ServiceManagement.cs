using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orek
{
    public partial class Service
    {
        private void StartManagingServices()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            List<Task> taskList= new List<Task>();
            foreach (ManagedService managedService in _config.ManagedServices)
            {
                Task task = StartManageSingleService(managedService);
                taskList.Add(task);
                MyLogger.Debug("Started the ManageSingleService task for {0}",managedService.ConsulServiceName);
            }
            foreach (Task task in taskList) task.Wait();
            MyLogger.Debug("All tasks done, Exit StartManagingServices");
        }

        private Task StartManageSingleService(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            return Task.Run(() =>
            {
                //Register Service in Consul
                RegisterSvcInConsul(svc.ConsulServiceName);
                //Register Service Check in Consul
                RegisterServiceRunningCheck(svc.ConsulServiceName);
                //Register the Service Ready Check
                RegisterServiceReadyCheck(svc.ConsulServiceName);
                //Start monitoring the service; 
                var service = svc;
                svc.MonitorThread = new Thread(() => MonitorService(service));
                svc.MonitorThread.Start();
                //Start the Standby mode thread which will start the active thread when needed and possible 
                svc.GetLockThread = new Thread(() => Standby(service));
                svc.GetLockThread.Start();
                MyLogger.Trace("Exiting {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            });
        }

        private void StopManagingServices()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            List<Task> taskList = new List<Task>();
            foreach (ManagedService managedService in _config.ManagedServices)
            {
                Task task = StopManageSingleService(managedService);
                taskList.Add(task);
                MyLogger.Debug("Started the StopManageSingleService task for {0}", managedService.ConsulServiceName);
            }
            foreach (Task task in taskList) task.Wait();
            MyLogger.Debug("All tasks done, Exit StopManagingServices");
        }

        private Task StopManageSingleService(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            return Task.Run(() =>
            {
                MyLogger.Debug("Stopping all stuff for {0}", svc.ConsulServiceName);
                if (svc.GetLockThread != null) svc.GetLockThread.Abort();
                MyLogger.Trace("Getlockthread for {0} stopped", svc.ConsulServiceName);
                if (svc.MonitorThread != null) svc.MonitorThread.Abort();
                MyLogger.Trace("MonitorThread for {0} stopped", svc.ConsulServiceName);
                StopService(svc);
                CleanUpSemaphore(svc);
                if (svc.RunThread != null) svc.RunThread.Abort();
                MyLogger.Trace("Getlockthread for {0} stopped", svc.ConsulServiceName);
                DeRegisterSvcInConsul(svc.ConsulServiceName);
            });
        }

        /// <summary>
        /// Cleanups the service and related components form Consul.
        /// </summary>
        /// <param name="svc">The managed service.</param>
        private void CleanupService(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            DeRegisterSvcInConsul(svc.ConsulServiceName);
        }

        /// <summary>
        /// Monitors the service running and ready state.
        /// </summary>
        /// <param name="svc">The SVC.</param>
        void MonitorService(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            //Should we continue or is the Orek service stopping
            while (_doAction)
            {
                //Check if service is ready
                svc.CanRun=CheckServiceReady(svc);
                //Check if service is running
                CheckServiceRunning(svc);
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Checks if the service status is "Running" and puts either a Pass or a Fail to the TTL Check
        /// </summary>
        /// <param name="svc">The managed service.</param>
        private void CheckServiceRunning(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            string stat = ServiceHelper.GetServiceStatus(svc.WindowsServiceName);
            if (stat == "Running")
            {
                _consulClient.Agent.PassTTL(svc.ConsulServiceName + "_Running", stat);
            }
            else
            {
                _consulClient.Agent.FailTTL(svc.ConsulServiceName + "_Running", stat);
            }
        }

        /// <summary>
        /// Checks if the service is ready to run.
        /// For now that means the service startup should be set to Manual.
        /// </summary>
        /// <param name="svc">The managed service.</param>
        private bool CheckServiceReady(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            try
            {
                PermissionSet ps = GetServicePermission(svc);
            }
            catch (Exception ex)
            {
                MyLogger.Error("Service Control permission for service {0} cannot be acquired: {1}", svc.WindowsServiceName,ex.Message);
                MyLogger.Debug(ex);
                _consulClient.Agent.FailTTL(svc.ConsulServiceName + "_Ready", "Service Control Permission not granted");
                return false;
            }
            string stat = ServiceHelper.GetStartupType(svc.WindowsServiceName);
            if (stat != "Manual")
            {
                _consulClient.Agent.FailTTL(svc.ConsulServiceName + "_Ready",
                    "Service Startup not set to Manual");
                return false;
            }
            _consulClient.Agent.PassTTL(svc.ConsulServiceName + "_Ready", stat);
            return true;
        }

        private bool StopService(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            bool stopped = false;
            try
            {
                PermissionSet ps = GetServicePermission(svc);
                ps.Assert();
            }
            catch (Exception ex)
            {
                MyLogger.Error("Cannot aquire service control permisison: {0}", ex.Message);
                MyLogger.Debug(ex);
            }
            ServiceController sc = new ServiceController(svc.WindowsServiceName, Environment.MachineName);
            if (sc.Status != ServiceControllerStatus.Stopped)
            {
                if (sc.Status == ServiceControllerStatus.StopPending)
                {
                    MyLogger.Debug("Stop pending");
                }
                else
                {
                    MyLogger.Info("Service {0} send stop command", svc.WindowsServiceName);
                    sc.Stop();
                }
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10.0));
                stopped = sc.Status == ServiceControllerStatus.Stopped;
                if (!stopped) MyLogger.Error("Service did not stop correctly, current status: {0}", sc.Status);
            }            
            sc.Close();
            CodeAccessPermission.RevertAssert();
            return stopped;
        }

        /// <summary>
        /// Gets the service control permission.
        /// </summary>
        /// <param name="svc">The SVC.</param>
        /// <returns></returns>
        /// <exception cref="System.Security.SecurityException">when the permission cannot be acquired</exception>
        private PermissionSet GetServicePermission(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            // Creates a permission set that allows no access to the resource. 
            System.Security.PermissionSet ps = new System.Security.PermissionSet(System.Security.Permissions.PermissionState.None);
            // Sets the security permission flag to use for this permission set.
            ps.AddPermission(new System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityPermissionFlag.Assertion));
            // Initializes a new instance of the System.ServiceProcess.ServiceControllerPermission class.
            ps.AddPermission(new System.ServiceProcess.ServiceControllerPermission(ServiceControllerPermissionAccess.Control, Environment.MachineName, svc.WindowsServiceName));
            ps.Demand();
            return ps;
        }

        /// <summary>
        /// Standby action for the specified SVC.
        /// Which means if the service is ready this thread should wait if a lock on the semaphore is acquired.
        /// When the semaphore is acquired the Active thread should start
        /// </summary>
        /// <param name="svc">The SVC.</param>
        void Standby(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            svc.ShouldRun = false;
            RegisterSemaphore(svc);
            // Wait if service is ready to manage
            if (!svc.CanRun) MyLogger.Info("Wait for service {0} to get in a ready state", svc.ConsulServiceName);
            while (_doAction && !svc.CanRun)
            {
                Thread.Sleep(1000);
            }
            MyLogger.Debug("Service {0} is ready, wait for lock", svc.ConsulServiceName);
            while (_doAction &&
                  (svc.Semaphore != null) &&
                  !svc.Semaphore.IsHeld)
            {
                MyLogger.Info("Acquiring lock on semaphore for {0}", svc.ConsulServiceName);
                try
                {
                    svc.Semaphore.Acquire();
                }
                catch
                {
                    Thread.Sleep(1000);

                }
            }
            if (_doAction)
            {
                MyLogger.Info("Lock acquired, {0} becoming active", svc.ConsulServiceName);
                svc.ShouldRun = true;
                svc.RunThread = new Thread(() => Active(svc));
                svc.RunThread.Start();
            }
        }

        /// <summary>
        /// Active action for the specified SVC.
        /// Which means the service should run as long as a lock on the semaphore is held.
        /// When the semaphore is no longer held the Standby thread should start
        /// </summary>
        /// <param name="svc">The SVC.</param>
        void Active(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}",MethodBase.GetCurrentMethod().Name,svc.ConsulServiceName);
            if (ServiceHelper.GetServiceStatus(svc.WindowsServiceName) != "Running")
            {
                ServiceController sc = new ServiceController(svc.WindowsServiceName);
                sc.Start();
                MyLogger.Info("Service {0} send start command", svc.WindowsServiceName);
                sc.Close();
            }
            while (_doAction && svc.Semaphore.IsHeld && svc.ShouldRun)
            {
                Thread.Sleep(1000);
                ServiceController sc = new ServiceController(svc.WindowsServiceName);
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    MyLogger.Debug("Service not running, releasing lock");
                    if ((svc.Semaphore !=null)&&(svc.Semaphore.IsHeld)) svc.Semaphore.Release();
                }
                sc.Close();
            }
            if (!svc.Semaphore.IsHeld)
            {
                MyLogger.Info("Lock no longer held, waiting for service timeout before trying again", svc.WindowsServiceName);
                Thread.Sleep(svc.Timeout);
            }
            else
            {
                svc.Semaphore.Release();
            }
            if (ServiceHelper.GetServiceStatus(svc.WindowsServiceName) == "Running") StopService(svc);            
            if (_doAction)
            {
                svc.GetLockThread = new Thread(() => Standby(svc));
                svc.GetLockThread.Start();
            }
        }
    }
}
