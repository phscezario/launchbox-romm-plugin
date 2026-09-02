using RommPlugin.Core.Locale;
using RommPlugin.Core.Logging;
using RommPlugin.UI.Forms;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    /// <summary>
    /// Menu item that opens the RomM plugin settings dialog.
    /// </summary>
    public class RommSettingsMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        private static bool _isOpen;

        /// <inheritdoc/>
        public override string Caption => LocaleManager.Get("menu.settings");

        /// <inheritdoc/>
        public override void OnSelected()
        {
            if (_isOpen) return;
            _isOpen = true;
            using (var form = new RommSettingsForm())
            {
                form.ShowDialog();
            }
            _isOpen = false;
        }
    }
}
