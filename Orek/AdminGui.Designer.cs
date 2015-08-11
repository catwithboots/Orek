namespace Orek
{
    partial class AdminGui
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabConfig = new System.Windows.Forms.TabPage();
            this.btnReloadConfig = new System.Windows.Forms.Button();
            this.btnCancelTimeOut = new System.Windows.Forms.Button();
            this.btnSaveTimeOut = new System.Windows.Forms.Button();
            this.btnCancelHeartBeatTTL = new System.Windows.Forms.Button();
            this.btnSveHeartBeatTTL = new System.Windows.Forms.Button();
            this.lblTimeOut = new System.Windows.Forms.Label();
            this.tbTimeOut = new System.Windows.Forms.TextBox();
            this.tbHeatBeatTTL = new System.Windows.Forms.TextBox();
            this.lblHeartBeatTTL = new System.Windows.Forms.Label();
            this.btnCreateOrekConsulConfig = new System.Windows.Forms.Button();
            this.lblOrekConsulConfigStatus = new System.Windows.Forms.Label();
            this.tabClusters = new System.Windows.Forms.TabPage();
            this.btnCancelCluster = new System.Windows.Forms.Button();
            this.btnSaveCluster = new System.Windows.Forms.Button();
            this.lblClusterStopTimeout = new System.Windows.Forms.Label();
            this.lblClusterStartTimeout = new System.Windows.Forms.Label();
            this.lblClusterLimit = new System.Windows.Forms.Label();
            this.lblClusterHbTtl = new System.Windows.Forms.Label();
            this.lblClusterWinSrv = new System.Windows.Forms.Label();
            this.tbStopTimeout = new System.Windows.Forms.TextBox();
            this.tbStartTimeout = new System.Windows.Forms.TextBox();
            this.tbLimit = new System.Windows.Forms.TextBox();
            this.tbHeartBeatTtl = new System.Windows.Forms.TextBox();
            this.tbWindowsServiceName = new System.Windows.Forms.TextBox();
            this.tbClusterName = new System.Windows.Forms.TextBox();
            this.lblClusterName = new System.Windows.Forms.Label();
            this.listboxClusters = new System.Windows.Forms.ListBox();
            this.btnReloadClusterConfig = new System.Windows.Forms.Button();
            this.btnPushClusterConfig = new System.Windows.Forms.Button();
            this.lblClusterConfigStatus = new System.Windows.Forms.Label();
            this.tabNodeAssignment = new System.Windows.Forms.TabPage();
            this.statusStrip1.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabConfig.SuspendLayout();
            this.tabClusters.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 533);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(932, 22);
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(118, 17);
            this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabConfig);
            this.tabControl.Controls.Add(this.tabClusters);
            this.tabControl.Controls.Add(this.tabNodeAssignment);
            this.tabControl.Location = new System.Drawing.Point(0, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(932, 518);
            this.tabControl.TabIndex = 1;
            this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
            this.tabControl.Deselecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabControl_Deselecting);
            // 
            // tabConfig
            // 
            this.tabConfig.Controls.Add(this.btnReloadConfig);
            this.tabConfig.Controls.Add(this.btnCancelTimeOut);
            this.tabConfig.Controls.Add(this.btnSaveTimeOut);
            this.tabConfig.Controls.Add(this.btnCancelHeartBeatTTL);
            this.tabConfig.Controls.Add(this.btnSveHeartBeatTTL);
            this.tabConfig.Controls.Add(this.lblTimeOut);
            this.tabConfig.Controls.Add(this.tbTimeOut);
            this.tabConfig.Controls.Add(this.tbHeatBeatTTL);
            this.tabConfig.Controls.Add(this.lblHeartBeatTTL);
            this.tabConfig.Controls.Add(this.btnCreateOrekConsulConfig);
            this.tabConfig.Controls.Add(this.lblOrekConsulConfigStatus);
            this.tabConfig.Location = new System.Drawing.Point(4, 22);
            this.tabConfig.Name = "tabConfig";
            this.tabConfig.Padding = new System.Windows.Forms.Padding(3);
            this.tabConfig.Size = new System.Drawing.Size(924, 492);
            this.tabConfig.TabIndex = 0;
            this.tabConfig.Text = "Configuration";
            this.tabConfig.UseVisualStyleBackColor = true;
            // 
            // btnReloadConfig
            // 
            this.btnReloadConfig.Location = new System.Drawing.Point(413, 6);
            this.btnReloadConfig.Name = "btnReloadConfig";
            this.btnReloadConfig.Size = new System.Drawing.Size(156, 23);
            this.btnReloadConfig.TabIndex = 10;
            this.btnReloadConfig.Text = "Reload Config";
            this.btnReloadConfig.UseVisualStyleBackColor = true;
            this.btnReloadConfig.Click += new System.EventHandler(this.btnReloadConfig_Click);
            // 
            // btnCancelTimeOut
            // 
            this.btnCancelTimeOut.Enabled = false;
            this.btnCancelTimeOut.Location = new System.Drawing.Point(332, 83);
            this.btnCancelTimeOut.Name = "btnCancelTimeOut";
            this.btnCancelTimeOut.Size = new System.Drawing.Size(75, 23);
            this.btnCancelTimeOut.TabIndex = 9;
            this.btnCancelTimeOut.Text = "Cancel";
            this.btnCancelTimeOut.UseVisualStyleBackColor = true;
            this.btnCancelTimeOut.Click += new System.EventHandler(this.btnCancelTimeOut_Click);
            // 
            // btnSaveTimeOut
            // 
            this.btnSaveTimeOut.Enabled = false;
            this.btnSaveTimeOut.Location = new System.Drawing.Point(251, 83);
            this.btnSaveTimeOut.Name = "btnSaveTimeOut";
            this.btnSaveTimeOut.Size = new System.Drawing.Size(75, 23);
            this.btnSaveTimeOut.TabIndex = 8;
            this.btnSaveTimeOut.Text = "Save";
            this.btnSaveTimeOut.UseVisualStyleBackColor = true;
            this.btnSaveTimeOut.Click += new System.EventHandler(this.btnSaveTimeOut_Click);
            // 
            // btnCancelHeartBeatTTL
            // 
            this.btnCancelHeartBeatTTL.Enabled = false;
            this.btnCancelHeartBeatTTL.Location = new System.Drawing.Point(332, 57);
            this.btnCancelHeartBeatTTL.Name = "btnCancelHeartBeatTTL";
            this.btnCancelHeartBeatTTL.Size = new System.Drawing.Size(75, 23);
            this.btnCancelHeartBeatTTL.TabIndex = 7;
            this.btnCancelHeartBeatTTL.Text = "Cancel";
            this.btnCancelHeartBeatTTL.UseVisualStyleBackColor = true;
            this.btnCancelHeartBeatTTL.Click += new System.EventHandler(this.btnCancelHeartBeatTTL_Click);
            // 
            // btnSveHeartBeatTTL
            // 
            this.btnSveHeartBeatTTL.Enabled = false;
            this.btnSveHeartBeatTTL.Location = new System.Drawing.Point(251, 57);
            this.btnSveHeartBeatTTL.Name = "btnSveHeartBeatTTL";
            this.btnSveHeartBeatTTL.Size = new System.Drawing.Size(75, 23);
            this.btnSveHeartBeatTTL.TabIndex = 6;
            this.btnSveHeartBeatTTL.Text = "Save";
            this.btnSveHeartBeatTTL.UseVisualStyleBackColor = true;
            this.btnSveHeartBeatTTL.Click += new System.EventHandler(this.btnSveHeartBeatTTL_Click);
            // 
            // lblTimeOut
            // 
            this.lblTimeOut.AutoSize = true;
            this.lblTimeOut.Location = new System.Drawing.Point(12, 88);
            this.lblTimeOut.Name = "lblTimeOut";
            this.lblTimeOut.Size = new System.Drawing.Size(47, 13);
            this.lblTimeOut.TabIndex = 5;
            this.lblTimeOut.Text = "TimeOut";
            // 
            // tbTimeOut
            // 
            this.tbTimeOut.Location = new System.Drawing.Point(145, 85);
            this.tbTimeOut.Name = "tbTimeOut";
            this.tbTimeOut.Size = new System.Drawing.Size(100, 20);
            this.tbTimeOut.TabIndex = 4;
            this.tbTimeOut.TextChanged += new System.EventHandler(this.tbTimeOut_TextChanged);
            // 
            // tbHeatBeatTTL
            // 
            this.tbHeatBeatTTL.Location = new System.Drawing.Point(145, 58);
            this.tbHeatBeatTTL.Name = "tbHeatBeatTTL";
            this.tbHeatBeatTTL.Size = new System.Drawing.Size(100, 20);
            this.tbHeatBeatTTL.TabIndex = 3;
            this.tbHeatBeatTTL.TextChanged += new System.EventHandler(this.tbHeatBeatTTL_TextChanged);
            // 
            // lblHeartBeatTTL
            // 
            this.lblHeartBeatTTL.AutoSize = true;
            this.lblHeartBeatTTL.Location = new System.Drawing.Point(12, 62);
            this.lblHeartBeatTTL.Name = "lblHeartBeatTTL";
            this.lblHeartBeatTTL.Size = new System.Drawing.Size(78, 13);
            this.lblHeartBeatTTL.TabIndex = 2;
            this.lblHeartBeatTTL.Text = "HeartBeat TTL";
            // 
            // btnCreateOrekConsulConfig
            // 
            this.btnCreateOrekConsulConfig.Enabled = false;
            this.btnCreateOrekConsulConfig.Location = new System.Drawing.Point(251, 6);
            this.btnCreateOrekConsulConfig.Name = "btnCreateOrekConsulConfig";
            this.btnCreateOrekConsulConfig.Size = new System.Drawing.Size(156, 23);
            this.btnCreateOrekConsulConfig.TabIndex = 1;
            this.btnCreateOrekConsulConfig.Text = "Push Config";
            this.btnCreateOrekConsulConfig.UseVisualStyleBackColor = true;
            this.btnCreateOrekConsulConfig.Click += new System.EventHandler(this.btnCreateOrekConsulConfig_Click);
            // 
            // lblOrekConsulConfigStatus
            // 
            this.lblOrekConsulConfigStatus.AutoSize = true;
            this.lblOrekConsulConfigStatus.Location = new System.Drawing.Point(12, 12);
            this.lblOrekConsulConfigStatus.Name = "lblOrekConsulConfigStatus";
            this.lblOrekConsulConfigStatus.Size = new System.Drawing.Size(162, 13);
            this.lblOrekConsulConfigStatus.TabIndex = 0;
            this.lblOrekConsulConfigStatus.Text = "No OREK config found in Consul";
            // 
            // tabClusters
            // 
            this.tabClusters.Controls.Add(this.btnCancelCluster);
            this.tabClusters.Controls.Add(this.btnSaveCluster);
            this.tabClusters.Controls.Add(this.lblClusterStopTimeout);
            this.tabClusters.Controls.Add(this.lblClusterStartTimeout);
            this.tabClusters.Controls.Add(this.lblClusterLimit);
            this.tabClusters.Controls.Add(this.lblClusterHbTtl);
            this.tabClusters.Controls.Add(this.lblClusterWinSrv);
            this.tabClusters.Controls.Add(this.tbStopTimeout);
            this.tabClusters.Controls.Add(this.tbStartTimeout);
            this.tabClusters.Controls.Add(this.tbLimit);
            this.tabClusters.Controls.Add(this.tbHeartBeatTtl);
            this.tabClusters.Controls.Add(this.tbWindowsServiceName);
            this.tabClusters.Controls.Add(this.tbClusterName);
            this.tabClusters.Controls.Add(this.lblClusterName);
            this.tabClusters.Controls.Add(this.listboxClusters);
            this.tabClusters.Controls.Add(this.btnReloadClusterConfig);
            this.tabClusters.Controls.Add(this.btnPushClusterConfig);
            this.tabClusters.Controls.Add(this.lblClusterConfigStatus);
            this.tabClusters.Location = new System.Drawing.Point(4, 22);
            this.tabClusters.Name = "tabClusters";
            this.tabClusters.Padding = new System.Windows.Forms.Padding(3);
            this.tabClusters.Size = new System.Drawing.Size(924, 492);
            this.tabClusters.TabIndex = 1;
            this.tabClusters.Text = "Clusters";
            this.tabClusters.UseVisualStyleBackColor = true;
            // 
            // btnCancelCluster
            // 
            this.btnCancelCluster.Enabled = false;
            this.btnCancelCluster.Location = new System.Drawing.Point(373, 213);
            this.btnCancelCluster.Name = "btnCancelCluster";
            this.btnCancelCluster.Size = new System.Drawing.Size(75, 23);
            this.btnCancelCluster.TabIndex = 17;
            this.btnCancelCluster.Text = "Cancel";
            this.btnCancelCluster.UseVisualStyleBackColor = true;
            this.btnCancelCluster.Click += new System.EventHandler(this.btnCancelCluster_Click);
            // 
            // btnSaveCluster
            // 
            this.btnSaveCluster.Enabled = false;
            this.btnSaveCluster.Location = new System.Drawing.Point(291, 213);
            this.btnSaveCluster.Name = "btnSaveCluster";
            this.btnSaveCluster.Size = new System.Drawing.Size(75, 23);
            this.btnSaveCluster.TabIndex = 16;
            this.btnSaveCluster.Text = "Save";
            this.btnSaveCluster.UseVisualStyleBackColor = true;
            this.btnSaveCluster.Click += new System.EventHandler(this.btnSaveCluster_Click);
            // 
            // lblClusterStopTimeout
            // 
            this.lblClusterStopTimeout.AutoSize = true;
            this.lblClusterStopTimeout.Location = new System.Drawing.Point(152, 189);
            this.lblClusterStopTimeout.Name = "lblClusterStopTimeout";
            this.lblClusterStopTimeout.Size = new System.Drawing.Size(92, 13);
            this.lblClusterStopTimeout.TabIndex = 15;
            this.lblClusterStopTimeout.Text = "Stop Timeout (ms)";
            // 
            // lblClusterStartTimeout
            // 
            this.lblClusterStartTimeout.AutoSize = true;
            this.lblClusterStartTimeout.Location = new System.Drawing.Point(152, 162);
            this.lblClusterStartTimeout.Name = "lblClusterStartTimeout";
            this.lblClusterStartTimeout.Size = new System.Drawing.Size(92, 13);
            this.lblClusterStartTimeout.TabIndex = 14;
            this.lblClusterStartTimeout.Text = "Start Timeout (ms)";
            // 
            // lblClusterLimit
            // 
            this.lblClusterLimit.AutoSize = true;
            this.lblClusterLimit.Location = new System.Drawing.Point(152, 135);
            this.lblClusterLimit.Name = "lblClusterLimit";
            this.lblClusterLimit.Size = new System.Drawing.Size(28, 13);
            this.lblClusterLimit.TabIndex = 13;
            this.lblClusterLimit.Text = "Limit";
            // 
            // lblClusterHbTtl
            // 
            this.lblClusterHbTtl.AutoSize = true;
            this.lblClusterHbTtl.Location = new System.Drawing.Point(152, 108);
            this.lblClusterHbTtl.Name = "lblClusterHbTtl";
            this.lblClusterHbTtl.Size = new System.Drawing.Size(100, 13);
            this.lblClusterHbTtl.TabIndex = 12;
            this.lblClusterHbTtl.Text = "HeartBeat TTL (ms)";
            // 
            // lblClusterWinSrv
            // 
            this.lblClusterWinSrv.AutoSize = true;
            this.lblClusterWinSrv.Location = new System.Drawing.Point(152, 81);
            this.lblClusterWinSrv.Name = "lblClusterWinSrv";
            this.lblClusterWinSrv.Size = new System.Drawing.Size(90, 13);
            this.lblClusterWinSrv.TabIndex = 11;
            this.lblClusterWinSrv.Text = "Windows Service";
            // 
            // tbStopTimeout
            // 
            this.tbStopTimeout.Location = new System.Drawing.Point(291, 186);
            this.tbStopTimeout.Name = "tbStopTimeout";
            this.tbStopTimeout.Size = new System.Drawing.Size(278, 20);
            this.tbStopTimeout.TabIndex = 10;
            this.tbStopTimeout.TextChanged += new System.EventHandler(this.ClusterSettings_Changed);
            // 
            // tbStartTimeout
            // 
            this.tbStartTimeout.Location = new System.Drawing.Point(291, 159);
            this.tbStartTimeout.Name = "tbStartTimeout";
            this.tbStartTimeout.Size = new System.Drawing.Size(278, 20);
            this.tbStartTimeout.TabIndex = 9;
            this.tbStartTimeout.TextChanged += new System.EventHandler(this.ClusterSettings_Changed);
            // 
            // tbLimit
            // 
            this.tbLimit.Location = new System.Drawing.Point(291, 132);
            this.tbLimit.Name = "tbLimit";
            this.tbLimit.Size = new System.Drawing.Size(278, 20);
            this.tbLimit.TabIndex = 8;
            this.tbLimit.TextChanged += new System.EventHandler(this.ClusterSettings_Changed);
            // 
            // tbHeartBeatTtl
            // 
            this.tbHeartBeatTtl.Location = new System.Drawing.Point(291, 105);
            this.tbHeartBeatTtl.Name = "tbHeartBeatTtl";
            this.tbHeartBeatTtl.Size = new System.Drawing.Size(278, 20);
            this.tbHeartBeatTtl.TabIndex = 7;
            this.tbHeartBeatTtl.TextChanged += new System.EventHandler(this.ClusterSettings_Changed);
            // 
            // tbWindowsServiceName
            // 
            this.tbWindowsServiceName.Location = new System.Drawing.Point(291, 78);
            this.tbWindowsServiceName.Name = "tbWindowsServiceName";
            this.tbWindowsServiceName.Size = new System.Drawing.Size(278, 20);
            this.tbWindowsServiceName.TabIndex = 6;
            this.tbWindowsServiceName.TextChanged += new System.EventHandler(this.ClusterSettings_Changed);
            // 
            // tbClusterName
            // 
            this.tbClusterName.Location = new System.Drawing.Point(291, 51);
            this.tbClusterName.Name = "tbClusterName";
            this.tbClusterName.Size = new System.Drawing.Size(278, 20);
            this.tbClusterName.TabIndex = 5;
            this.tbClusterName.TextChanged += new System.EventHandler(this.ClusterSettings_Changed);
            // 
            // lblClusterName
            // 
            this.lblClusterName.AutoSize = true;
            this.lblClusterName.Location = new System.Drawing.Point(152, 54);
            this.lblClusterName.Name = "lblClusterName";
            this.lblClusterName.Size = new System.Drawing.Size(35, 13);
            this.lblClusterName.TabIndex = 4;
            this.lblClusterName.Text = "Name";
            // 
            // listboxClusters
            // 
            this.listboxClusters.FormattingEnabled = true;
            this.listboxClusters.HorizontalScrollbar = true;
            this.listboxClusters.Items.AddRange(new object[] {
            "<New Cluster...>"});
            this.listboxClusters.Location = new System.Drawing.Point(15, 51);
            this.listboxClusters.Name = "listboxClusters";
            this.listboxClusters.Size = new System.Drawing.Size(131, 420);
            this.listboxClusters.TabIndex = 3;
            this.listboxClusters.SelectedIndexChanged += new System.EventHandler(this.listboxClusters_SelectedIndexChanged);
            // 
            // btnReloadClusterConfig
            // 
            this.btnReloadClusterConfig.Location = new System.Drawing.Point(453, 6);
            this.btnReloadClusterConfig.Name = "btnReloadClusterConfig";
            this.btnReloadClusterConfig.Size = new System.Drawing.Size(156, 23);
            this.btnReloadClusterConfig.TabIndex = 2;
            this.btnReloadClusterConfig.Text = "Reload Config";
            this.btnReloadClusterConfig.UseVisualStyleBackColor = true;
            this.btnReloadClusterConfig.Click += new System.EventHandler(this.btnReloadClusterConfig_Click);
            // 
            // btnPushClusterConfig
            // 
            this.btnPushClusterConfig.Location = new System.Drawing.Point(291, 6);
            this.btnPushClusterConfig.Name = "btnPushClusterConfig";
            this.btnPushClusterConfig.Size = new System.Drawing.Size(156, 23);
            this.btnPushClusterConfig.TabIndex = 1;
            this.btnPushClusterConfig.Text = "Push Config";
            this.btnPushClusterConfig.UseVisualStyleBackColor = true;
            this.btnPushClusterConfig.Click += new System.EventHandler(this.btnPushClusterConfig_Click);
            // 
            // lblClusterConfigStatus
            // 
            this.lblClusterConfigStatus.AutoSize = true;
            this.lblClusterConfigStatus.Location = new System.Drawing.Point(12, 12);
            this.lblClusterConfigStatus.Name = "lblClusterConfigStatus";
            this.lblClusterConfigStatus.Size = new System.Drawing.Size(168, 13);
            this.lblClusterConfigStatus.TabIndex = 0;
            this.lblClusterConfigStatus.Text = "No clusters config found in Consul";
            // 
            // tabNodeAssignment
            // 
            this.tabNodeAssignment.Location = new System.Drawing.Point(4, 22);
            this.tabNodeAssignment.Name = "tabNodeAssignment";
            this.tabNodeAssignment.Size = new System.Drawing.Size(924, 492);
            this.tabNodeAssignment.TabIndex = 2;
            this.tabNodeAssignment.Text = "Node Assignment";
            this.tabNodeAssignment.UseVisualStyleBackColor = true;
            // 
            // AdminGui
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(932, 555);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.statusStrip1);
            this.Name = "AdminGui";
            this.Text = "AdminGui";
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tabControl.ResumeLayout(false);
            this.tabConfig.ResumeLayout(false);
            this.tabConfig.PerformLayout();
            this.tabClusters.ResumeLayout(false);
            this.tabClusters.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabConfig;
        private System.Windows.Forms.TabPage tabClusters;
        private System.Windows.Forms.Button btnCancelTimeOut;
        private System.Windows.Forms.Button btnSaveTimeOut;
        private System.Windows.Forms.Button btnCancelHeartBeatTTL;
        private System.Windows.Forms.Button btnSveHeartBeatTTL;
        private System.Windows.Forms.Label lblTimeOut;
        private System.Windows.Forms.TextBox tbTimeOut;
        private System.Windows.Forms.TextBox tbHeatBeatTTL;
        private System.Windows.Forms.Label lblHeartBeatTTL;
        private System.Windows.Forms.Button btnCreateOrekConsulConfig;
        private System.Windows.Forms.Label lblOrekConsulConfigStatus;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.Button btnReloadConfig;
        private System.Windows.Forms.TabPage tabNodeAssignment;
        private System.Windows.Forms.Label lblClusterConfigStatus;
        private System.Windows.Forms.Button btnReloadClusterConfig;
        private System.Windows.Forms.Button btnPushClusterConfig;
        private System.Windows.Forms.Label lblClusterStopTimeout;
        private System.Windows.Forms.Label lblClusterStartTimeout;
        private System.Windows.Forms.Label lblClusterLimit;
        private System.Windows.Forms.Label lblClusterHbTtl;
        private System.Windows.Forms.Label lblClusterWinSrv;
        private System.Windows.Forms.TextBox tbStopTimeout;
        private System.Windows.Forms.TextBox tbStartTimeout;
        private System.Windows.Forms.TextBox tbLimit;
        private System.Windows.Forms.TextBox tbHeartBeatTtl;
        private System.Windows.Forms.TextBox tbWindowsServiceName;
        private System.Windows.Forms.TextBox tbClusterName;
        private System.Windows.Forms.Label lblClusterName;
        private System.Windows.Forms.ListBox listboxClusters;
        private System.Windows.Forms.Button btnCancelCluster;
        private System.Windows.Forms.Button btnSaveCluster;
    }
}