using RommPlugin.Core.Locale;
using RommPlugin.Services;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    /// <summary>
    /// Menu item that opens the Game Manager window for handling install and uninstall operations.
    /// </summary>
    public class RommGameManagerMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        /// <inheritdoc/>
        public override string Caption => LocaleManager.Get("menu.game_manager");

        /// <inheritdoc/>
        public override void OnSelected()
        {
            GameManagerLauncher.EnsureOpen();
        }
    }
}
