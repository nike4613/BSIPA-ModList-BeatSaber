using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMarkupLanguage.ViewControllers;
using IPA.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using HMUI;

namespace IPA.ModList.BeatSaber.UI
{

    [HotReload(PathMap = new[] { "C:\\", CompileConstants.SolutionDirectory })]
    internal class ModListViewController : BSMLAutomaticViewController, INotifiableHost
    {
        private List<CustomListTableData.CustomCellInfo> ListValues { get; } = new List<CustomListTableData.CustomCellInfo>();

#pragma warning disable CS0649 // Field never assigned
        [UIComponent("list")]
        internal CustomListTableData customListTableData;
#pragma warning restore CS0649 // Field never assigned

        private List<PluginInformation> PluginList { get; } = new List<PluginInformation>();

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            Logger.log.Debug("Test VC activated");

            customListTableData.data = ListValues;
            ReloadViewList();
        }

        public event Action<PluginInformation> DidSelectPlugin;

        [UIAction("#post-parse")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void SetupList()
        {
            (transform as RectTransform).sizeDelta = new Vector2(70, 0);
            (transform as RectTransform).anchorMin = new Vector2(0.5f, 0);
            (transform as RectTransform).anchorMax = new Vector2(0.5f, 1);

            ReloadPluginList();
        }

        [UIAction("list-select")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void OnListSelect(TableView table, int index)
        {
            if (index < 0 || index >= PluginList.Count)
            {
                ReloadViewList();
                return;
            }

            DidSelectPlugin?.Invoke(PluginList[index]);
        }

        private void ReloadPluginList()
        {
            PluginList.Clear();
            PluginList.AddRange(
                        PluginManager.EnabledPlugins .Where(m => !m.IsBare).AsInfos(PluginState.Enabled)
                .Concat(PluginManager.DisabledPlugins.Where(m => !m.IsBare).AsInfos(PluginState.Disabled))
                .Concat(PluginManager.EnabledPlugins .Where(m =>  m.IsBare).AsInfos(PluginState.Enabled))
                .Concat(PluginManager.DisabledPlugins.Where(m =>  m.IsBare).AsInfos(PluginState.Disabled))
                .Concat(PluginManager.IgnoredPlugins.Keys.AsInfos(PluginState.Ignored)));
            ReloadViewList();
        }

        private void ReloadViewList()
        {
            ListValues.Clear();
            ListValues.AddRange(
                PluginList.Select(p => 
                    new CustomListTableData.CustomCellInfo(
                        p.Plugin.Name,
                        $"{p.Plugin.Author} <size=80%>{p.Plugin.Version}</size>", 
                        Helpers.ReadPluginIcon(p),
                        Enumerable.Empty<Sprite>()
                            .MaybeAppend(p.Plugin.IsBare, Helpers.LibrarySprite)
                            .MaybeAppend(p.State == PluginState.Disabled, Helpers.XSprite)
                            .MaybeAppend(p.State == PluginState.Enabled 
                                      && p.Plugin.RuntimeOptions == RuntimeOptions.DynamicInit, Helpers.OSprite))));

            customListTableData?.tableView?.ReloadData();
        }
    }
}
