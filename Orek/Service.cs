using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThreadState = System.Threading.ThreadState;

namespace Orek
{
    public partial class OrekService
    {
        private void StartService()
        {
            _serviceManagementThread = new Thread(Service);
            _serviceManagementThread.Start();
        }
        private void StopService()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            _serviceManagementThread.Join(TimeSpan.FromMilliseconds(_onlineConfig.OrekSettings.StopTimeout));
            if (_serviceManagementThread.ThreadState != ThreadState.Stopped)
            {
                MyLogger.Trace("thread did not reach stopped state within timeout, aborting thread");
                _serviceManagementThread.Abort();
            }
        }
        public void Service()
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            while (!_shouldStop)
            {
                MyLogger.Trace("Starting loop");
                Stopwatch sw=new Stopwatch();
                sw.Start();
                //Send Heartbeat
                try
                {
                    _consulClient.Agent.PassTTL("OREK" + "_Running", "is running");
                }
                catch (Exception ex)
                {
                    MyLogger.Error("Error sending heartbeat: {0}", ex.Message);
                    MyLogger.Debug(ex);
                    _shouldStop = true;
                }
                //Check if config has changed

                //wait before looping
                if (sw.ElapsedMilliseconds < (_onlineConfig.OrekSettings.HeartBeatTtl*80/100))
                {
                    MyLogger.Trace("Waiting before looping again");
                    Thread.Sleep(TimeSpan.FromMilliseconds((_onlineConfig.OrekSettings.HeartBeatTtl * 80 / 100) - sw.ElapsedMilliseconds));
                }
            }
            MyLogger.Info("ServiceManagement Thread ended, exiting program");
        }
    }
}
