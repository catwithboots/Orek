using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Orek
{
    public partial class Service : ServiceBase
    {
        private Thread _haertbeatThread;
        //private Thread _actionThread;
        private static readonly Logger MyLogger = Program.MyLogger;
        private Configuration _config;
        private bool _doAction;
        public Service()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            if (CreateConsulClient())
            {
                _consulEnabled = true;
            }
            else
            {
                throw new Exception("ConsulClient initiation failed, Check if local Consul Agent is running");
            };
            _config = ReadConfiguration();
            InitializeComponent();
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
            _doAction = true;
            // Register the Orek Service and Heartbeat in Consul
            RegisterSvcInConsul(_config.Name);
            // Register and Start the Orek Heartbeat thread
            RegisterServiceRunningCheck(_config.Name);
            //RegisterOrekHeartbeatCheck();
            _haertbeatThread=new Thread(OrekHeartbeat);
            _haertbeatThread.Start();
            //For each managed service start managing the service
            StartManagingServices();
        }        

        protected override void OnStop()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            MyLogger.Info("Stopping Service");
            // Set flag to false
            _doAction = false;
            // Wait for ManageService Threads to exit within the timeout or kill them
            StopManagingServices();
            //stop the heartbeat
            _haertbeatThread.Abort();
            //derigister the orek service (which includes the heartbeatcheck)
            DeRegisterSvcInConsul(_config.Name);
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
