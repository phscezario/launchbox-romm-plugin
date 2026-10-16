namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Desired router flags for DLC/game-content entries. Pure data so it can
    /// be unit tested without LaunchBox types.
    /// </summary>
    public sealed class DlcRoute
    {
        public bool UseEmulator { get; set; }
        public string EmulatorId { get; set; }
        public bool WaitForExit { get; set; }
    }

    /// <summary>
    /// Pure decision logic for routing DLC entries through the game's emulator.
    /// Opt-in via DlcUseEmulator: the emulator (or its wrapper) owns DLC
    /// installation, so entries boot through it instead of running files
    /// directly. UI-agnostic and free of LaunchBox types so it can be unit tested.
    /// </summary>
    public static class DlcEntryRouting
    {
        /// <param name="dlcUseEmulator">The _launchbox.json opt-in flag (may be null).</param>
        /// <param name="gameEmulatorId">The game's assigned emulator id (may be null).</param>
        public static DlcRoute Decide(bool? dlcUseEmulator, string gameEmulatorId)
        {
            bool viaEmulator = dlcUseEmulator == true && !string.IsNullOrWhiteSpace(gameEmulatorId);
            return new DlcRoute
            {
                UseEmulator = viaEmulator,
                EmulatorId = viaEmulator ? gameEmulatorId : null,
                WaitForExit = viaEmulator,
            };
        }
    }
}
