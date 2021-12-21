using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using IPA.ModList.BeatSaber.Models;
using IPA.ModList.BeatSaber.Utilities;
using UnityEngine;

namespace IPA.ModList.BeatSaber.UI.ViewControllers
{
    internal class PluginCellViewController : TableCell, INotifyPropertyChanged
    {
        private PluginInformation? pluginInformation;

        private string? pluginName;
        public string Name
        {
            get => pluginName ?? "";
            set
            {
                pluginName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        private string? authorAndVersion;
        public string AuthorAndVersion
        {
            get => authorAndVersion ?? "";
            set
            {
                authorAndVersion = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AuthorAndVersion)));
            }
        }

        public Sprite? IconSprite;
        public List<Sprite>? StatusSprites;

        [UIComponent("IconImage")]
        internal ImageView IconImage = null!;

        [UIComponent("StatusIcon1")]
        internal ImageView StatusIcon1 = null!;

        [UIComponent("StatusIcon2")]
        internal ImageView StatusIcon2 = null!;

        [UIComponent("background")]
        internal ImageView background = null!;

        public readonly Color highlightedColor = new Color(0xBD, 0x28, 0x81, 0x99);
        public readonly Color selectedColor = new Color(0xFF, 0x69, 0xB4);

        public event PropertyChangedEventHandler? PropertyChanged;

        public PluginCellViewController PopulateCell(PluginInformation pluginInformation)
        {
            this.pluginInformation = pluginInformation;

            if (!pluginInformation.SpriteWasLoaded)
            {
                pluginInformation.SpriteLoadedEvent += OnSpriteLoaded;
            }

            IconSprite = pluginInformation.Icon;
            IconImage.sprite = IconSprite;
            Name = pluginInformation.Plugin.Name;
            AuthorAndVersion = $"{pluginInformation.Plugin.Author} <size=80%>{pluginInformation.Plugin.HVersion}</size>";
            StatusSprites = Enumerable.Empty<Sprite>()
                        .AppendIf(pluginInformation.Plugin.IsBare, Helpers.LibrarySprite)
                        .AppendIf(pluginInformation.State == PluginState.Disabled, Helpers.XSprite)
                        .AppendIf(pluginInformation.State == PluginState.Enabled && pluginInformation.Plugin.RuntimeOptions == RuntimeOptions.DynamicInit, Helpers.OSprite)
                        .AppendIf(pluginInformation.State == PluginState.Ignored, Helpers.WarnSprite).ToList();

            ConfigureStatusIconImageView(StatusIcon1, 0);
            ConfigureStatusIconImageView(StatusIcon2, 1);

            return this;
        }

        private void OnSpriteLoaded(PluginInformation pluginInformation, Sprite iconSprite)
        {
            pluginInformation.SpriteLoadedEvent -= OnSpriteLoaded;

            if (this.pluginInformation == pluginInformation)
            {
                IconSprite = iconSprite;
                if (IconImage != null)
                {
                    IconImage.sprite = iconSprite;
                }
            }
        }

        private void ConfigureStatusIconImageView(ImageView statusImageView, int index)
        {
            if (StatusSprites != null && StatusSprites.Count > index)
            {
                statusImageView.sprite = StatusSprites[index];
                statusImageView.enabled = true;
            }
            else
            {
                statusImageView.enabled = false;
            }
        }

        protected override void SelectionDidChange(TransitionType transitionType) => RefreshBackground();

        protected override void HighlightDidChange(TransitionType transitionType) => RefreshBackground();

        private void RefreshBackground()
        {
            if (highlighted)
            {
                background.color = highlightedColor;
            }
            else if (selected)
            {
                background.color = selectedColor;
            }
            background.gameObject.SetActive(highlighted || selected);
        }
    }
}