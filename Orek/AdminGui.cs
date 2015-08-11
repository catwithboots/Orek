using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Consul;

namespace Orek
{
    public partial class AdminGui : Form
    {
        private OrekService OrekService { get; set; }
        public AdminGui(OrekService orekServices)
        {
            OrekService = orekServices;
            InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (OrekService.Config.ValidOnlineConfig)
            {
                lblOrekConsulConfigStatus.Text = string.Format("Orek config found at {0}", OrekService.Config.ConfigPrefix);
                btnCreateOrekConsulConfig.Enabled = false;
                tbTimeOut.Enabled = true;
                tbHeatBeatTTL.Enabled = true;
                tbTimeOut.Text = OrekService.Config.TimeOut.ToString();
                tbHeatBeatTTL.Text = OrekService.Config.HeartBeatTtl.ToString();
                toolStripStatusLabel1.Text = "Online config is valid and loaded";
            }
            else
            {
                lblOrekConsulConfigStatus.Text = string.Format("No valid Orek config found at {0}", OrekService.Config.ConfigPrefix);
                btnCreateOrekConsulConfig.Enabled = true;
                tbTimeOut.Text = OrekService.Config.TimeOut.ToString();
                tbHeatBeatTTL.Text = OrekService.Config.HeartBeatTtl.ToString();
                toolStripStatusLabel1.Text = "Online config is invalid, using defaults";
            }
            btnSaveTimeOut.Enabled = false;
            btnCancelTimeOut.Enabled = false;
            btnSveHeartBeatTTL.Enabled = false;
            btnCancelHeartBeatTTL.Enabled = false;
        }

        private void LoadClusters()
        {
            listboxClusters.Items.Clear();
            listboxClusters.Items.AddRange(new object[] {"<New Cluster...>"});
            listboxClusters.SelectedIndex = 0;
            var qr = OrekService.ConsulClient.KV.List(OrekService.Config.ClustersPrefix);
            if ((qr != null) && (qr.Response != null))
            {
                KVPair[] clusterKvPairArray = qr.Response;
                if (clusterKvPairArray.Count() > 1)
                {
                    var searchPattern = new Regex(OrekService.Config.ClustersPrefix+"(.*)/$", RegexOptions.IgnoreCase);
                    var clusterKvPairs = clusterKvPairArray.Where(f => searchPattern.IsMatch(f.Key)).ToList();
                    foreach (var clusterKvPair in clusterKvPairs)
                    {
                        listboxClusters.Items.Add(clusterKvPair.Key.Replace(OrekService.Config.ClustersPrefix, "").Replace("/", ""));
                    }                    
                }
                toolStripStatusLabel1.Text = string.Format("Loaded cluster configuration from {0}", OrekService.Config.ClustersPrefix);
                lblClusterConfigStatus.Text = string.Format("Loaded cluster configuration from {0}", OrekService.Config.ClustersPrefix);
            }
            else
            {
                toolStripStatusLabel1.Text = string.Format("No valid cluster configuration found at {0}", OrekService.Config.ClustersPrefix);
                lblClusterConfigStatus.Text = string.Format("No valid cluster configuration found at {0}", OrekService.Config.ClustersPrefix);
                btnPushClusterConfig.Enabled = true;
            }
        }

        private void btnCreateOrekConsulConfig_Click(object sender, EventArgs e)
        {
            KVPair kvHeartBeatTtl = new KVPair(OrekService.Config.ConfigPrefix + "heartbeatttl") { Value = Encoding.UTF8.GetBytes(tbTimeOut.Text) };
            bool result = OrekService.ConsulClient.KV.Put(kvHeartBeatTtl).Response;
            KVPair kvTimeout = new KVPair(OrekService.Config.ConfigPrefix + "timeout") { Value = Encoding.UTF8.GetBytes(tbTimeOut.Text) };
            result = result && OrekService.ConsulClient.KV.Put(kvTimeout).Response;
            if (result)
            {
                toolStripStatusLabel1.Text = "Succesfully saved initial config";
                btnSaveTimeOut.Enabled = false;
                btnCancelTimeOut.Enabled = false;
                btnSveHeartBeatTTL.Enabled = false;
                btnCancelHeartBeatTTL.Enabled = false;
                btnCreateOrekConsulConfig.Enabled = false;
            }
            else { toolStripStatusLabel1.Text = "Save of initial config failed"; }
        }

        private void btnSveHeartBeatTTL_Click(object sender, EventArgs e)
        {
            KVPair kvHeartBeatTtl = new KVPair(OrekService.Config.ConfigPrefix + "heartbeatttl") { Value = Encoding.UTF8.GetBytes(tbTimeOut.Text) };
            bool result=OrekService.ConsulClient.KV.Put(kvHeartBeatTtl).Response;
            if (result)
            {
                toolStripStatusLabel1.Text = "Succesfully saved HeartBeatTTL";
                btnSveHeartBeatTTL.Enabled = false;
                btnCancelHeartBeatTTL.Enabled = false;
            }
            else { toolStripStatusLabel1.Text = "Save of HeartBeatTTL failed"; }
        }

