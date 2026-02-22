using System;
using System.Drawing;
using System.Windows;
using RommPlugin.Services;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin
{
    public class RommMenuPlugin : ISystemEventsPlugin
    {
        private RommSyncService sync = new RommSyncService();

        public async void OnEventRaised(string eventType)
        {
            if (eventType != SystemEventTypes.LaunchBoxStartupCompleted)
            {
                return;
            }

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
