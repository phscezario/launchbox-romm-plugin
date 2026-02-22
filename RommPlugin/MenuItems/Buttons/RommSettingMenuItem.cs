using RommPlugin.UI.Forms;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    public class RommSettingsMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        public override string Caption => "RomM: Configurations";

        public override void OnSelected()
        {
            var form = new RommSettingsForm();
            form.ShowDialog();
        }
    }
}
