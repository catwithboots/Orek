using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using NLog;

namespace Orek
{
    public partial class OrekService : ServiceBase
    {
        private Thread _heartbeatThread;
        private Thread _monitorConfigThread;
        private Thread _serviceManagementThread;
        private static readonly Logger MyLogger = Program.MyLogger;
        internal readonly Configuration Config;
        private volatile bool _shouldStop;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrekService"/> class.
        /// </summary>
        /// <exception cref="System.Exception">
        /// ConsulClient initiation failed, Check if local Consul Agent is running
        /// or
        /// Reading or parsing configfile failed
        /// </exception>
        public OrekService()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            if (!CreateConsulClient()) throw new Exception("ConsulClient initiation failed, Check if local Consul Agent is running");
            Config=new Configuration(this);
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);                     
            MyLogger.Info("Starting Service");
            // Set flag to true
            _shouldStop = false;
            // Register the Orek Service in Consul
            RegisterService(Config.Name);
            // Register the heartbeat check
            RegisterServiceRunningCheck(Config.Name,Config.HeartBeatTtl);
            //Create and start the heartbeat thread
            StartHeartBeat(Config.HeartBeatTtl);
            StartMonitorConfig();
            //Give a bit time to get the initial config
            Thread.Sleep(1000);
            //Start the manageServices Thread
            StartServiceManagement();
            MyLogger.Debug("Onstart Completed");
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Stop command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            MyLogger.Info("Stopping Service");
            // Set flag to false
            _shouldStop = true;
            // Wait for ManageService Threads to exit within the timeout or kill them
            StopMonitorConfig();
            StopServiceManagement();
            //stop the heartbeat
            StopHeartBeat(Config.TimeOut);
            //derigister the orek service (which includes the heartbeatcheck)
            DeRegisterService(Config.Name);

            MyLogger.Debug("OnStop Completed");
        }

        /// <summary>
        /// Starts the service as console application when running interactively.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public void StartConsole(string[] args)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            OnStart(args);
        }
        /// <summary>
        /// Stops the service as console application when running interactively.
        /// </summary>
        public void StopConsole()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            OnStop();
        }

    }
}
