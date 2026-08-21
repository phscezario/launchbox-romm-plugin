namespace RommPlugin.UI.Forms
{
    partial class RommSettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.txtBaseUrl = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.btnBrowseRomsPath = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.labelTokenHint = new System.Windows.Forms.Label();
            this.txtClientApiToken = new System.Windows.Forms.TextBox();
            this.labelToken = new System.Windows.Forms.Label();
            this.txtRomsPath = new System.Windows.Forms.TextBox();
            this.keepLocalData = new System.Windows.Forms.CheckBox();
            this.saveLogs = new System.Windows.Forms.CheckBox();
            this.processPendingOnStartup = new System.Windows.Forms.CheckBox();
            this.forceFullResync = new System.Windows.Forms.CheckBox();
            this.publicScreenshots = new System.Windows.Forms.CheckBox();
            this.updateStatsOnLaunch = new System.Windows.Forms.CheckBox();
            this.lblLogRetention = new System.Windows.Forms.Label();
            this.nudLogRetention = new System.Windows.Forms.NumericUpDown();
            this.lblLogRetentionHint = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.cmbLanguage = new System.Windows.Forms.ComboBox();
            this.lblLanguage = new System.Windows.Forms.Label();
            this.isAdmin = new System.Windows.Forms.CheckBox();
            this.lblAutoSyncInterval = new System.Windows.Forms.Label();
            this.nudAutoSyncInterval = new System.Windows.Forms.NumericUpDown();
            this.lblAutoSyncIntervalHint = new System.Windows.Forms.Label();
            this.lblBehavior = new System.Windows.Forms.Label();
            this.btnCheckUpdates = new System.Windows.Forms.Button();
            this.btnAbout = new System.Windows.Forms.Button();
            this.lblSaveBatchSize = new System.Windows.Forms.Label();
            this.nudSaveBatchSize = new System.Windows.Forms.NumericUpDown();
            this.lblSaveBatchSizeHint = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudLogRetention)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudAutoSyncInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSaveBatchSize)).BeginInit();
            this.SuspendLayout();
            // 
            // txtBaseUrl
            // 
            this.txtBaseUrl.Location = new System.Drawing.Point(26, 134);
            this.txtBaseUrl.Name = "txtBaseUrl";
            this.txtBaseUrl.Size = new System.Drawing.Size(274, 20);
            this.txtBaseUrl.TabIndex = 0;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(522, 631);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 28);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(21, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(251, 29);
            this.label1.TabIndex = 4;
            this.label1.Text = "RomM Configuration";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(22, 112);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "Base URL:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(8, 18);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(73, 16);
            this.label3.TabIndex = 7;
            this.label3.Text = "Username:";
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(10, 38);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(246, 20);
            this.txtUsername.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(8, 63);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(70, 16);
            this.label4.TabIndex = 9;
            this.label4.Text = "Password:";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(10, 83);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(246, 20);
            this.txtPassword.TabIndex = 8;
            this.txtPassword.UseSystemPasswordChar = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(22, 392);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(76, 16);
            this.label5.TabIndex = 11;
            this.label5.Text = "Roms Path:";
            // 
            // btnBrowseRomsPath
            // 
            this.btnBrowseRomsPath.Location = new System.Drawing.Point(264, 414);
            this.btnBrowseRomsPath.Name = "btnBrowseRomsPath";
            this.btnBrowseRomsPath.Size = new System.Drawing.Size(36, 20);
            this.btnBrowseRomsPath.TabIndex = 12;
            this.btnBrowseRomsPath.Text = "...";
            this.btnBrowseRomsPath.UseVisualStyleBackColor = true;
            this.btnBrowseRomsPath.Click += new System.EventHandler(this.btnBrowseRomsPath_Click);
            // 
            // label6
            // 
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label6.ForeColor = System.Drawing.Color.Gainsboro;
            this.label6.Location = new System.Drawing.Point(8, 172);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(248, 38);
            this.label6.TabIndex = 13;
            this.label6.Text = "Credentials are saved in plain text. We recommend creating a unique user with \"vi" +
    "ewer\" role.";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.labelTokenHint);
            this.groupBox1.Controls.Add(this.txtClientApiToken);
            this.groupBox1.Controls.Add(this.labelToken);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.txtPassword);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtUsername);
            this.groupBox1.ForeColor = System.Drawing.Color.White;
            this.groupBox1.Location = new System.Drawing.Point(26, 164);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(274, 218);
            this.groupBox1.TabIndex = 14;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Login";
            // 
            // labelTokenHint
            // 
            this.labelTokenHint.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic);
            this.labelTokenHint.ForeColor = System.Drawing.Color.Gainsboro;
            this.labelTokenHint.Location = new System.Drawing.Point(8, 152);
            this.labelTokenHint.Name = "labelTokenHint";
            this.labelTokenHint.Size = new System.Drawing.Size(248, 16);
            this.labelTokenHint.TabIndex = 23;
            this.labelTokenHint.Text = "If set, token takes priority over username/password.";
            // 
            // txtClientApiToken
            // 
            this.txtClientApiToken.Location = new System.Drawing.Point(10, 128);
            this.txtClientApiToken.Name = "txtClientApiToken";
            this.txtClientApiToken.Size = new System.Drawing.Size(246, 20);
            this.txtClientApiToken.TabIndex = 22;
            this.txtClientApiToken.UseSystemPasswordChar = true;
            // 
            // labelToken
            // 
            this.labelToken.AutoSize = true;
            this.labelToken.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.labelToken.ForeColor = System.Drawing.Color.White;
            this.labelToken.Location = new System.Drawing.Point(8, 108);
            this.labelToken.Name = "labelToken";
            this.labelToken.Size = new System.Drawing.Size(162, 16);
            this.labelToken.TabIndex = 21;
            this.labelToken.Text = "Client API Token (rmm_...):";
            // 
            // txtRomsPath
            // 
            this.txtRomsPath.Location = new System.Drawing.Point(26, 414);
            this.txtRomsPath.Name = "txtRomsPath";
            this.txtRomsPath.Size = new System.Drawing.Size(234, 20);
            this.txtRomsPath.TabIndex = 15;
            // 
            // keepLocalData
            // 
            this.keepLocalData.AutoSize = true;
            this.keepLocalData.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.keepLocalData.ForeColor = System.Drawing.Color.White;
            this.keepLocalData.Location = new System.Drawing.Point(325, 85);
            this.keepLocalData.Margin = new System.Windows.Forms.Padding(2);
            this.keepLocalData.Name = "keepLocalData";
            this.keepLocalData.Size = new System.Drawing.Size(162, 19);
            this.keepLocalData.TabIndex = 17;
            this.keepLocalData.Text = "Keep Local data in sync?";
            this.keepLocalData.UseVisualStyleBackColor = true;
            // 
            // saveLogs
            // 
            this.saveLogs.AutoSize = true;
            this.saveLogs.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.saveLogs.ForeColor = System.Drawing.Color.White;
            this.saveLogs.Location = new System.Drawing.Point(325, 365);
            this.saveLogs.Margin = new System.Windows.Forms.Padding(2);
            this.saveLogs.Name = "saveLogs";
            this.saveLogs.Size = new System.Drawing.Size(118, 19);
            this.saveLogs.TabIndex = 19;
            this.saveLogs.Text = "Enable Debug Mode?";
            this.saveLogs.UseVisualStyleBackColor = true;
            // 
            // processPendingOnStartup
            // 
            this.processPendingOnStartup.AutoSize = true;
            this.processPendingOnStartup.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.processPendingOnStartup.ForeColor = System.Drawing.Color.White;
            this.processPendingOnStartup.Location = new System.Drawing.Point(325, 110);
            this.processPendingOnStartup.Margin = new System.Windows.Forms.Padding(2);
            this.processPendingOnStartup.Name = "processPendingOnStartup";
            this.processPendingOnStartup.Size = new System.Drawing.Size(223, 19);
            this.processPendingOnStartup.TabIndex = 20;
            this.processPendingOnStartup.Text = "Process pending installs on startup?";
            this.processPendingOnStartup.UseVisualStyleBackColor = true;
            // 
            // forceFullResync
            // 
            this.forceFullResync.AutoSize = true;
            this.forceFullResync.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.forceFullResync.ForeColor = System.Drawing.Color.Orange;
            this.forceFullResync.Location = new System.Drawing.Point(325, 135);
            this.forceFullResync.Margin = new System.Windows.Forms.Padding(2);
            this.forceFullResync.Name = "forceFullResync";
            this.forceFullResync.Size = new System.Drawing.Size(191, 19);
            this.forceFullResync.TabIndex = 27;
            this.forceFullResync.Text = "Force full resync on next sync?";
            this.forceFullResync.UseVisualStyleBackColor = true;
            // 
            // publicScreenshots
            // 
            this.publicScreenshots.AutoSize = true;
            this.publicScreenshots.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.publicScreenshots.ForeColor = System.Drawing.Color.White;
            this.publicScreenshots.Location = new System.Drawing.Point(325, 160);
            this.publicScreenshots.Margin = new System.Windows.Forms.Padding(2);
            this.publicScreenshots.Name = "publicScreenshots";
            this.publicScreenshots.Size = new System.Drawing.Size(169, 19);
            this.publicScreenshots.TabIndex = 28;
            this.publicScreenshots.Text = "Make screenshots public?";
            this.publicScreenshots.UseVisualStyleBackColor = true;
            // 
            // updateStatsOnLaunch
            // 
            this.updateStatsOnLaunch.AutoSize = true;
            this.updateStatsOnLaunch.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.updateStatsOnLaunch.ForeColor = System.Drawing.Color.White;
            this.updateStatsOnLaunch.Location = new System.Drawing.Point(325, 185);
            this.updateStatsOnLaunch.Margin = new System.Windows.Forms.Padding(2);
            this.updateStatsOnLaunch.Name = "updateStatsOnLaunch";
            this.updateStatsOnLaunch.Size = new System.Drawing.Size(215, 19);
            this.updateStatsOnLaunch.TabIndex = 29;
            this.updateStatsOnLaunch.Text = "Update stats on game launch/exit?";
            this.updateStatsOnLaunch.UseVisualStyleBackColor = true;
            // 
            // lblLogRetention
            // 
            this.lblLogRetention.AutoSize = true;
            this.lblLogRetention.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.lblLogRetention.ForeColor = System.Drawing.Color.White;
            this.lblLogRetention.Location = new System.Drawing.Point(322, 215);
            this.lblLogRetention.Name = "lblLogRetention";
            this.lblLogRetention.Size = new System.Drawing.Size(128, 16);
            this.lblLogRetention.TabIndex = 29;
            this.lblLogRetention.Text = "Log retention (days):";
            // 
            // nudLogRetention
            // 
            this.nudLogRetention.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.nudLogRetention.ForeColor = System.Drawing.Color.White;
            this.nudLogRetention.Location = new System.Drawing.Point(325, 237);
            this.nudLogRetention.Maximum = new decimal(new int[] {
            365,
            0,
            0,
            0});
            this.nudLogRetention.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudLogRetention.Name = "nudLogRetention";
            this.nudLogRetention.Size = new System.Drawing.Size(60, 20);
            this.nudLogRetention.TabIndex = 30;
            this.nudLogRetention.Value = new decimal(new int[] {
            7,
            0,
            0,
            0});
            // 
            // lblLogRetentionHint
            // 
            this.lblLogRetentionHint.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic);
            this.lblLogRetentionHint.ForeColor = System.Drawing.Color.Gainsboro;
            this.lblLogRetentionHint.Location = new System.Drawing.Point(322, 262);
            this.lblLogRetentionHint.Name = "lblLogRetentionHint";
            this.lblLogRetentionHint.Size = new System.Drawing.Size(274, 16);
            this.lblLogRetentionHint.TabIndex = 31;
            this.lblLogRetentionHint.Text = "Logs older than this will be automatically deleted.";
            // 
            // lblSaveBatchSize
            // 
            this.lblSaveBatchSize.AutoSize = true;
            this.lblSaveBatchSize.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.lblSaveBatchSize.ForeColor = System.Drawing.Color.White;
            this.lblSaveBatchSize.Location = new System.Drawing.Point(322, 285);
            this.lblSaveBatchSize.Name = "lblSaveBatchSize";
            this.lblSaveBatchSize.Size = new System.Drawing.Size(170, 16);
            this.lblSaveBatchSize.TabIndex = 40;
            this.lblSaveBatchSize.Text = "Save frequency (games per save):";
            // 
            // nudSaveBatchSize
            // 
            this.nudSaveBatchSize.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.nudSaveBatchSize.ForeColor = System.Drawing.Color.White;
            this.nudSaveBatchSize.Location = new System.Drawing.Point(325, 307);
            this.nudSaveBatchSize.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.nudSaveBatchSize.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudSaveBatchSize.Name = "nudSaveBatchSize";
            this.nudSaveBatchSize.Size = new System.Drawing.Size(60, 20);
            this.nudSaveBatchSize.TabIndex = 41;
            this.nudSaveBatchSize.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // lblSaveBatchSizeHint
            // 
            this.lblSaveBatchSizeHint.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic);
            this.lblSaveBatchSizeHint.ForeColor = System.Drawing.Color.Gainsboro;
            this.lblSaveBatchSizeHint.Location = new System.Drawing.Point(322, 332);
            this.lblSaveBatchSizeHint.Name = "lblSaveBatchSizeHint";
            this.lblSaveBatchSizeHint.Size = new System.Drawing.Size(274, 16);
            this.lblSaveBatchSizeHint.TabIndex = 42;
            this.lblSaveBatchSizeHint.Text = "Lower = safer on cancel, higher = faster sync. Default: 50.";
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(440, 631);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 28);
            this.btnCancel.TabIndex = 18;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnTestConnection
            // 
            this.btnTestConnection.Location = new System.Drawing.Point(26, 450);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(120, 28);
            this.btnTestConnection.TabIndex = 24;
            this.btnTestConnection.Text = "Test Connection";
            this.btnTestConnection.UseVisualStyleBackColor = true;
            this.btnTestConnection.Click += new System.EventHandler(this.btnTestConnection_Click);
            // 
            // cmbLanguage
            // 
            this.cmbLanguage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.cmbLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLanguage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbLanguage.ForeColor = System.Drawing.Color.White;
            this.cmbLanguage.FormattingEnabled = true;
            this.cmbLanguage.Location = new System.Drawing.Point(26, 80);
            this.cmbLanguage.Name = "cmbLanguage";
            this.cmbLanguage.Size = new System.Drawing.Size(274, 21);
            this.cmbLanguage.TabIndex = 25;

            // 
            // lblLanguage
            // 
            this.lblLanguage.AutoSize = true;
            this.lblLanguage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.lblLanguage.ForeColor = System.Drawing.Color.White;
            this.lblLanguage.Location = new System.Drawing.Point(22, 58);
            this.lblLanguage.Name = "lblLanguage";
            this.lblLanguage.Size = new System.Drawing.Size(71, 16);
            this.lblLanguage.TabIndex = 26;
            this.lblLanguage.Text = "Language:";
            // 
            // isAdmin
            // 
            this.isAdmin.AutoSize = true;
            this.isAdmin.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.isAdmin.ForeColor = System.Drawing.Color.Orange;
            this.isAdmin.Location = new System.Drawing.Point(325, 390);
            this.isAdmin.Margin = new System.Windows.Forms.Padding(2);
            this.isAdmin.Name = "isAdmin";
            this.isAdmin.Size = new System.Drawing.Size(190, 19);
            this.isAdmin.TabIndex = 33;
            this.isAdmin.Text = "Admin (bidirectional sync)?";
            this.isAdmin.UseVisualStyleBackColor = true;
            // 
            // lblAutoSyncInterval
            // 
            this.lblAutoSyncInterval.AutoSize = true;
            this.lblAutoSyncInterval.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.lblAutoSyncInterval.ForeColor = System.Drawing.Color.White;
            this.lblAutoSyncInterval.Location = new System.Drawing.Point(322, 415);
            this.lblAutoSyncInterval.Name = "lblAutoSyncInterval";
            this.lblAutoSyncInterval.Size = new System.Drawing.Size(160, 16);
            this.lblAutoSyncInterval.TabIndex = 37;
            this.lblAutoSyncInterval.Text = "Auto sync interval (days):";
            // 
            // nudAutoSyncInterval
            // 
            this.nudAutoSyncInterval.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.nudAutoSyncInterval.ForeColor = System.Drawing.Color.White;
            this.nudAutoSyncInterval.Location = new System.Drawing.Point(325, 437);
            this.nudAutoSyncInterval.Maximum = new decimal(new int[] {
            365,
            0,
            0,
            0});
            this.nudAutoSyncInterval.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.nudAutoSyncInterval.Name = "nudAutoSyncInterval";
            this.nudAutoSyncInterval.Size = new System.Drawing.Size(60, 20);
            this.nudAutoSyncInterval.TabIndex = 38;
            this.nudAutoSyncInterval.Value = new decimal(new int[] {
            0,
            0,
            0,
            0});
            // 
            // lblAutoSyncIntervalHint
            // 
            this.lblAutoSyncIntervalHint.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic);
            this.lblAutoSyncIntervalHint.ForeColor = System.Drawing.Color.Gainsboro;
            this.lblAutoSyncIntervalHint.Location = new System.Drawing.Point(322, 462);
            this.lblAutoSyncIntervalHint.Name = "lblAutoSyncIntervalHint";
            this.lblAutoSyncIntervalHint.Size = new System.Drawing.Size(294, 32);
            this.lblAutoSyncIntervalHint.TabIndex = 39;
            this.lblAutoSyncIntervalHint.Text = "0 = sync every startup. Set to 1+ to sync only after N days.";
            // 
            // btnCheckUpdates
            // 
            this.btnCheckUpdates.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.btnCheckUpdates.ForeColor = System.Drawing.Color.Black;
            this.btnCheckUpdates.Location = new System.Drawing.Point(325, 505);
            this.btnCheckUpdates.Name = "btnCheckUpdates";
            this.btnCheckUpdates.Size = new System.Drawing.Size(150, 28);
            this.btnCheckUpdates.TabIndex = 34;
            this.btnCheckUpdates.Text = "Check for Updates";
            this.btnCheckUpdates.UseVisualStyleBackColor = true;
            this.btnCheckUpdates.Click += new System.EventHandler(this.btnCheckUpdates_Click);
            // 
            // btnAbout
            // 
            this.btnAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAbout.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.btnAbout.ForeColor = System.Drawing.Color.Black;
            this.btnAbout.Location = new System.Drawing.Point(26, 631);
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(75, 28);
            this.btnAbout.TabIndex = 35;
            this.btnAbout.Text = "About";
            this.btnAbout.UseVisualStyleBackColor = true;
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
            //
            // lblBehavior
            // 
            this.lblBehavior.AutoSize = true;
            this.lblBehavior.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblBehavior.ForeColor = System.Drawing.Color.White;
            this.lblBehavior.Location = new System.Drawing.Point(322, 58);
            this.lblBehavior.Name = "lblBehavior";
            this.lblBehavior.Size = new System.Drawing.Size(73, 16);
            this.lblBehavior.TabIndex = 27;
            this.lblBehavior.Text = "Behavior:";
            // 
            // RommSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(620, 667);
            this.Controls.Add(this.isAdmin);
            this.Controls.Add(this.lblAutoSyncInterval);
            this.Controls.Add(this.nudAutoSyncInterval);
            this.Controls.Add(this.lblAutoSyncIntervalHint);
            this.Controls.Add(this.lblSaveBatchSize);
            this.Controls.Add(this.nudSaveBatchSize);
            this.Controls.Add(this.lblSaveBatchSizeHint);
            this.Controls.Add(this.btnAbout);
            this.Controls.Add(this.btnCheckUpdates);
            this.Controls.Add(this.lblBehavior);
            this.Controls.Add(this.lblLogRetentionHint);
            this.Controls.Add(this.nudLogRetention);
            this.Controls.Add(this.lblLogRetention);
            this.Controls.Add(this.publicScreenshots);
            this.Controls.Add(this.updateStatsOnLaunch);
            this.Controls.Add(this.lblLanguage);
            this.Controls.Add(this.cmbLanguage);
            this.Controls.Add(this.btnTestConnection);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.forceFullResync);
            this.Controls.Add(this.processPendingOnStartup);
            this.Controls.Add(this.saveLogs);
            this.Controls.Add(this.keepLocalData);
            this.Controls.Add(this.txtRomsPath);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnBrowseRomsPath);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.txtBaseUrl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "RommSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RommSettingsForm";
            this.Load += new System.EventHandler(this.RommSettingsForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudLogRetention)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudAutoSyncInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSaveBatchSize)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtBaseUrl;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnBrowseRomsPath;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txtRomsPath;
        private System.Windows.Forms.CheckBox keepLocalData;
        private System.Windows.Forms.CheckBox saveLogs;
        private System.Windows.Forms.CheckBox processPendingOnStartup;
        private System.Windows.Forms.CheckBox forceFullResync;
        private System.Windows.Forms.CheckBox publicScreenshots;
        private System.Windows.Forms.CheckBox updateStatsOnLaunch;
        private System.Windows.Forms.Label lblLogRetention;
        private System.Windows.Forms.NumericUpDown nudLogRetention;
        private System.Windows.Forms.Label lblLogRetentionHint;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label labelToken;
        private System.Windows.Forms.TextBox txtClientApiToken;
        private System.Windows.Forms.Label labelTokenHint;
        private System.Windows.Forms.Button btnTestConnection;
        private System.Windows.Forms.ComboBox cmbLanguage;
        private System.Windows.Forms.Label lblLanguage;
        private System.Windows.Forms.CheckBox isAdmin;
        private System.Windows.Forms.Label lblBehavior;
        private System.Windows.Forms.Button btnCheckUpdates;
        private System.Windows.Forms.Button btnAbout;
        private System.Windows.Forms.Label lblAutoSyncInterval;
        private System.Windows.Forms.NumericUpDown nudAutoSyncInterval;
        private System.Windows.Forms.Label lblAutoSyncIntervalHint;
        private System.Windows.Forms.Label lblSaveBatchSize;
        private System.Windows.Forms.NumericUpDown nudSaveBatchSize;
        private System.Windows.Forms.Label lblSaveBatchSizeHint;
    }
}
