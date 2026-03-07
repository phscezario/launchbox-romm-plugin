using System;
using RommPlugin.Services;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin
{
    public class RommMenuPlugin : ISystemEventsPlugin
    {
        private RommProcessInstallUninstallService sync = new RommProcessInstallUninstallService();

        public async void OnEventRaised(string eventType)
        {
            if (eventType != SystemEventTypes.LaunchBoxStartupCompleted)
            {
                return;
            }

            try
            {
                await sync.ProcessInstallUninstallEvents();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[RommPlugin] Sync error: " + ex);
            }
        }
    }
}
