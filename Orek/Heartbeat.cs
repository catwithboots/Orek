using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orek
{
    public partial class OrekService
    {
        /// <summary>
        /// Starts the heartbeat thread.
        /// </summary>
        private void StartHeartBeat(int ttl)
        {
            _heartbeatThread = new Thread(() =>HeartBeat(ttl));
            _heartbeatThread.Start();
        }

        /// <summary>
        /// Waits for the heartbeat thread to stop gracefully within the timeout milliseconds
        /// Otherwise aborts the thread
        /// </summary>
        /// <param name="timeout">The timeout in milliseconds.</param>
        private void StopHeartBeat(int timeout)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            _heartbeatThread.Join(TimeSpan.FromMilliseconds(timeout));
            if (_heartbeatThread.ThreadState != ThreadState.Stopped)
            {
                MyLogger.Trace("thread did not reach stopped state within timeout, aborting thread");
                _heartbeatThread.Abort();
            }
        }

        /// <summary>
        /// This method will be called when the heartbeat thread is started.
        /// Heartbeat will be send every half amount of the TTL.
        /// </summary>
        /// <param name="ttl">The heartbeat ttl in milliseconds.</param>
        public void HeartBeat(int ttl)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            while (!_shouldStop)
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
                Thread.Sleep(ttl/2);
            } 
            MyLogger.Trace("Exiting " + MethodBase.GetCurrentMethod().Name);
        }
    }
}
