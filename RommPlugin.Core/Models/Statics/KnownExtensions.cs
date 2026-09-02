using System;
using System.Collections.Generic;

namespace RommPlugin.Core.Models.Statics
{
    /// <summary>
    /// Contains a set of known ROM and game file extensions recognized by the plugin.
    /// Used for file type detection and filtering during sync and installation operations.
    /// </summary>
    public static class KnownExtensions
    {
        /// <summary>
        /// Set of known ROM file extensions (case-insensitive).
        /// Includes archive formats (zip, 7z, rar), disc images (iso, cue, bin, chd),
        /// and platform-specific ROM formats (nes, gba, nds, etc.).
        /// </summary>
        public static readonly HashSet<string> Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".7z", ".rar",
            ".iso", ".cue", ".bin", ".img",
            ".chd", ".cso",
            ".nes", ".sfc", ".smc", ".gba",
            ".gb", ".gbc", ".n64", ".z64", ".v64",
            ".nds", ".3ds",
            ".gcz", ".nkit",
            ".xiso", ".xci", ".rvz",
            ".vpx", ".wad", ".wux",
            ".elf", ".prx", ".pkg"
        };
    }
}
