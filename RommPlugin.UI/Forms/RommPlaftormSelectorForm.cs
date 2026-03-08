using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RommPlugin.Core.Models;

namespace RommPlugin.UI.Forms
{
    public partial class RommPlatformSelectorForm : Form
    {
        public List<PlatformSelection> Platforms { get; private set; }

        public RommPlatformSelectorForm(List<PlatformSelection> platforms)
        {
            InitializeComponent();

            Platforms = platforms;

            checkedListBoxPlatforms.BeginUpdate();

            foreach (var p in Platforms)
            {
                checkedListBoxPlatforms.Items.Add(p.Name, p.Selected);
            }

            checkedListBoxPlatforms.EndUpdate();
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBoxPlatforms.Items.Count; i++)
            {
                checkedListBoxPlatforms.SetItemChecked(i, true);
            }
        }

        private void btnClearAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBoxPlatforms.Items.Count; i++)
            {
                checkedListBoxPlatforms.SetItemChecked(i, false);
            }  
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBoxPlatforms.Items.Count; i++)
            {
                Platforms[i].Selected = checkedListBoxPlatforms.GetItemChecked(i);
            }

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
