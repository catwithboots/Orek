using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using System.Security;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Semaphore = System.Threading.Semaphore;

namespace Orek
{
    public partial class OrekService
    {
        #region ServiceManagement Variables
        /// <summary>
        /// Flag to keep track if the config changed
        /// </summary>
        private volatile bool _configChanged;
        /// <summary>
        /// The active managed services
        /// </summary>
        private List<ManagedService> _managedServices = new List<ManagedService>();
        /// <summary>
        /// The new managed services (if the config changed)
        /// </summary>
        private List<ServiceDef> _newManagedServices;
        #endregion ServiceManagement Variables

        #region ServiceManagement Main Thread Methods
        /// <summary>
        /// Starts the serviceManagement thread.
        /// </summary>
        private void StartServiceManagement()
        {
            _serviceManagementThread = new Thread(() => ServiceManagement());
            _serviceManagementThread.Start();
        }
        /// <summary>
        /// Waits for the serviceManagement thread to stop gracefully within the timeout milliseconds
        /// Otherwise aborts the thread
        /// </summary>
        /// <param name="timeout">The timeout in milliseconds.</param>
        private void StopServiceManagement(int timeout = 15000)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            _serviceManagementThread.Join(TimeSpan.FromMilliseconds(timeout));
            if (_serviceManagementThread.ThreadState != ThreadState.Stopped)
            {
                MyLogger.Trace("thread did not reach stopped state within timeout, aborting thread");
                _serviceManagementThread.Abort();
            }
        }
        #endregion ServiceManagement Main Thread Methods

