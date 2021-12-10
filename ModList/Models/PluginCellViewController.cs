using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using IPA.ModList.BeatSaber.Utilities;
using UnityEngine;

namespace IPA.ModList.BeatSaber.Models
{
    internal class PluginCellViewController
    {
        private PluginInformation pluginInformation;

        public string Name;
        public string AuthorAndVersion;
        public Sprite? IconSprite;
        public List<Sprite> StatusSprites;

        [UIComponent("IconImage")]
        internal ImageView IconImage = null!;

        [UIComponent("StatusIcon1")]
        internal ImageView StatusIcon1 = null!;

        [UIComponent("StatusIcon2")]
        internal ImageView StatusIcon2 = null!;

        public PluginCellViewController(PluginInformation pluginInformation)
        {
            this.pluginInformation = pluginInformation;

            if (!pluginInformation.spriteWasLoaded)
            {
                pluginInformation.SpriteLoadedEvent += OnSpriteLoaded;
            }

            IconSprite = pluginInformation.Icon;
            Name = pluginInformation.Plugin.Name;
            AuthorAndVersion = $"{pluginInformation.Plugin.Author} <size=80%>{pluginInformation.Plugin.HVersion}</size>";
            StatusSprites = Enumerable.Empty<Sprite>()
                        .AppendIf(pluginInformation.Plugin.IsBare, Helpers.LibrarySprite)
                        .AppendIf(pluginInformation.State == PluginState.Disabled, Helpers.XSprite)
                        .AppendIf(pluginInformation.State == PluginState.Enabled && pluginInformation.Plugin.RuntimeOptions == RuntimeOptions.DynamicInit, Helpers.OSprite)
                        .AppendIf(pluginInformation.State == PluginState.Ignored, Helpers.WarnSprite).ToList();
        }

        private void OnSpriteLoaded(Sprite iconSprite)
        {
            pluginInformation.SpriteLoadedEvent -= OnSpriteLoaded;
            IconSprite = iconSprite;

            if (IconImage != null)
            {
                IconImage.sprite = iconSprite;
            }
        }

        [UIAction("#post-parse")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void Setup()
        {
            IconImage.sprite = IconSprite;

            ConfigureStatusIconImageView(StatusIcon1, 0);
            ConfigureStatusIconImageView(StatusIcon2, 1);
        }

        private void ConfigureStatusIconImageView(ImageView statusImageView, int index)
        {
            if (StatusSprites.Count > index)
            {
                statusImageView.sprite = StatusSprites[index];
                statusImageView.enabled = true;
            }
            else
            {
                statusImageView.enabled = false;
            }
        }
    }
}