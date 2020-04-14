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
using UnityEngine;

namespace IPA.ModList.BeatSaber.UI
{
    [HotReload(PathMap = new[] { "C:\\", CompileConstants.SolutionDirectory })]
    internal class ModListViewController : BSMLAutomaticViewController, INotifiableHost
    {
        private List<CustomListTableData.CustomCellInfo> ListValues { get; } = new List<CustomListTableData.CustomCellInfo>();

        [UIComponent("list")]
        internal CustomListTableData customListTableData;

        private List<PluginMetadata> PluginList { get; } = new List<PluginMetadata>();

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            Logger.log.Debug("Test VC activated");

            customListTableData.data = ListValues;
        }

        [UIAction("#post-parse")]
        internal void SetupList()
        {
            (transform as RectTransform).sizeDelta = new Vector2(70, 0);
            (transform as RectTransform).anchorMin = new Vector2(0.5f, 0);
            (transform as RectTransform).anchorMax = new Vector2(0.5f, 1);

            ReloadPluginList();
            ReloadViewList();
        }

        private void ReloadPluginList()
        {
            PluginList.Clear();
            PluginList.AddRange(
                        PluginManager.EnabledPlugins .Where(m => !m.IsBare)
                .Concat(PluginManager.DisabledPlugins.Where(m => !m.IsBare))
                .Concat(PluginManager.EnabledPlugins .Where(m =>  m.IsBare))
                .Concat(PluginManager.DisabledPlugins.Where(m =>  m.IsBare))
                .Concat(PluginManager.IgnoredPlugins.Keys));
        }

        private void ReloadViewList()
        {
            ListValues.Clear();
            ListValues.AddRange(
                PluginList.Select(m => 
                    new CustomListTableData.CustomCellInfo(
                        m.Name,
                        $"{m.Author} <size=80%>{m.Version}</size>", 
                        Helpers.ReadPluginIcon(m))));

            customListTableData?.tableView?.ReloadData();
        }
    }
}
