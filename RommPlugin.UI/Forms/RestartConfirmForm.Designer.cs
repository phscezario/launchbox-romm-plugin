namespace RommPlugin.UI.Forms
{
    partial class RestartConfirmForm
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
            this.lblMessage = new System.Windows.Forms.Label();
            this.btnRestartNow = new System.Windows.Forms.Button();
            this.btnRestartLater = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblMessage
            //
            this.lblMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMessage.ForeColor = System.Drawing.Color.White;
            this.lblMessage.Location = new System.Drawing.Point(20, 20);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(350, 60);
            this.lblMessage.TabIndex = 0;
            this.lblMessage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // btnRestartNow
            //
            this.btnRestartNow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRestartNow.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnRestartNow.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRestartNow.ForeColor = System.Drawing.Color.White;
            this.btnRestartNow.Location = new System.Drawing.Point(200, 95);
            this.btnRestartNow.Name = "btnRestartNow";
            this.btnRestartNow.Size = new System.Drawing.Size(110, 30);
            this.btnRestartNow.TabIndex = 1;
            this.btnRestartNow.Text = "Restart Now";
            this.btnRestartNow.UseVisualStyleBackColor = false;
            this.btnRestartNow.Click += new System.EventHandler(this.btnRestartNow_Click);
            //
            // btnRestartLater
            //
            this.btnRestartLater.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRestartLater.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnRestartLater.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRestartLater.ForeColor = System.Drawing.Color.White;
            this.btnRestartLater.Location = new System.Drawing.Point(320, 95);
            this.btnRestartLater.Name = "btnRestartLater";
            this.btnRestartLater.Size = new System.Drawing.Size(110, 30);
            this.btnRestartLater.TabIndex = 2;
            this.btnRestartLater.Text = "Restart Later";
            this.btnRestartLater.UseVisualStyleBackColor = false;
            this.btnRestartLater.Click += new System.EventHandler(this.btnRestartLater_Click);
            //
            // RestartConfirmForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.ClientSize = new System.Drawing.Size(450, 145);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.btnRestartNow);
            this.Controls.Add(this.btnRestartLater);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RestartConfirmForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RomM Plugin";
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblMessage;
        private System.Windows.Forms.Button btnRestartNow;
        private System.Windows.Forms.Button btnRestartLater;

        private void btnRestartNow_Click(object sender, System.EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Yes;
            Close();
        }

        private void btnRestartLater_Click(object sender, System.EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.No;
            Close();
        }
    }
}
