using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RommPlugin.Core.Storage;

namespace RommPlugin.UI.Helpers
{
    /// <summary>
    /// Provides helper methods for loading form icons from the plugin's image resources.
    /// </summary>
    public static class FormIconHelper
    {
        /// <summary>
        /// Loads and sets the icon for a <see cref="Form"/> from the plugin's images folder.
        /// </summary>
        /// <param name="form">The form to set the icon on.</param>
        public static void LoadIcon(Form form)
        {
            try
            {
                var iconPath = Path.Combine(RommPaths.ImagesFolder, "ico.ico");

                if (!File.Exists(iconPath))
                {
                    iconPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Images", "ico.ico");
                }

                if (File.Exists(iconPath))
                {
                    form.Icon = new Icon(iconPath);
                }
            }
            catch
            {
            }
        }
    }
}
