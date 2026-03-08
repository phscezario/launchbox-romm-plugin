using System;
using System.Windows.Forms;

namespace RommPlugin.UI.Forms
{
    public partial class RommAdversityLoginForm : Form
    {
        public string Username { get; private set; }
        public string Password { get; private set; }

        public RommAdversityLoginForm()
        {
            InitializeComponent();
        }

        private void btnProceed_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Username is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Password is required.");
                return;
            }

            Username = txtUsername.Text;
            Password = txtPassword.Text;

            DialogResult = DialogResult.OK;
            Close();
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
