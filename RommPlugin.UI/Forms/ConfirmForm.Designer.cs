namespace RommPlugin.UI.Forms
{
    partial class ConfirmForm
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
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.chkSuppress = new System.Windows.Forms.CheckBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // txtMessage
            //
            this.txtMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMessage.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.txtMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtMessage.ForeColor = System.Drawing.Color.White;
            this.txtMessage.Location = new System.Drawing.Point(20, 20);
            this.txtMessage.Multiline = true;
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.ReadOnly = true;
            this.txtMessage.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtMessage.WordWrap = false;

            this.txtMessage.Size = new System.Drawing.Size(420, 120);
            this.txtMessage.TabIndex = 0;
            //
            // chkSuppress
            //
            this.chkSuppress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkSuppress.ForeColor = System.Drawing.Color.LightGray;
            this.chkSuppress.Location = new System.Drawing.Point(20, 150);
            this.chkSuppress.Name = "chkSuppress";
            this.chkSuppress.Size = new System.Drawing.Size(350, 24);
            this.chkSuppress.TabIndex = 1;
            this.chkSuppress.UseVisualStyleBackColor = false;
            //
            // btnOk
            //
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.ForeColor = System.Drawing.Color.White;
            this.btnOk.Location = new System.Drawing.Point(280, 185);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(90, 30);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = false;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            //
            // ConfirmForm
            //
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.ClientSize = new System.Drawing.Size(390, 230);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.chkSuppress);
            this.Controls.Add(this.btnOk);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfirmForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Romm Plugin";
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.CheckBox chkSuppress;
        private System.Windows.Forms.Button btnOk;

        private void btnOk_Click(object sender, System.EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }
    }
}
