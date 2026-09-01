using RommPlugin.Core.Locale;
using RommPlugin.Services;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    public class RommGameManagerMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        public override string Caption => LocaleManager.Get("menu.game_manager");

        public override void OnSelected()
        {
            GameManagerLauncher.EnsureOpen();
        }
    }
}
