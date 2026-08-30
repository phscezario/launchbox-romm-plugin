using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RommPlugin.Core.Storage;

namespace RommPlugin.UI.Helpers
{
    public static class FormIconHelper
    {
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
