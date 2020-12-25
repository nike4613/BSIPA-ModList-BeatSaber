using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.ModList.BeatSaber.Helpers;
using IPA.ModList.BeatSaber.Models;
using IPA.ModList.BeatSaber.Services;
using SiraUtil.Tools;
using UnityEngine;
using Zenject;

namespace IPA.ModList.BeatSaber.UI.ViewControllers
{
    [HotReload(RelativePathToLayout = @"..\Views\ModListView.bsml")]
    [ViewDefinition("IPA.ModList.BeatSaber.UI.Views.ModListView.bsml")]
    internal class ModListViewController : BSMLAutomaticViewController
    {
        private SiraLog _siraLog = null!;
        private ModProviderService _modProviderService = null!;

        private List<object> ListValues { get; } = new List<object>();


        [Inject]
        internal void Construct(SiraLog siraLog, ModProviderService modProviderService)
        {
            _siraLog = siraLog;
            _modProviderService = modProviderService;
        }

        internal event Action<PluginInformation>? DidSelectPlugin;

        [UIComponent("list")]
        internal CustomCellListTableData customListTableData = null!;

        [UIAction("list-select")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void OnListSelect(TableView _, object @object)
        {
            var index = ListValues.IndexOf(@object);

            DidSelectPlugin?.Invoke(_modProviderService.PluginList[index]);
        }

        [UIAction("#post-parse")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void Setup()
        {
            var stoppyWatch = new System.Diagnostics.Stopwatch();
            stoppyWatch.Start();

            if (customListTableData != null)
            {
                customListTableData.data = ListValues;
            }

            stoppyWatch.Stop();
            _siraLog.Warning($"ModListViewController post-parse setup took {stoppyWatch.Elapsed:c}");
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            if (customListTableData != null && customListTableData.data?.Count != _modProviderService.PluginList.Count)
            {
                ReloadViewList();
            }
        }

        internal void ReloadViewList()
        {
            ListValues.Clear();
            ListValues.AddRange(_modProviderService.PluginList.Select(p =>
                new PluginCellViewController(
                    p.Plugin.Name,
                    $"{p.Plugin.Author} <size=80%>{p.Plugin.Version}</size>",
                    p.Icon,
                    Enumerable.Empty<Sprite>()
                        .AppendIf(p.Plugin.IsBare, Helpers.Helpers.LibrarySprite)
                        .AppendIf(p.State == PluginState.Disabled, Helpers.Helpers.XSprite)
                        .AppendIf(p.State == PluginState.Enabled && p.Plugin.RuntimeOptions == RuntimeOptions.DynamicInit, Helpers.Helpers.OSprite)
                        .AppendIf(p.State == PluginState.Ignored, Helpers.Helpers.WarnSprite))));
            /*PluginList.Select(p =>
                new CustomListTableData.CustomCellInfo(
                    p.Plugin.Name,
                    $"{p.Plugin.Author} <size=80%>{p.Plugin.Version}</size>",
                    p.Icon.AsSprite(),
                    Enumerable.Empty<Sprite>()
                        .AppendIf(p.Plugin.IsBare, Helpers.LibrarySprite)
                        .AppendIf(p.State == PluginState.Disabled, Helpers.XSprite)
                        .AppendIf(p.State == PluginState.Enabled && p.Plugin.RuntimeOptions == RuntimeOptions.DynamicInit, Helpers.OSprite)
                        .AppendIf(p.State == PluginState.Ignored, Helpers.WarnSprite))));*/

            if (customListTableData != null && customListTableData.tableView != null)
            {
                customListTableData.tableView.ReloadData();
            }
        }
    }
}