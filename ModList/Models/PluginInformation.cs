using System;
using IPA.Loader;
using IPA.ModList.BeatSaber.Utilities;
using UnityEngine;

namespace IPA.ModList.BeatSaber.Models
{
    internal class PluginInformation
    {
        private Sprite? icon;
        public bool spriteWasLoaded;
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
                Plugin.QueueReadPluginIcon((Sprite icon) =>
                {
                    this.icon = icon;
                    spriteWasLoaded = true;
                    SpriteLoadedEvent?.Invoke(icon);
                });
                return BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;
            }
        }

        public PluginInformation(PluginMetadata meta, PluginState state)
        {
            Plugin = meta;
            State = state;
        }
    }
}