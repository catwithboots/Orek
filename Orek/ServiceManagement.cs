using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security;
using System.ServiceProcess;
using System.Threading;
using Consul;
using ThreadState = System.Threading.ThreadState;

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
        /// The assigned clusters (see it as active conf)
        /// </summary>
        public readonly List<string> AssignedClusters = new List<string>();

        #endregion ServiceManagement Variables

        #region ServiceManagement Thread Methods

        /// <summary>
        /// Starts the serviceManagement thread.
        /// </summary>
        private void StartServiceManagement()
        {
            _serviceManagementThread = new Thread(ServiceManagement) { IsBackground = false };
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
            List<ManagedService> managedServices = new List<ManagedService>();
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            while (!_shouldStop)
            {
                if (_configChanged)
                {
                    var clustersToRemove = AssignedClusters.Except(Config.ClusterAssignments).ToList();
                    var managedServicesToRemove = managedServices.Where(ms => clustersToRemove.Any(cl => cl == ms.ConsulServiceName));
                    foreach (var ms in StopManagingClusters(managedServicesToRemove)) managedServices.Remove(ms);
                    var clustersToAdd = Config.ClusterAssignments.Except(AssignedClusters).ToList();
                    managedServices.AddRange(StartManagingClusters(clustersToAdd));
                    _configChanged = false;
                }
                else Thread.Sleep(5000);
            }
            StopManagingClusters(managedServices);
        }

        #endregion ServiceManagement Main Method

        #region Overall cluster Management

        private List<ManagedService> StartManagingClusters(IEnumerable<string> clusters)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            List<ManagedService> result = new List<ManagedService>();
            foreach (var cluster in clusters)
            {
                ServiceDef sd = ServiceDef.GetServiceDefinition(this, cluster);
                if (sd == null) continue;
                
                ManagedService ms = sd.ToManagedService();
                RegisterService(sd.ConsulServiceName);
                RegisterServiceRunningCheck(sd.ConsulServiceName, sd.HeartBeatTtl);
                RegisterServiceReadyCheck(sd.ConsulServiceName);
                ms.MonitorThread = new Thread(() => MonitorService(ms)) { IsBackground = false };
                ms.MonitorThread.Start();
                ms.RunThread = new Thread(() => AllInOne(ms));
                ms.RunThread.Start();
                AssignedClusters.Add(cluster);
                result.Add(ms);
            }
            return result;
        }

        private List<ManagedService> StopManagingClusters(IEnumerable<ManagedService> managedClusters)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            // Workaround to prevent "Collection was modiefied" exception
            List<ManagedService> cl = new List<ManagedService>();
            List<ManagedService> result = new List<ManagedService>();
            cl.AddRange(managedClusters);
            try
            {
                foreach (var ms in cl)
                {
                    ms.ShouldRun = false;
                    MyLogger.Info("Send a trigger to stop managing {0}", ms.ConsulServiceName);
                    Thread.Sleep(ms.StopTimeout);
                    try
                    {
                        if (ms.MonitorThread != null) ms.MonitorThread.Abort();
                        if (ms.RunThread != null) ms.RunThread.Abort(); 
                        StopService(ms);
                        CleanUpSemaphore(ms);
                        DeRegisterService(ms.ConsulServiceName);
                        AssignedClusters.Remove(ms.ConsulServiceName);
                        result.Add(ms);
                    }
                    catch (Exception ex1)
                    {
                        MyLogger.Error("Error in stopmanaging services for service {0}: {1}", ms.ConsulServiceName,ex1.Message);
                        MyLogger.Debug(ex1);
                    }
                }
            }
            catch (Exception ex)
            {
                MyLogger.Error("Error in stopmanaging services: {0}", ex.Message);
                MyLogger.Debug(ex);
            }
            return result;
        }

        #endregion Overall cluster Management


        #region Windows Service Management Tasks and Methods

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
                SendPassTTL(svc.ConsulServiceName + "_Running", stat);
            }
            else
            {
                SendFailTTL(svc.ConsulServiceName + "_Running", stat);
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
                GetServicePermission(svc);
            }
            catch (Exception ex)
            {
                MyLogger.Error("Service Control permission for service {0} cannot be acquired: {1}", svc.WindowsServiceName, ex.Message);
                MyLogger.Debug(ex);
                SendFailTTL(svc.ConsulServiceName + "_Ready", "Service Control Permission not granted");
                return false;
            }
            string stat = ServiceHelper.GetStartupType(svc.WindowsServiceName);
            if (stat != "Manual")
            {
                SendFailTTL(svc.ConsulServiceName + "_Ready","Service Startup not set to Manual");                
                return false;
            }
            SendPassTTL(svc.ConsulServiceName + "_Ready", stat);            
            return true;
        }

        private bool StartService(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            try
            {
                PermissionSet ps = GetServicePermission(svc);
                ps.Assert();
            }
            catch (Exception ex)
            {
                MyLogger.Error("Cannot aquire service control permisison: {0}", ex.Message);
                MyLogger.Debug(ex);
                return false;
            }
            ServiceController sc = new ServiceController(svc.WindowsServiceName, Environment.MachineName);
            if (sc.Status != ServiceControllerStatus.Running)
            {
                if (sc.Status == ServiceControllerStatus.StartPending)
                {
                    MyLogger.Debug("Start pending");
                }
                else
                {
                    MyLogger.Info("Service {0} send start command", svc.WindowsServiceName);
                    sc.Start();
                }
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(svc.StartTimeout));
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    MyLogger.Error("Service did not start within starttimeout period, current status: {0}", sc.Status);
                    sc.Close();
                    CodeAccessPermission.RevertAssert();
                    return false;
                }
            }
            sc.Close();
            CodeAccessPermission.RevertAssert();
            return true;
        }

        private bool StopService(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            try
            {
                PermissionSet ps = GetServicePermission(svc);
                ps.Assert();
            }
            catch (Exception ex)
            {
                MyLogger.Error("Cannot aquire service control permisison: {0}", ex.Message);
                MyLogger.Debug(ex);
                return false;
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
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    MyLogger.Error("Service did not stop correctly, current status: {0}", sc.Status);
                    sc.Close();
                    CodeAccessPermission.RevertAssert();
                    return false;
                }
            }
            sc.Close();
            CodeAccessPermission.RevertAssert();
            return true;
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
            PermissionSet ps = new PermissionSet(System.Security.Permissions.PermissionState.None);
            // Sets the security permission flag to use for this permission set.
            ps.AddPermission(new System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityPermissionFlag.Assertion));
            // Initializes a new instance of the System.ServiceProcess.ServiceControllerPermission class.
            ps.AddPermission(new ServiceControllerPermission(ServiceControllerPermissionAccess.Control, Environment.MachineName, svc.WindowsServiceName));
            ps.Demand();
            return ps;
        }

        #endregion Windows Service Management Methods

        #region Single Service Management Thread Methods

        /// <summary>
        /// Monitors the service running and ready state.
        /// </summary>
        /// <param name="svc">The SVC.</param>
        void MonitorService(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            MyLogger.Info("Start monitoring service {0} for cluster{1}",svc.WindowsServiceName,svc.ConsulServiceName);
            //Should we continue or is the Orek service stopping
            Stopwatch sw = new Stopwatch();
            while (!_shouldStop)
            {
                sw.Restart();
                //Check if service is ready
                svc.CanRun = CheckServiceReady(svc);
                //Check if service is running
                CheckServiceRunning(svc);
                if (sw.ElapsedMilliseconds < (svc.HeartBeatTtl/2))
                {
                    Thread.Sleep(Convert.ToInt32((svc.HeartBeatTtl/2) - sw.ElapsedMilliseconds));
                }
                else
                {
                    MyLogger.Warn("Monitoring Service takes longer than 50% of the Heartbeat ttl, Consider increasing the ttl value");
                }
            }            
            MyLogger.Info("Stopped monitoring service {0} for cluster{1}", svc.WindowsServiceName, svc.ConsulServiceName);
        }

        void AllInOne(ManagedService svc)
        {
            MyLogger.Trace("Entering {0} for service: {1}", MethodBase.GetCurrentMethod().Name, svc.ConsulServiceName);
            
            // Wait if service is ready to manage
            if (!svc.CanRun) MyLogger.Info("Wait for service {0} to get in a ready state", svc.ConsulServiceName);
            while (!_shouldStop && !svc.CanRun && svc.ShouldRun)
            {
                Thread.Sleep(1000);
            }
            MyLogger.Debug("Service {0} is ready, wait for lock", svc.ConsulServiceName);
            while (!_shouldStop && svc.ShouldRun)
            {
                var semaphoreOptions = new SemaphoreOptions(Config.KvPrefix + Config.SemaPrefix + svc.ConsulServiceName,
                    svc.Limit) { SessionName = svc.ConsulServiceName + "_Session", SessionTTL = TimeSpan.FromSeconds(10) };
                var semaphore = ConsulClient.Semaphore(semaphoreOptions);
                // Try to get a lock until it is aqcuired or the program should stop
                while (!_shouldStop && !semaphore.IsHeld && svc.ShouldRun)
                {
                    MyLogger.Info("Acquiring lock on semaphore for {0}", svc.ConsulServiceName);
                    try
                    {
                        semaphore.Acquire();
                    }
                    catch
                    {
                        Thread.Sleep(1000);

                    }
                }
                // if the program should not stop and the semaphore is aqcuired, start the service
                if (!_shouldStop && svc.ShouldRun && semaphore.IsHeld && (ServiceHelper.GetServiceStatus(svc.WindowsServiceName) != "Running"))
                {
                    bool svcstarted = StartService(svc);
                    MyLogger.Info("Service {0} send start command with success: {1}", svc.WindowsServiceName, svcstarted);
                }
                // Make sure the service runs while the lock is held or the program should stop, otherwise stop the service and releade the lock
                while (!_shouldStop && svc.ShouldRun && semaphore.IsHeld)
                {
                    ServiceController sc = new ServiceController(svc.WindowsServiceName);
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        MyLogger.Debug("Service not running, releasing lock");
                        if (semaphore.IsHeld) semaphore.Release();
                    }
                    sc.Close();
                }
                if (_shouldStop || !svc.ShouldRun)
                {
                    if (ServiceHelper.GetServiceStatus(svc.WindowsServiceName) == "Running")
                    {
                        bool svcstopped = StopService(svc);
                        MyLogger.Info("Service {0} send stop command with success: {1}", svc.WindowsServiceName, svcstopped);
                    }
                    if (semaphore.IsHeld) semaphore.Release();
                    try
                    {
                        semaphore.Destroy();
                    }
                    catch (Exception ex)
                    {
                        MyLogger.Error("some error: {0}",ex.Message);
                        MyLogger.Debug(ex);
                    }
                }                
                else
                {
                    MyLogger.Info("Lock no longer held for {0}",svc.WindowsServiceName);
                    //Release lock because it could be the lock isheld was set to false due to consistency timeout
                    try
                    {
                        semaphore.Release();
                    }
                    catch (SemaphoreNotHeldException)
                    {
                    }
                    catch (Exception ex1) {
                        MyLogger.Error("Some error: {0}",ex1.Message);
                        MyLogger.Debug(ex1);
                    }
                    if (ServiceHelper.GetServiceStatus(svc.WindowsServiceName) == "Running")
                    {
                        bool svcstopped = StopService(svc);
                        MyLogger.Info("Service {0} send stop command with success: {1}", svc.WindowsServiceName, svcstopped);
                    }
                }                
            }
        }
        #endregion Single Service Management Thread Methods
    }
}
