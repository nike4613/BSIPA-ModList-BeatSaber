using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.ModList.BeatSaber.Models;
using IPA.ModList.BeatSaber.Services;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace IPA.ModList.BeatSaber.UI.ViewControllers
{
    [HotReload(RelativePathToLayout = @".\ModListView.bsml")]
    internal class ModListViewController : BSMLAutomaticViewController, TableView.IDataSource
    {
        private SiraLog siraLog = null!;
        private ModProviderService modProviderService = null!;

        private List<PluginInformation> ListValues = new List<PluginInformation>();

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
        internal CustomListTableData CustomListTableData = null!;

        [UIAction("list-select")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void OnListSelect(TableView _, int index)
        {
            DidSelectPlugin?.Invoke(ListValues[index]);
        }

        [UIAction("#post-parse")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void Setup()
        {
            var stoppyWatch = new System.Diagnostics.Stopwatch();
            stoppyWatch.Start();

            if (CustomListTableData != null)
            {
                CustomListTableData.tableView.SetDataSource(this, false);
            }

            stoppyWatch.Stop();
            siraLog.Warn($"ModListViewController post-parse setup took {stoppyWatch.Elapsed:c}");
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            if (ListValues.Count != modProviderService.PluginList.Count)
            {
                Loaded = false;
            }
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            CustomListTableData.tableView.ClearSelection();
            DidSelectPlugin?.Invoke(null);
        }

        internal async void OnAnimationFinish()
        {
            if (ListValues.Count != modProviderService.PluginList.Count)
            {
                await SiraUtil.Extras.Utilities.PauseChamp;
                ReloadViewList();
                Loaded = true;
            }
        }

        internal void ReloadViewList()
        {
            ListValues.Clear();
            ListValues.AddRange(modProviderService.PluginList);
            CustomListTableData.tableView.ReloadData();
        }

        #region TableData

        public const string ReuseIdentifier = "ModListCell";
        private PluginCellViewController GetCell()
        {
            var tableCell = CustomListTableData.tableView.DequeueReusableCellForIdentifier(ReuseIdentifier);

            if (tableCell == null)
            {
                tableCell = new GameObject(nameof(PluginCellViewController), new[] { typeof(Touchable) }).AddComponent<PluginCellViewController>();
                tableCell.interactable = true;

                tableCell.reuseIdentifier = ReuseIdentifier;
                BSMLParser.instance.Parse(
                BeatSaberMarkupLanguage.Utilities.GetResourceContent(
                    Assembly.GetExecutingAssembly(),
                    "IPA.ModList.BeatSaber.UI.ViewControllers.PluginCellViewController.bsml"),
                tableCell.gameObject,
                tableCell);
            }

            return (PluginCellViewController) tableCell;
        }

        public float CellSize() => 8;
        public int NumberOfCells() => ListValues.Count;
        public TableCell CellForIdx(TableView tableView, int idx) => GetCell().PopulateCell(ListValues[idx]);

        #endregion
    }
}
