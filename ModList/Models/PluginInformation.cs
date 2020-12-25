using IPA.Loader;
using IPA.ModList.BeatSaber.Helpers;
using UnityEngine;

namespace IPA.ModList.BeatSaber.Models
{
    internal class PluginInformation
    {
        private Sprite? _icon;

        public PluginMetadata Plugin { get; }
        public PluginState State { get; set; }
        public Sprite Icon => _icon ??= Plugin.ReadPluginIcon();

        public PluginInformation(PluginMetadata meta, PluginState state)
        {
            Plugin = meta;
            State = state;
        }
    }
}