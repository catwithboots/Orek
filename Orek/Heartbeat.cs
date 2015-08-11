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
            MyLogger.Trace("Exiting " + MethodBase.GetCurrentMethod().Name);
        }

        /// <summary>
        /// This method will be called when the heartbeat thread is started.
        /// Heartbeat will be send every half amount of the TTL.
        /// </summary>
        /// <param name="ttl">The heartbeat ttl in milliseconds.</param>
        public void HeartBeat(int ttl)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            Stopwatch sw = new Stopwatch();
            while (!_shouldStop)
            {
                sw.Restart();
                SendPassTTL(Config.Name + "_Running", "is running");
                if (sw.ElapsedMilliseconds < (ttl / 2))
                {
                    Thread.Sleep(Convert.ToInt32((ttl / 2) - sw.ElapsedMilliseconds));
                }
                else
                {
                    MyLogger.Warn("Sending Heartbeat takes longer than 50% of the Heartbeat ttl, Consider increasing the ttl value");
                }
            } 
            MyLogger.Trace("Exiting " + MethodBase.GetCurrentMethod().Name);
        }
    }
}
