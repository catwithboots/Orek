using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Orek
{
    static class Program
    {
        internal static Logger MyLogger=LogManager.GetCurrentClassLogger();
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            MyLogger.Trace("Entering " + MethodBase.GetCurrentMethod().Name);
            MyLogger.Info("Start");
            Service orekService = null;
            try
            {
                orekService = new Service();
            }
            catch (Exception ex)
            {
                MyLogger.Fatal("Initiation Failed, exiting service: {0}",ex.Message);
                MyLogger.Debug(ex);
                Environment.Exit(1);
            }
            if (Environment.UserInteractive)
            {
                MyLogger.Debug("Userinteractive session found, starting on console..");
                orekService.StartConsole(args);
                Console.WriteLine("Press any key to stop program");
                Console.Read();
                MyLogger.Debug("Key pressed, stopping console run....");
                orekService.StopConsole();
            }
            else
            {
                MyLogger.Debug("Not Interactive Startup, starting as Service");                
                ServiceBase.Run(orekService);
            }
            MyLogger.Info("Finished");
        }
    }
}
