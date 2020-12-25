using System;

namespace IPA.ModList.BeatSaber.Models
{
    [Flags]
    internal enum PluginState
    {
        Enabled,
        Disabled,
        Ignored
    }
}