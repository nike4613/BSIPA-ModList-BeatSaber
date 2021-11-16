using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.ModList.BeatSaber.Models;
using IPA.ModList.BeatSaber.Services;
using IPA.ModList.BeatSaber.Utilities;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace IPA.ModList.BeatSaber.UI.ViewControllers
{
    [HotReload(RelativePathToLayout = @".\ModListView.bsml")]
    internal class ModListViewController : BSMLAutomaticViewController
    {
        private SiraLog siraLog = null!;
        private ModProviderService modProviderService = null!;

        private List<object> ListValues { get; } = new List<object>();


        [Inject]
        internal void Construct(SiraLog siraLog, ModProviderService modProviderService)
        {
            this.siraLog = siraLog;
            this.modProviderService = modProviderService;
        }

        internal event Action<PluginInformation>? DidSelectPlugin;

        [UIComponent("list")]
        internal CustomCellListTableData CustomListTableData = null!;

        [UIAction("list-select")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void OnListSelect(TableView _, object @object)
        {
            var index = ListValues.IndexOf(@object);

            DidSelectPlugin?.Invoke(modProviderService.PluginList[index]);
        }

        [UIAction("#post-parse")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void Setup()
        {
            var stoppyWatch = new System.Diagnostics.Stopwatch();
            stoppyWatch.Start();

            if (CustomListTableData != null)
            {
                CustomListTableData.data = ListValues;
            }

            stoppyWatch.Stop();
            siraLog.Warn($"ModListViewController post-parse setup took {stoppyWatch.Elapsed:c}");
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            if (CustomListTableData != null && CustomListTableData.data?.Count != modProviderService.PluginList.Count)
            {
                ReloadViewList();
            }
        }

        internal void ReloadViewList()
        {
            ListValues.Clear();
            ListValues.AddRange(modProviderService.PluginList.Select(p =>
                new PluginCellViewController(
                    p.Plugin.Name,
                    $"{p.Plugin.Author} <size=80%>{p.Plugin.HVersion}</size>",
                    p.Icon,
                    Enumerable.Empty<Sprite>()
                              .AppendIf(p.Plugin.IsBare, Helpers.LibrarySprite)
                              .AppendIf(p.State == PluginState.Disabled, Helpers.XSprite)
                              .AppendIf(p.State == PluginState.Enabled && p.Plugin.RuntimeOptions == RuntimeOptions.DynamicInit, Helpers.OSprite)
                              .AppendIf(p.State == PluginState.Ignored, Helpers.WarnSprite))));

            if (CustomListTableData != null && CustomListTableData.tableView != null)
            {
                CustomListTableData.tableView.ReloadData();
            }
        }
    }
}