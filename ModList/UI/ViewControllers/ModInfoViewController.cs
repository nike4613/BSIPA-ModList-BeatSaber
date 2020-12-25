using System.Diagnostics.CodeAnalysis;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.ModList.BeatSaber.Models;
using SiraUtil.Tools;
using UnityEngine;
using Zenject;

namespace IPA.ModList.BeatSaber.UI.ViewControllers
{
    [HotReload(RelativePathToLayout = @"..\Views\ModInfoView.bsml")]
    [ViewDefinition("IPA.ModList.BeatSaber.UI.Views.ModInfoView.bsml")]
    internal class ModInfoViewController : BSMLAutomaticViewController
    {
        private SiraLog siraLog = null!;

        private PluginInformation? pluginInfo;

        internal bool Activated { get; private set; }

        [Inject]
        internal void Construct(SiraLog siraLog)
        {
            this.siraLog = siraLog;
        }

        [UIParams]
        internal BSMLParserParams ParserParams = null!;

        public Sprite? PluginIcon => pluginInfo?.Icon;

        [UIValue("show-select-plugin-text")]
        public bool ShowSelectPlugin => pluginInfo == null;

        [UIValue("plugin-selected")]
        public bool PluginSelected => pluginInfo != null;

        [UIValue("name")]
        public string PluginName => pluginInfo?.Plugin.Name ?? string.Empty;

        [UIValue("version")]
        public string PluginVersion => pluginInfo?.Plugin.Version.ToString() ?? string.Empty;

        [UIValue("author")]
        public string PluginAuthor => pluginInfo?.Plugin.Author ?? string.Empty;

        [UIValue("description")]
        public string PluginDescription => pluginInfo?.Plugin.Description ?? string.Empty;

        [UIComponent("IconImage")]
        internal ImageView IconImage = null!;

        public void SetPlugin(PluginInformation plugin)
        {
            pluginInfo = plugin;
            NotifyPluginChanged();
        }

        [UIAction("OnDescLinkPressed")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void OnDescriptionLinkPressed(string url, string title)
        {
            siraLog.Debug($"Link to {url} ({title}) has been clicked");

            OpenLink(url, title);
        }

        #region Link Panel management

        [UIValue("has_links")]
        internal bool HasLinks => HasSourceLink || HasHomeLink || HasDonateLink;

        [UIValue("description-panel-height")]
        internal int PreferredDescriptionPanelHeight => HasLinks ? 50 : 60;

        private string? SourceLink => pluginInfo?.Plugin.PluginSourceLink?.ToString();
        private string? HomeLink => pluginInfo?.Plugin.PluginHomeLink?.ToString();
        private string? DonateLink => pluginInfo?.Plugin.DonateLink?.ToString();

        [UIValue("has_source_link")]
        internal bool HasSourceLink => SourceLink != null;

        [UIValue("has_home_link")]
        internal bool HasHomeLink => HomeLink != null;

        [UIValue("has_donate_link")]
        internal bool HasDonateLink => DonateLink != null;

        private void NotifyLinksChanged()
        {
            NotifyPropertyChanged(nameof(HasLinks));
            NotifyPropertyChanged(nameof(HasSourceLink));
            NotifyPropertyChanged(nameof(HasHomeLink));
            NotifyPropertyChanged(nameof(HasDonateLink));
            NotifyPropertyChanged(nameof(PreferredDescriptionPanelHeight));
        }

        [UIAction(nameof(SourceLinkPressed))]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void SourceLinkPressed() => OpenLink(SourceLink!, $"{PluginName} Source");

        [UIAction(nameof(HomeLinkPressed))]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void HomeLinkPressed() => OpenLink(HomeLink!, $"{PluginName} Home");

        [UIAction(nameof(DonateLinkPressed))]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void DonateLinkPressed() => OpenLink(DonateLink!, $"Donate to {PluginAuthor}");

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
        internal string CurrentLinkUrl { get; private set; } = string.Empty;

        [UIValue("link_title")]
        internal string CurrentLinkTitle { get; private set; } = string.Empty;

        [UIValue("link_has_title")]
        internal bool HasLinkTitle => !string.IsNullOrEmpty(CurrentLinkTitle);

        [UIAction("ConfirmOpenLink")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void OpenLink()
        {
            siraLog.Debug($"Link to {CurrentLinkUrl} ({CurrentLinkTitle}) has been confirmed");

            Application.OpenURL(CurrentLinkUrl);
        }

        #endregion

        private void NotifyPluginChanged()
        {
            NotifyPropertyChanged(nameof(ShowSelectPlugin));
            NotifyPropertyChanged(nameof(PluginSelected));
            NotifyPropertyChanged(nameof(PluginIcon));
            NotifyPropertyChanged(nameof(PluginName));
            NotifyPropertyChanged(nameof(PluginVersion));
            NotifyPropertyChanged(nameof(PluginAuthor));
            NotifyPropertyChanged(nameof(PluginDescription));
            NotifyLinksChanged();

            if (IconImage != null)
            {
                IconImage.sprite = PluginIcon;
            }
        }

        [UIAction("#post-parse")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void Setup()
        {
            if (IconImage != null && PluginIcon != null)
            {
                IconImage.sprite = PluginIcon;
            }
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            Activated = true;
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            Activated = false;

            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }
    }
}