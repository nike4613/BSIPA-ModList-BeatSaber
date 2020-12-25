using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using UnityEngine;

namespace IPA.ModList.BeatSaber.Models
{
    internal class PluginCellViewController
    {
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

        public PluginCellViewController(string name, string authorAndVersion, Sprite iconSprite, IEnumerable<Sprite> statusSprites)
        {
            IconSprite = iconSprite;

            Name = name;
            AuthorAndVersion = authorAndVersion;
            StatusSprites = statusSprites.ToList();
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