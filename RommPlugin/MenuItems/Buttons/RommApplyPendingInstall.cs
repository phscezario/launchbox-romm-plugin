using System;
using RommPlugin.Services;
using Unbroken.LaunchBox.Plugins;

namespace RommPlugin.MenuItems.Buttons
{
    public class RommApplyPendingMenuMenuItem : RommMenuItem, ISystemMenuItemPlugin
    {
        private static readonly RommSyncService sync = new RommSyncService();

        public override string Caption => "RomM: Apply pending installs";

        public override async void OnSelected()
        {
            try
            {
                await sync.ProcessSyncEvents();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[RommPlugin] Sync error: " + ex);
            }
        }
    }

}
