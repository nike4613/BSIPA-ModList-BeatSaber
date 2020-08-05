using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IPA.ModList.BeatSaber.UI
{
    [HotReload(PathMap = new[] { "C:\\", CompileConstants.SolutionDirectory })]
    internal class ModInfoViewController : BSMLAutomaticViewController
    {
        [UIParams]
        internal BSMLParserParams ParserParams;

        [UIValue("icon")]
        public Texture2D PluginIcon => plugin.Icon;
        [UIValue("name")]
        public string PluginName => plugin.Plugin.Name;
        [UIValue("version")]
        public string PluginVersion => plugin.Plugin.Version.ToString();
        [UIValue("author")]
        public string PluginAuthor => plugin.Plugin.Author;
        [UIValue("description")]
        public string PluginDescription => plugin.Plugin.Description;

        [UIComponent("IconImage")]
        internal RawImage IconImage = null;

        private PluginInformation plugin;
        private void NotifyPluginChanged()
        {
            NotifyPropertyChanged(nameof(PluginIcon));
            NotifyPropertyChanged(nameof(PluginName));
            NotifyPropertyChanged(nameof(PluginVersion));
            NotifyPropertyChanged(nameof(PluginAuthor));
            NotifyPropertyChanged(nameof(PluginDescription));
            NotifyLinksChanged();

            if (IconImage != null)
                IconImage.texture = PluginIcon;

            UpdateDescriptionBackground();
        }

        public bool Activated { get; private set; } = false;

        public void SetPlugin(PluginInformation plugin)
        {
            this.plugin = plugin;
            NotifyPluginChanged();
        }

        [UIComponent("DescriptionBackground")]
        internal Backgroundable DescriptionBackground = null;
        private Sprite descBgSprite = null;
        private Sprite descBgSpriteFlatBase = null;

        [UIAction("#post-parse")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void Setup()
        {
            (transform as RectTransform).sizeDelta = new Vector2(70, 0);
            (transform as RectTransform).anchorMin = new Vector2(0.5f, 0);
            (transform as RectTransform).anchorMax = new Vector2(0.5f, 1);

            if (IconImage != null)
                IconImage.texture = PluginIcon;
            if (DescriptionBackground != null)
            {
                descBgSprite = Helpers.SmallRoundedRectSprite;
                descBgSpriteFlatBase = Helpers.SmallRoundedRectFlatSprite;
            }

            UpdateDescriptionBackground();
        }

        private void UpdateDescriptionBackground()
        {
            if (DescriptionBackground != null)
            {
                DescriptionBackground.background.sprite
                    = HasLinks ? descBgSpriteFlatBase
                               : descBgSprite;
            }
        }

        [UIAction("OnDescLinkPressed")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void OnDescriptionLinkPressed(string url, string title)
        {
            Logger.log.Debug($"Link to {url} ({title}) has been clicked");

            OpenLink(url, title);
        }

        #region Link Panel management
        [UIValue("has_links")]
        internal bool HasLinks => HasSourceLink || HasHomeLink || HasDonateLink;
        [UIValue("show_pad_links")]
        internal bool ShowPadLinks // only true when exactly one link is present
            => (HasSourceLink ? 1 : 0) + (HasHomeLink ? 1 : 0) + (HasDonateLink ? 1 : 0) == 1;

        [UIValue("desc_anchor_min")]
        internal float DescriptionAnchorMinY => HasLinks ? .1f : 0f;
        [UIValue("desc_size_delta_min")]
        internal float DescriptionContentSizeDeltaY => HasLinks ? -2.5f : -5f;

        private string SourceLink => plugin.Plugin.PluginSourceLink?.ToString();
        private string HomeLink => plugin.Plugin.PluginHomeLink?.ToString();
        private string DonateLink => plugin.Plugin.DonateLink?.ToString();

        [UIValue("has_source_link")]
        internal bool HasSourceLink => SourceLink != null;
        [UIValue("has_home_link")]
        internal bool HasHomeLink => HomeLink != null;
        [UIValue("has_donate_link")]
        internal bool HasDonateLink => DonateLink != null;

        private void NotifyLinksChanged()
        {
            NotifyPropertyChanged(nameof(HasLinks));
            NotifyPropertyChanged(nameof(ShowPadLinks));
            NotifyPropertyChanged(nameof(HasSourceLink));
            NotifyPropertyChanged(nameof(HasHomeLink));
            NotifyPropertyChanged(nameof(HasDonateLink));
            NotifyPropertyChanged(nameof(DescriptionAnchorMinY));
            NotifyPropertyChanged(nameof(DescriptionContentSizeDeltaY));
        }

        [UIAction(nameof(SourceLinkPressed))]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void SourceLinkPressed() => OpenLink(SourceLink, $"{PluginName} Source");
        [UIAction(nameof(HomeLinkPressed))]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void HomeLinkPressed() => OpenLink(HomeLink, $"{PluginName} Home");
        [UIAction(nameof(DonateLinkPressed))]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void DonateLinkPressed() => OpenLink(DonateLink, $"Donate to {PluginAuthor}");
        #endregion

        #region Links
        private void OpenLink(string url, string title)
        {
            CurrentLinkUrl = url;
            CurrentLinkTitle = title ?? "";
            NotifyPropertyChanged(nameof(CurrentLinkUrl));
            NotifyPropertyChanged(nameof(CurrentLinkTitle));
            NotifyPropertyChanged(nameof(HasLinkTitle));

            ParserParams.EmitEvent("ShowLinkModal");
        }

        [UIValue("link_url")]
        internal string CurrentLinkUrl { get; private set; } = "";

        [UIValue("link_title")]
        internal string CurrentLinkTitle { get; private set; } = "";
        [UIValue("link_has_title")]
        internal bool HasLinkTitle => !string.IsNullOrEmpty(CurrentLinkTitle);

        [UIAction("ConfirmOpenLink")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void OpenLink()
        {
            Logger.log.Debug($"Link to {CurrentLinkUrl} ({CurrentLinkTitle}) has been confirmed");

            Process.Start(CurrentLinkUrl);
        }
        #endregion

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            Activated = true;
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            Activated = false;
        }

    }
}