        #region ServiceManagement Main Method
        /// <summary>
        /// This method will be called when the serviceManagement thread is started.
        /// </summary>
        public void ServiceManagement()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            while (!_shouldStop)
            {
                if (_configChanged)
                {
                    List<ManagedService> servicesToRemove = GetServicesFromFirstNotInSecondList(_managedServices, _newManagedServices).OfType<ManagedService>().ToList();
                    List<ServiceDef> servicesToAdd = GetServicesFromFirstNotInSecondList(_newManagedServices, _managedServices).ToList();
                    StopManagingServices(servicesToRemove);
                    StartManagingServices(servicesToAdd);
                    _configChanged = false;
                }
                else Thread.Sleep(5000);
            }
            StopManagingServices(_managedServices);
            MyLogger.Info("ServiceManagement Thread ended, exiting program");
            Environment.Exit(2);
        }

        

        /// <summary>
        /// Gets the services from first but not in second list.
        /// </summary>
        /// <param name="firstList">The first list.</param>
        /// <param name="secondList">The second list.</param>
        /// <returns>a new list</returns>
        private List<ServiceDef> GetServicesFromFirstNotInSecondList(IEnumerable<ServiceDef> firstList,
            IEnumerable<ServiceDef> secondList)
        {
            var serviceDefs = firstList as IList<ServiceDef> ?? firstList.ToList();
            IEnumerable<ServiceDef> diffQuery = serviceDefs.Except(secondList, new ServiceComparer());
            return diffQuery.ToList();
        }

        private void StartManagingServices(List<ServiceDef> serviceList)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            List<Task> taskList = new List<Task>();
            if (serviceList.Count != 0)
            {
                foreach (var serviceDef in serviceList)
                {
                    ManagedService managedService = new ManagedService
                    {
                        WindowsServiceName = serviceDef.WindowsServiceName,
                        ConsulServiceName = serviceDef.ConsulServiceName,
                        Timeout = serviceDef.Timeout,
                        Limit = serviceDef.Limit,
                        HeartBeatTtl = serviceDef.HeartBeatTtl
                    };
                    Task task = StartManageSingleService(managedService);
                    taskList.Add(task);
                    MyLogger.Debug("Started the ManageSingleService task for {0}", managedService.ConsulServiceName);
                }
                foreach (Task task in taskList) task.Wait();
            }
            MyLogger.Debug("All tasks done, Exit StartManagingServices");
        }

        /// <summary>
        /// Stops managing the list of services.
        /// </summary>
        /// <param name="serviceList">The service list.</param>
        private void StopManagingServices(List<ManagedService> serviceList)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            List<Task> taskList = new List<Task>();
            foreach (ManagedService managedService in serviceList)
            {
                Task task = StopManageSingleService(managedService);
                taskList.Add(task);
                MyLogger.Debug("Started the StopManageSingleService task for {0}", managedService.ConsulServiceName);
            }
            foreach (Task task in taskList) task.Wait();
            MyLogger.Debug("All tasks done, Exit StopManagingServices");
        }
        #endregion ServiceManagement Main Method

        #region Single Service Management Tasks and Methods
        private Task StartManageSingleService(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            return Task.Run(() =>
            {
                //Register Service in Consul
                RegisterService(svc.ConsulServiceName);
                //Register Service Check in Consul
                RegisterServiceRunningCheck(svc.ConsulServiceName, svc.HeartBeatTtl);
                //Register the Service Ready Check
                RegisterServiceReadyCheck(svc.ConsulServiceName);
                //Start monitoring the service; 
                svc.ShouldRun = true;
                //var service = svc;
                svc.MonitorThread = new Thread(() => MonitorService(svc));
                svc.MonitorThread.Start();
                //Start the Standby mode thread which will start the active thread when needed and possible 
                //svc.GetLockThread = new Thread(() => Standby(service));
                //svc.GetLockThread.Start();
                svc.ManageThread = new Thread(() => Manage(svc));
                svc.ManageThread.Start();
                _managedServices.Add(svc);
                MyLogger.Trace("Exiting {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            });
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
                svc.ShouldRun = false;
                MyLogger.Trace("Stopping service {0}", svc.WindowsServiceName);
                try
                {
                    if (ServiceHelper.StopService(svc.WindowsServiceName))
                        MyLogger.Info("Service {0} Stopped", svc.WindowsServiceName);
                    else MyLogger.Error("Service {0} did not stop within 30 seconds");
                }
                catch (Exception ex)
                {
                    MyLogger.Error("Error stopping service {0}: {1}", svc.WindowsServiceName, ex.Message);
                    MyLogger.Debug(ex);
                }
                CleanUpSemaphore(svc);
                if (svc.RunThread != null) svc.RunThread.Abort();
                MyLogger.Trace("Getlockthread for {0} stopped", svc.ConsulServiceName);
                DeRegisterService(svc.ConsulServiceName);
                _managedServices.Remove(svc);
            });
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
                ServiceHelper.GetServicePermission(svc.WindowsServiceName);
            }
            catch (Exception ex)
            {
                MyLogger.Error("Service Control permission for service {0} cannot be acquired: {1}", svc.WindowsServiceName, ex.Message);
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
        
        
        #endregion Single Service Management Methods

        #region Single Service Management Thread Methods
        /// <summary>
        /// Monitors the service running and ready state.
        /// </summary>
        /// <param name="svc">The SVC.</param>
        void MonitorService(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            //Should we continue or is the Orek service stopping
            while (!_shouldStop)
            {
                try
                {
                    //Check if service is ready
                    svc.CanRun = CheckServiceReady(svc);
                    //Check if service is running
                    CheckServiceRunning(svc);
                }
                catch
                {
                    MyLogger.Error("Error monitoring service {0}",svc.WindowsServiceName);
                }
                finally
                {
                    Thread.Sleep(1000);
                }
            }
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
            while (!_shouldStop && !svc.CanRun)
            {
                Thread.Sleep(1000);
            }
            MyLogger.Debug("Service {0} is ready, wait for lock", svc.ConsulServiceName);
            while (!_shouldStop &&
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
            if (!_shouldStop)
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
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            if (ServiceHelper.GetServiceStatus(svc.WindowsServiceName) != "Running")
            {
                ServiceController sc = new ServiceController(svc.WindowsServiceName);
                sc.Start();
                MyLogger.Info("Service {0} send start command", svc.WindowsServiceName);
                sc.Close();
            }
            //while (!_shouldStop && svc.Semaphore.IsHeld && svc.ShouldRun)
            while (!_shouldStop && svc.Semaphore.IsHeld && svc.ShouldRun)
            {
                Thread.Sleep(1000);
                ServiceController sc = new ServiceController(svc.WindowsServiceName);
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    MyLogger.Debug("Service not running, releasing lock");
                    if ((svc.Semaphore != null) && (svc.Semaphore.IsHeld)) svc.Semaphore.Release();
                }
                sc.Close();
            }
            if (!svc.ShouldRun || _shouldStop)
            {
                if (ServiceHelper.GetServiceStatus(svc.WindowsServiceName) == "Running")
                {
                    MyLogger.Trace("Stopping service {0}", svc.WindowsServiceName);
                    try
                    {
                        if (ServiceHelper.StopService(svc.WindowsServiceName))
                            MyLogger.Info("Service {0} Stopped", svc.WindowsServiceName);
                        else MyLogger.Error("Service {0} did not stop within 30 seconds");
                    }
                    catch (Exception ex)
                    {
                        MyLogger.Error("Error stopping service {0}: {1}", svc.WindowsServiceName, ex.Message);
                        MyLogger.Debug(ex);
                    }
                }
                if (svc.Semaphore.IsHeld) svc.Semaphore.Release();
            }
            else
            {
                MyLogger.Info("Lock no longer held for {0}, waiting for service timeout before trying again", svc.WindowsServiceName);
                //Release lock because it could be the lock isheld was set to false due to consistency timeout
                try { svc.Semaphore.Release(); }
                catch { MyLogger.Debug("Lock should not be held, but release said otherwise, probably due to cinsitency related timeout"); }
                if (ServiceHelper.GetServiceStatus(svc.WindowsServiceName) == "Running")
                {
                    MyLogger.Trace("Stopping service {0}", svc.WindowsServiceName);
                    try
                    {
                        if (ServiceHelper.StopService(svc.WindowsServiceName))
                            MyLogger.Info("Service {0} Stopped", svc.WindowsServiceName);
                        else MyLogger.Error("Service {0} did not stop within 30 seconds");
                    }
                    catch (Exception ex)
                    {
                        MyLogger.Error("Error stopping service {0}: {1}", svc.WindowsServiceName, ex.Message);
                        MyLogger.Debug(ex);
                    }
                }
                Thread.Sleep(svc.Timeout);
                svc.GetLockThread = new Thread(() => Standby(svc));
                svc.GetLockThread.Start();
            }
        }


        /// <summary>
        /// Manage action for the specified SVC.
        /// Which means if the service is ready this thread should wait if a lock on the semaphore is acquired.
        /// When the semaphore is acquired the service should start untill the lock is lost again
        /// </summary>
        /// <param name="svc">The SVC.</param>
        void Manage(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            Consul.Semaphore semaphore = null;
            while (!_shouldStop && svc.ShouldRun)
            {

                var semaphoreOptions = new SemaphoreOptions(_config.KvPrefix + svc.ConsulServiceName + _config.SemaPrefix,
                    svc.Limit) { SessionName = svc.ConsulServiceName + "_Session", SessionTTL = TimeSpan.FromSeconds(10) };
                
                if (semaphore != null) try { semaphore.Destroy(); }
                    catch (SemaphoreInUseException) { }
                    catch (Exception) { }
                semaphore = _consulClient.Semaphore(semaphoreOptions);
                //RegisterSemaphore(svc);
                
                // Wait if service is ready to manage
                if (!svc.CanRun) MyLogger.Info("Wait for service {0} to get in a ready state", svc.ConsulServiceName);
                while (!_shouldStop && svc.ShouldRun && !svc.CanRun)
                {
                    Thread.Sleep(1000);
                }
                //service canrun or shouldstop or not shouldrun
                if (!_shouldStop) MyLogger.Debug("Service {0} is ready, wait for lock", svc.ConsulServiceName);
                while (!_shouldStop &&
                       svc.ShouldRun &&
                       //(svc.Semaphore != null) &&
                       //!svc.Semaphore.IsHeld)
                       (semaphore != null) &&
                       !semaphore.IsHeld)
                {
                    MyLogger.Info("Acquiring lock on semaphore for {0}", svc.ConsulServiceName);
                    try
                    {
                        //svc.Semaphore.Acquire();
                        semaphore.Acquire();
                    }
                    catch
                    {
                        Thread.Sleep(1000);

                    }
                }
                //semaphore held or shouldstop or not shouldrun
                //if (svc.Semaphore != null && (!_shouldStop && svc.ShouldRun && svc.Semaphore.IsHeld))
                if (semaphore != null && (!_shouldStop && svc.ShouldRun && semaphore.IsHeld))
                {
                    MyLogger.Info("Lock acquired, {0} becoming active", svc.ConsulServiceName);
                    try
                    {
                        if (ServiceHelper.StartService(svc.WindowsServiceName))
                            MyLogger.Info("Service {0} started", svc.WindowsServiceName);
                        else MyLogger.Error("Service {0} did not start within 30 seconds", svc.WindowsServiceName);
                    }
                    catch (Exception ex)
                    {
                        MyLogger.Error("Error starting service {0}: {1}",svc.WindowsServiceName,ex.Message);
                        MyLogger.Debug(ex);
                    }
                }
                //while (svc.Semaphore != null && (!_shouldStop && svc.Semaphore.IsHeld && svc.ShouldRun))
                while (semaphore != null && (!_shouldStop && semaphore.IsHeld && svc.ShouldRun))
                {
                    if (ServiceHelper.GetServiceStatus(svc.WindowsServiceName) != "Running")
                    {
                        MyLogger.Debug("Service not running, releasing lock");
                        //if ((svc.Semaphore != null) && (svc.Semaphore.IsHeld)) svc.Semaphore.Release();
                        if ((semaphore != null) && (semaphore.IsHeld)) semaphore.Release();
                    }
                    Thread.Sleep(1000);
                }
                //semaphore lost or shouldstop or not shouldrun
                //if (svc.Semaphore != null && !svc.Semaphore.IsHeld) MyLogger.Info("Lock no longer held for {0}, waiting for service timeout", svc.WindowsServiceName);
                if (semaphore != null && !semaphore.IsHeld)
                {
                    MyLogger.Info("Lock no longer held for {0}, waiting for service timeout", svc.WindowsServiceName);
                }
                if (ServiceHelper.GetServiceStatus(svc.WindowsServiceName) == "Running")
                {
                    MyLogger.Trace("Stopping service {0}",svc.WindowsServiceName);
                    try
                    {
                        if (ServiceHelper.StopService(svc.WindowsServiceName))
                            MyLogger.Info("Service {0} Stopped", svc.WindowsServiceName);
                        else MyLogger.Error("Service {0} did not stop within 30 seconds");
                    }
                    catch (Exception ex)
                    {
                        MyLogger.Error("Error stopping service {0}: {1}", svc.WindowsServiceName, ex.Message);
                        MyLogger.Debug(ex);
                    }
                }
                //Release lock because it could be the lock isheld was set to false due to consistency timeout
                //try { svc.Semaphore.Release(); }
                try { semaphore.Release(); }
                catch { MyLogger.Debug("Lock should not be held, but release said otherwise, probably due to consitency related timeout"); }
            }
            //shouldstop or not svc.shouldrun
            MyLogger.Info(
                !svc.ShouldRun
                    ? "Manage service stop signal detected, stopping Manage thread for {0}"
                    : "Program Stop signal noticed, stopping Manage thread for {0}", svc.WindowsServiceName);
        }
        #endregion Single Service Management Thread Methods
    }
}
