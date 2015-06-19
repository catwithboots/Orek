using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Management;
using System.Security;

namespace Orek
{
    public static class ServiceHelper
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern Boolean ChangeServiceConfig(
            IntPtr hService,
            UInt32 nServiceType,
            UInt32 nStartType,
            UInt32 nErrorControl,
            String lpBinaryPathName,
            String lpLoadOrderGroup,
            IntPtr lpdwTagId,
            [In] char[] lpDependencies,
            String lpServiceStartName,
            String lpPassword,
            String lpDisplayName);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr OpenService(
            IntPtr hScManager, string lpServiceName, uint dwDesiredAccess);

        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode,
            SetLastError = true)]
        public static extern IntPtr OpenSCManager(
            string machineName, string databaseName, uint dwAccess);

        [DllImport("advapi32.dll", EntryPoint = "CloseServiceHandle")]
        public static extern int CloseServiceHandle(IntPtr hScObject);

        private const uint ServiceNoChange = 0xFFFFFFFF;
        private const uint ServiceQueryConfig = 0x00000001;
        private const uint ServiceChangeConfig = 0x00000002;
        private const uint ScManagerAllAccess = 0x000F003F;
        private const uint ScManagerConnect = 0x0001;
        private const uint ScManagerEnumerateService = 0x0004;

        public static void ChangeStartMode(ServiceController svc, ServiceStartMode mode)
        {
            //var scManagerHandle = OpenSCManager(null, null, SC_MANAGER_CONNECT + SC_MANAGER_ENUMERATE_SERVICE);
            var scManagerHandle = OpenSCManager(null, null, ScManagerAllAccess);
            if (scManagerHandle == IntPtr.Zero)
            {
                throw new ExternalException("Open Service Manager Error");
            }

            var serviceHandle = OpenService(
                scManagerHandle,
                svc.ServiceName,
                ServiceQueryConfig | ServiceChangeConfig);

            if (serviceHandle == IntPtr.Zero)
            {
                throw new ExternalException("Open Service Error");
            }

            var result = ChangeServiceConfig(
                serviceHandle,
                ServiceNoChange,
                (uint) mode,
                ServiceNoChange,
                null,
                null,
                IntPtr.Zero,
                null,
                null,
                null,
                null);

            if (result == false)
            {
                int nError = Marshal.GetLastWin32Error();
                var win32Exception = new Win32Exception(nError);
                throw new ExternalException("Could not change service start type: "
                                            + win32Exception.Message);
            }

            CloseServiceHandle(serviceHandle);
            CloseServiceHandle(scManagerHandle);
        }

        public static string GetStartupType(string serviceName)
        {
           string wmiQuery = "Select StartMode from Win32_Service where Name='" + serviceName + "'";

            ManagementObjectSearcher wmi = new ManagementObjectSearcher(wmiQuery);
            ManagementObjectCollection coll = wmi.Get();

            foreach (var service in coll)
            {
                return service["StartMode"].ToString();
            }

            return string.Empty;
        }

        public static void SetStartupType(string serviceName, ServiceStartMode mode)
        {
            //if (startupType != "Automatic" && startupType != "Manual" && startupType != "Disabled") ;            
            //throw new Exception("The valid values are Automatic, Manual or Disabled");    
            if(serviceName!=null)
            {
                ////construct the management path
                //string path="Win32_Service.Name='"+serviceName+"'";
                //ManagementPath p=new ManagementPath(path);
                ////construct the management object
                //ManagementObject ManagementObj=new ManagementObject(p);
                ////we will use the invokeMethod method of the ManagementObject class
                //object[] parameters=new object[1];
                //parameters[0]=mode.ToString();
                //var result=ManagementObj.InvokeMethod("ChangeStartMode",parameters); 
                //return Enum.Parse(typeof(string),result.ToString());
                var svc = new ServiceController(serviceName);
                ChangeStartMode(svc, ServiceStartMode.Automatic);
            }        
        }

        /// <summary>
        /// Gets the service status as string.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns></returns>
        public static string GetServiceStatus(string serviceName)
        {
            ServiceController sc = new ServiceController(serviceName);
            switch (sc.Status)
            {
                case ServiceControllerStatus.Running:
                    return "Running";
                case ServiceControllerStatus.Stopped:
                    return "Stopped";
                case ServiceControllerStatus.Paused:
                    return "Paused";
                case ServiceControllerStatus.StopPending:
                    return "Stopping";
                case ServiceControllerStatus.StartPending:
                    return "Starting";
                default:
                    return "Status Changing";
            }
        }

        /// <summary>
        /// Starts the service.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns></returns>
        /// <exception cref="System.Security.SecurityException">when the permission cannot be acquired</exception>
        public static bool StartService(string serviceName)
        {
            PermissionSet ps = GetServicePermission(serviceName);
            ps.Assert();  
            ServiceController sc = new ServiceController(serviceName);
            if (!((sc.Status == ServiceControllerStatus.Running) || (sc.Status == ServiceControllerStatus.StartPending))) sc.Start();            
            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
            bool result = sc.Status == ServiceControllerStatus.Running;
            sc.Close();
            CodeAccessPermission.RevertAssert();
            return result;
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns></returns>
        /// <exception cref="System.Security.SecurityException">when the permission cannot be acquired</exception>
        public static bool StopService(string serviceName)
        {
            PermissionSet ps = GetServicePermission(serviceName);
            ps.Assert();            
            ServiceController sc = new ServiceController(serviceName, Environment.MachineName);
            if (!((sc.Status == ServiceControllerStatus.Stopped)||(sc.Status == ServiceControllerStatus.StopPending))) sc.Stop();            
            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            bool result = sc.Status == ServiceControllerStatus.Stopped;            
            sc.Close();
            CodeAccessPermission.RevertAssert();
            return result;
        }

        /// <summary>
        /// Gets the service control permission.
        /// </summary>
        /// <param name="svc">The SVC.</param>
        /// <returns></returns>
        /// <exception cref="System.Security.SecurityException">when the permission cannot be acquired</exception>
        public static PermissionSet GetServicePermission(string serviceName)
        {
            // Creates a permission set that allows no access to the resource. 
            PermissionSet ps = new PermissionSet(System.Security.Permissions.PermissionState.None);
            // Sets the security permission flag to use for this permission set.
            ps.AddPermission(new System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityPermissionFlag.Assertion));
            // Initializes a new instance of the System.ServiceProcess.ServiceControllerPermission class.
            ps.AddPermission(new ServiceControllerPermission(ServiceControllerPermissionAccess.Control, Environment.MachineName, serviceName));
            ps.Demand();
            return ps;
        }
    }
}
