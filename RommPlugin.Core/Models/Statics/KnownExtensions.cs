using System;
using System.Collections.Generic;

namespace RommPlugin.Core.Models.Statics
{
    public static class KnownExtensions
    {
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
            ".vpx", ".wad", ".wux"
        };
    }
}
