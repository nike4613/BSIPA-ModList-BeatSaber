using System;
using IPA.Loader;
using IPA.ModList.BeatSaber.Utilities;
using UnityEngine;

namespace IPA.ModList.BeatSaber.Models
{
    internal class PluginInformation
    {
        private Sprite? icon;
        public bool SpriteWasLoaded { get; private set; }
        public event Action<Sprite>? SpriteLoadedEvent;
        public PluginMetadata Plugin { get; }
        public PluginState State { get; set; }

        public Sprite Icon
        {
            get
            {
                if (icon != null)
                {
                    return icon;
                }

                return Plugin.QueueReadPluginIcon((Sprite icon) =>
                {
                    this.icon = icon;
                    SpriteWasLoaded = true;
                    SpriteLoadedEvent?.Invoke(icon);
                });
            }
        }

        public PluginInformation(PluginMetadata meta, PluginState state)
        {
            Plugin = meta;
            State = state;
        }
    }
}