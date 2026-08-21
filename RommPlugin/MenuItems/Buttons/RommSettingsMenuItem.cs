using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.UI.Forms;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    public class RommSettingsMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        private static bool _isOpen;

        public override string Caption => LocaleManager.Get("menu.settings");

        public override void OnSelected()
        {
            if (_isOpen) return;
            _isOpen = true;
            RommLogger.Log("[DIAG] RommSettingsMenuItem.OnSelected: clicked");
            using (var form = new RommSettingsForm())
            {
                form.ShowDialog();
            }
            _isOpen = false;
        }
    }
}