        private void btnCancelHeartBeatTTL_Click(object sender, EventArgs e)
        {
            tbHeatBeatTTL.Text = OrekService.Config.HeartBeatTtl.ToString();
            btnSveHeartBeatTTL.Enabled = false;
            btnCancelHeartBeatTTL.Enabled = false;
            toolStripStatusLabel1.Text = "HeartBeatTTL value restored.";
        }

        private void btnSaveTimeOut_Click(object sender, EventArgs e)
        {
            KVPair kvTimeout = new KVPair(OrekService.Config.ConfigPrefix + "timeout") { Value = Encoding.UTF8.GetBytes(tbTimeOut.Text) };
            bool result = OrekService.ConsulClient.KV.Put(kvTimeout).Response;
            if (result)
            {
                toolStripStatusLabel1.Text = "Succesfully saved TimeOut";
                btnSaveTimeOut.Enabled = false;
                btnCancelTimeOut.Enabled = false;
            }
            else { toolStripStatusLabel1.Text = "Save of TimeOut failed"; }
        }

        private void btnCancelTimeOut_Click(object sender, EventArgs e)
        {
            tbTimeOut.Text = OrekService.Config.TimeOut.ToString();
            btnSaveTimeOut.Enabled = false;
            btnCancelTimeOut.Enabled = false;
            toolStripStatusLabel1.Text = "TimeOut value restored.";
        }

        private void tbHeatBeatTTL_TextChanged(object sender, EventArgs e)
        {
            try
            {
                int validInt = Convert.ToInt32(tbHeatBeatTTL.Text);
                btnSveHeartBeatTTL.Enabled = true;
                btnCancelHeartBeatTTL.Enabled = true;
                toolStripStatusLabel1.Text = "HeartBeatTTL has changed but not yet saved";
            }
            catch
            {
                toolStripStatusLabel1.Text = "Invalid value for HeartBeatTTL";
                btnSveHeartBeatTTL.Enabled = false;
                btnCancelHeartBeatTTL.Enabled = true;
            }
        }

        private void tbTimeOut_TextChanged(object sender, EventArgs e)
        {
            try
            {
                int validInt = Convert.ToInt32(tbTimeOut.Text);
                btnSaveTimeOut.Enabled = true;
                btnCancelTimeOut.Enabled = true;
                toolStripStatusLabel1.Text = "TimeOut has changed but not yet saved";
            }
            catch
            {
                toolStripStatusLabel1.Text = "Invalid value for TimeOut";
                btnSaveTimeOut.Enabled = false;
                btnCancelTimeOut.Enabled = true;
            }
        }

        private void btnReloadConfig_Click(object sender, EventArgs e)
        {
            OrekService.Config.ValidOnlineConfig=OrekService.Config.GetOnlineConfiguration();
            LoadConfig();
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            var control = sender as TabControl;
            if (control != null)
                switch (control.SelectedTab.Text) 
                {
                    case "Configuration":
                        LoadConfig();
                        break;
                    case "Clusters":
                        LoadClusters();
                        break;
                    case "Node Assignment":
                        break;
                }
        }

        private void tabControl_Deselecting(dynamic sender, TabControlCancelEventArgs e)
        {
            toolStripStatusLabel1.Text = "Switching Tab";
            dynamic control = sender as TabControl;
            if (control != null)
            {
                int currentindex = sender.SelectedIndex;
                var current = tabControl.TabPages[currentindex];
                bool go_ahead = true;
                switch (current.Text)
                {
                    case "Configuration":
                        go_ahead = ConfirmIfConfigChanged();
                        break;
                    case "Clusters":
                        break;
                    case "Node Assignment":
                        break;
                }
                if (!go_ahead) toolStripStatusLabel1.Text = "Switching Tab Cancelled";
                e.Cancel = !go_ahead;
            }
            
        }

        private bool ConfirmIfConfigChanged()
        {
            bool savepending = btnSveHeartBeatTTL.Enabled || btnSaveTimeOut.Enabled;
            if (!savepending) return true;
            var confirmResult = MessageBox.Show("There are unsaved changes, are you sure you want to leave this tab?",
                                     "Confirm Tab Change",
                                     MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                return true;
            }
            return false;                       
        }

        private void btnPushClusterConfig_Click(object sender, EventArgs e)
        {
            KVPair kvClusters = new KVPair(OrekService.Config.ClustersPrefix) { Value = Encoding.UTF8.GetBytes("")};
            bool result = OrekService.ConsulClient.KV.Put(kvClusters).Response;
            if (result)
            {
                toolStripStatusLabel1.Text = "Succesfully saved initial config";
                btnPushClusterConfig.Enabled = false;
                btnReloadClusterConfig.Enabled = true;
            }
            else { toolStripStatusLabel1.Text = "Save of initial config failed"; }
        }

        private void btnReloadClusterConfig_Click(object sender, EventArgs e)
        {
            LoadClusters();
        }

