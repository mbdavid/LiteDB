namespace LiteDB.Studio.Forms
{
    partial class ConnectionForm
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
            this.components = new System.ComponentModel.Container();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radModeEmbedded = new System.Windows.Forms.RadioButton();
            this.btnOK = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.btnOpen = new System.Windows.Forms.Button();
            this.chkUTC = new System.Windows.Forms.CheckBox();
            this.txtLimitSize = new System.Windows.Forms.TextBox();
            this.txtInitialSize = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtFilename = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.chkUpgrade = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.txtTimeout = new System.Windows.Forms.TextBox();
            this.chkReadonly = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.radModeShared = new System.Windows.Forms.RadioButton();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radModeShared);
            this.groupBox1.Controls.Add(this.radModeEmbedded);
            this.groupBox1.Location = new System.Drawing.Point(12, 11);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(569, 60);
            this.groupBox1.TabIndex = 27;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Connection Mode";
            // 
            // radModeEmbedded
            // 
            this.radModeEmbedded.AutoSize = true;
            this.radModeEmbedded.Checked = true;
            this.radModeEmbedded.Location = new System.Drawing.Point(30, 25);
            this.radModeEmbedded.Name = "radModeEmbedded";
            this.radModeEmbedded.Size = new System.Drawing.Size(82, 19);
            this.radModeEmbedded.TabIndex = 9;
            this.radModeEmbedded.Text = "Embedded";
            this.toolTip.SetToolTip(this.radModeEmbedded, "Open database as embedded.\r\n- Support only this single connection\r\n- Support mult" +
        "ple threads in this connection");
            this.radModeEmbedded.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOK.Image = global::LiteDB.Studio.Properties.Resources.database_connect;
            this.btnOK.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOK.Location = new System.Drawing.Point(454, 318);
            this.btnOK.Name = "btnOK";
            this.btnOK.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.btnOK.Size = new System.Drawing.Size(127, 38);
            this.btnOK.TabIndex = 11;
            this.btnOK.Text = "Connect";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnConnect_Click);
            // 
            // btnOpen
            // 
            this.btnOpen.Image = global::LiteDB.Studio.Properties.Resources.folder_explore;
            this.btnOpen.Location = new System.Drawing.Point(527, 28);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(28, 24);
            this.btnOpen.TabIndex = 3;
            this.toolTip.SetToolTip(this.btnOpen, "Open existing datafile");
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.BtnOpen_Click);
            // 
            // chkUTC
            // 
            this.chkUTC.AutoSize = true;
            this.chkUTC.Location = new System.Drawing.Point(257, 53);
            this.chkUTC.Name = "chkUTC";
            this.chkUTC.Size = new System.Drawing.Size(75, 19);
            this.chkUTC.TabIndex = 26;
            this.chkUTC.Text = "UTC Date";
            this.toolTip.SetToolTip(this.chkUTC, "When deserialize BSON document from datafile, use UTC converstion (default is con" +
        "vert date into Local time)");
            this.chkUTC.UseVisualStyleBackColor = true;
            // 
            // txtLimitSize
            // 
            this.txtLimitSize.Location = new System.Drawing.Point(151, 109);
            this.txtLimitSize.Name = "txtLimitSize";
            this.txtLimitSize.Size = new System.Drawing.Size(70, 23);
            this.txtLimitSize.TabIndex = 25;
            this.txtLimitSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtInitialSize
            // 
            this.txtInitialSize.Location = new System.Drawing.Point(151, 80);
            this.txtInitialSize.Name = "txtInitialSize";
            this.txtInitialSize.Size = new System.Drawing.Size(70, 23);
            this.txtInitialSize.TabIndex = 24;
            this.txtInitialSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnOpen);
            this.groupBox2.Controls.Add(this.txtFilename);
            this.groupBox2.Location = new System.Drawing.Point(12, 81);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(569, 72);
            this.groupBox2.TabIndex = 28;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Filename";
            // 
            // txtFilename
            // 
            this.txtFilename.Location = new System.Drawing.Point(19, 28);
            this.txtFilename.Name = "txtFilename";
            this.txtFilename.Size = new System.Drawing.Size(502, 23);
            this.txtFilename.TabIndex = 2;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.chkUpgrade);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.txtPassword);
            this.groupBox3.Controls.Add(this.txtTimeout);
            this.groupBox3.Controls.Add(this.txtLimitSize);
            this.groupBox3.Controls.Add(this.txtInitialSize);
            this.groupBox3.Controls.Add(this.chkReadonly);
            this.groupBox3.Controls.Add(this.chkUTC);
            this.groupBox3.Controls.Add(this.label4);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Location = new System.Drawing.Point(12, 159);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(569, 147);
            this.groupBox3.TabIndex = 29;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Parameters";
            // 
            // chkUpgrade
            // 
            this.chkUpgrade.AutoSize = true;
            this.chkUpgrade.Checked = true;
            this.chkUpgrade.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkUpgrade.Location = new System.Drawing.Point(257, 111);
            this.chkUpgrade.Name = "chkUpgrade";
            this.chkUpgrade.Size = new System.Drawing.Size(213, 19);
            this.chkUpgrade.TabIndex = 33;
            this.chkUpgrade.Text = "Upgrade data file if version is earlier";
            this.chkUpgrade.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 15);
            this.label1.TabIndex = 32;
            this.label1.Text = "Password";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(151, 22);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(404, 23);
            this.txtPassword.TabIndex = 4;
            // 
            // txtTimeout
            // 
            this.txtTimeout.Location = new System.Drawing.Point(151, 51);
            this.txtTimeout.Name = "txtTimeout";
            this.txtTimeout.Size = new System.Drawing.Size(70, 23);
            this.txtTimeout.TabIndex = 23;
            this.txtTimeout.Text = "00:01:00";
            this.txtTimeout.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // chkReadonly
            // 
            this.chkReadonly.AutoSize = true;
            this.chkReadonly.Location = new System.Drawing.Point(257, 82);
            this.chkReadonly.Name = "chkReadonly";
            this.chkReadonly.Size = new System.Drawing.Size(78, 19);
            this.chkReadonly.TabIndex = 27;
            this.chkReadonly.Text = "Read only";
            this.chkReadonly.UseVisualStyleBackColor = true;
            this.chkReadonly.CheckedChanged += new System.EventHandler(this.chkReadonly_CheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 112);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(89, 15);
            this.label4.TabIndex = 31;
            this.label4.Text = "Limit Size (MB):";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 83);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(91, 15);
            this.label3.TabIndex = 30;
            this.label3.Text = "Initial Size (MB):";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(109, 15);
            this.label2.TabIndex = 29;
            this.label2.Text = "Timeout (seconds):";
            // 
            // radModeShared
            // 
            this.radModeShared.AutoSize = true;
            this.radModeShared.Location = new System.Drawing.Point(118, 25);
            this.radModeShared.Name = "radModeShared";
            this.radModeShared.Size = new System.Drawing.Size(61, 19);
            this.radModeShared.TabIndex = 10;
            this.radModeShared.Text = "Shared";
            this.toolTip.SetToolTip(this.radModeShared, "Open database as shared mode.\r\n- Open datafile cross processes (and threads)\r\n- G" +
        "reat to access data when running app\r\n- Slow connection (not recommened for prod" +
        "uction)");
            this.radModeShared.UseVisualStyleBackColor = true;
            // 
            // ConnectionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(596, 368);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnOK);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConnectionForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Connection Manager";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radModeEmbedded;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.TextBox txtFilename;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox txtTimeout;
        private System.Windows.Forms.TextBox txtLimitSize;
        private System.Windows.Forms.TextBox txtInitialSize;
        private System.Windows.Forms.CheckBox chkReadonly;
        private System.Windows.Forms.CheckBox chkUTC;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.CheckBox chkUpgrade;
        private System.Windows.Forms.RadioButton radModeShared;
    }
}