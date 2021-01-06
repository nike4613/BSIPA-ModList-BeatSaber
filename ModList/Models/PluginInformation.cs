using IPA.Loader;
using IPA.ModList.BeatSaber.Utilities;
using UnityEngine;

namespace IPA.ModList.BeatSaber.Models
{
    internal class PluginInformation
    {
        private Sprite? icon;

        public PluginMetadata Plugin { get; }
        public PluginState State { get; set; }
        public Sprite Icon => icon ??= Plugin.ReadPluginIcon();

        public PluginInformation(PluginMetadata meta, PluginState state)
        {
            Plugin = meta;
            State = state;
        }
    }
}