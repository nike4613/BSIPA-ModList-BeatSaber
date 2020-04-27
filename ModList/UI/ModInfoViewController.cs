using BeatSaberMarkupLanguage.Attributes;
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

        [UIComponent("icon-img")]
        internal RawImage IconImage = null;

        private PluginInformation plugin;
        private void NotifyPluginChanged()
        {
            NotifyPropertyChanged(nameof(PluginIcon));
            NotifyPropertyChanged(nameof(PluginName));
            NotifyPropertyChanged(nameof(PluginVersion));
            NotifyPropertyChanged(nameof(PluginAuthor));
            NotifyPropertyChanged(nameof(PluginDescription));

            if (IconImage != null)
                IconImage.texture = PluginIcon;
        }

        public bool Activated { get; private set; } = false;

        public void SetPlugin(PluginInformation plugin)
        {
            this.plugin = plugin;
            NotifyPluginChanged();
        }

        [UIAction("#post-parse")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void Setup()
        {
            (transform as RectTransform).sizeDelta = new Vector2(70, 0);
            (transform as RectTransform).anchorMin = new Vector2(0.5f, 0);
            (transform as RectTransform).anchorMax = new Vector2(0.5f, 1);

            if (IconImage != null)
                IconImage.texture = PluginIcon;
        }

        [UIValue("link_url")]
        public string CurrentLinkUrl { get; private set; } = "";

        [UIValue("link_title")]
        public string CurrentLinkTitle { get; private set; } = "";

        [UIComponent("link-title-text")]
        internal TextMeshProUGUI LinkTitleText;

        [UIAction("OnDescLinkPressed")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void OnDescriptionLinkPressed(string url, string title)
        {
            Logger.log.Debug($"Link to {url} ({title}) has been clicked");

            CurrentLinkUrl = url;
            CurrentLinkTitle = title ?? "";
            NotifyPropertyChanged(nameof(CurrentLinkUrl));
            NotifyPropertyChanged(nameof(CurrentLinkTitle));

            LinkTitleText.enabled = !string.IsNullOrEmpty(CurrentLinkTitle);

            ParserParams.EmitEvent("ShowLinkModal");
        }

        [UIAction("ConfirmOpenLink")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void OpenLink()
        {
            Logger.log.Debug($"Link to {CurrentLinkUrl} ({CurrentLinkTitle}) has been confirmed");

            Process.Start(CurrentLinkUrl);
        }

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