        private void listboxClusters_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listboxClusters.SelectedItem.ToString() == "<New Cluster...>")
            {
                NewClusterSettings();
                btnSaveCluster.Enabled = ValidateClusterSettings();
                btnCancelCluster.Enabled = false;
            }
            else
            {
                GetClusterSettings(listboxClusters.SelectedItem.ToString());
                btnSaveCluster.Enabled = false;
                btnCancelCluster.Enabled = false;
            }
        }

        private bool GetClusterSettings(string clustername)
        {
            var qr = OrekService.ConsulClient.KV.List(OrekService.Config.ClustersPrefix+clustername);
            if ((qr != null) && (qr.Response != null))
            {
                tbClusterName.Text = Encoding.UTF8.GetString(qr.Response.First(f => f.Key.EndsWith("/name")).Value);
                tbWindowsServiceName.Text = Encoding.UTF8.GetString(qr.Response.First(f => f.Key.EndsWith("/windowsservice")).Value);
                tbLimit.Text = Encoding.UTF8.GetString(qr.Response.First(f => f.Key.EndsWith("/limit")).Value);
                tbHeartBeatTtl.Text = Encoding.UTF8.GetString(qr.Response.First(f => f.Key.EndsWith("/heartbeatttl")).Value);
                tbStartTimeout.Text = Encoding.UTF8.GetString(qr.Response.First(f => f.Key.EndsWith("/starttimeout")).Value);
                tbStopTimeout.Text = Encoding.UTF8.GetString(qr.Response.First(f => f.Key.EndsWith("/stoptimeout")).Value);
                return true;
            }            
            return false;
        }

        private bool SetClusterSettings(string clustername)
        {
            return true;
        }

        private void NewClusterSettings()
        {
            tbClusterName.Text = "<Enter the name of the new cluster>";
            tbWindowsServiceName.Text = "<Enter the name of the windows service to manage>";
            tbLimit.Text = "<Enter the number of active nodes desired in the cluster>";
            tbHeartBeatTtl.Text = "<Enter the heartbeat timeout (ms)>";
            tbStartTimeout.Text = "<Enter the start timeout (ms)>";
            tbStopTimeout.Text = "<Enter the stop timeout (ms)>";
        }

        private bool ValidateClusterSettings()
        {
            bool valid=ClusterNameIsValid();
            if (!valid) return false;
            toolStripStatusLabel1.Text = "Cluster Settings are valid, press Save or Cancel to restore previous values";
            return true;
        }

        private bool ClusterNameIsValid()
        {
            if (tbClusterName.Text.All(char.IsLetterOrDigit)) return true;            
            toolStripStatusLabel1.Text = "Invalid cluster name, use only alphanumeric characters";
            return false;
        }

        private void ClusterSettings_Changed(object sender, EventArgs e)
        {
            btnSaveCluster.Enabled = ValidateClusterSettings();
            btnCancelCluster.Enabled = true;
        }

        private void btnSaveCluster_Click(object sender, EventArgs e)
        {
            var clusterpath = OrekService.Config.ClustersPrefix + tbClusterName.Text + "/";
            KVPair kvCluster = new KVPair(clusterpath) { Value = Encoding.UTF8.GetBytes("") };
            bool result = OrekService.ConsulClient.KV.Put(kvCluster).Response;
            KVPair kvName = new KVPair(clusterpath + "name") { Value = Encoding.UTF8.GetBytes(tbClusterName.Text) };
            result = result && OrekService.ConsulClient.KV.Put(kvName).Response;
            KVPair kvWindowsServiceName = new KVPair(clusterpath + "windowsservice") { Value = Encoding.UTF8.GetBytes(tbWindowsServiceName.Text) };
            result = result && OrekService.ConsulClient.KV.Put(kvWindowsServiceName).Response;
            KVPair kvLimit = new KVPair(clusterpath + "limit") { Value = Encoding.UTF8.GetBytes(tbLimit.Text) };
            result = result && OrekService.ConsulClient.KV.Put(kvLimit).Response;
            KVPair kvHeartBeatTtl = new KVPair(clusterpath + "heartbeatttl") { Value = Encoding.UTF8.GetBytes(tbHeartBeatTtl.Text) };
            result = result && OrekService.ConsulClient.KV.Put(kvHeartBeatTtl).Response;
            KVPair kvStartTimeout = new KVPair(clusterpath + "starttimeout") { Value = Encoding.UTF8.GetBytes(tbStartTimeout.Text) };
            result = result && OrekService.ConsulClient.KV.Put(kvStartTimeout).Response;
            KVPair kvStopTimeout = new KVPair(clusterpath + "stoptimeout") { Value = Encoding.UTF8.GetBytes(tbStopTimeout.Text) };
            result = result && OrekService.ConsulClient.KV.Put(kvStopTimeout).Response;
            if (result)
            {
                toolStripStatusLabel1.Text = "Succesfully saved initial config";
                btnSaveCluster.Enabled = false;
                btnCancelCluster.Enabled = false;
                LoadClusters();
            }
            else { toolStripStatusLabel1.Text = "Save of initial config failed"; }
        }

        private void btnCancelCluster_Click(object sender, EventArgs e)
        {
            GetClusterSettings(listboxClusters.SelectedItem.ToString());
            btnSaveCluster.Enabled = false;
            btnCancelCluster.Enabled = false;
        }

    }
}
