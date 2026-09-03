using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Models;
using RommPlugin.Core.Storage;
using RommPlugin.UI.Helpers;

namespace RommPlugin.UI.Forms
{
    /// <summary>
    /// A form that allows users to select which platforms to sync from the Romm server.
    /// </summary>
    public partial class RommPlatformSelectorForm : Form
    {
        /// <summary>
        /// Gets the list of platform selections with their current checked state.
        /// </summary>
        public List<PlatformSelection> Platforms { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RommPlatformSelectorForm"/> class.
        /// </summary>
        /// <param name="platforms">The list of platforms to display for selection.</param>
        public RommPlatformSelectorForm(List<PlatformSelection> platforms)
        {
            InitializeComponent();
            FormIconHelper.LoadIcon(this);
            ApplyLocale();
            ActiveControl = btnCancel;

            Platforms = platforms;

            checkedListBoxPlatforms.BeginUpdate();

            foreach (var p in Platforms)
            {
                checkedListBoxPlatforms.Items.Add(p.Name, p.Selected);
            }

            checkedListBoxPlatforms.EndUpdate();
        }

        private void ApplyLocale()
        {
            Text = LocaleManager.Get("platform_selector.window_title");
            label1.Text = LocaleManager.Get("platform_selector.title");
            btnSelectAll.Text = LocaleManager.Get("platform_selector.select_all");
            btnClearAll.Text = LocaleManager.Get("platform_selector.clear_all");
            btnOk.Text = LocaleManager.Get("platform_selector.ok");
            btnCancel.Text = LocaleManager.Get("platform_selector.cancel");
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
