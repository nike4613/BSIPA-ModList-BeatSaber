using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
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

        private bool _loaded;

        [UIValue("is-loading")]
        public bool IsLoading => !Loaded;

        [UIValue("loaded")]
        public bool Loaded
        {
            get => _loaded;
            set
            {
                _loaded = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsLoading));
            }
        }

        [Inject]
        internal void Construct(SiraLog siraLog, ModProviderService modProviderService)
        {
            this.siraLog = siraLog;
            this.modProviderService = modProviderService;
        }

        internal event Action<PluginInformation?>? DidSelectPlugin;

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
            Loaded = false;
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            CustomListTableData.tableView.ClearSelection();
            DidSelectPlugin?.Invoke(null);
        }

        internal async void OnAnimationFinish()
        {
            if (CustomListTableData != null && CustomListTableData.data?.Count != modProviderService.PluginList.Count)
            {
                await SiraUtil.Extras.Utilities.PauseChamp;
                ReloadViewList();
            }
            Loaded = true;
        }

        internal void ReloadViewList()
        {
            ListValues.Clear();
            ListValues.AddRange(modProviderService.PluginList.Select(p => new PluginCellViewController(p)));

            if (CustomListTableData != null && CustomListTableData.tableView != null)
            {
                CustomListTableData.tableView.ReloadData();
            }
        }
    }
}
