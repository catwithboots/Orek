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
        private Thread _actionThread;
        private static readonly Logger MyLogger = Program.MyLogger;
        private Configuration _config;
        private bool _doAction = true;
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

        protected override void OnStart(string[] args)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);                     
            MyLogger.Info("Starting Service");
            RegisterSvcInConsul(_config.Name);
            RegisterOrekHeartbeatCheck();
            _haertbeatThread=new Thread(Heartbeat);
            _haertbeatThread.Start();
            _actionThread = new Thread(Action);
            _actionThread.Start();
        }

        protected override void OnStop()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            MyLogger.Info("Stopping Service");
            _doAction = false;
            StopAction(TimeSpan.FromMilliseconds(_config.TimeOut));
            _haertbeatThread.Abort();
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
