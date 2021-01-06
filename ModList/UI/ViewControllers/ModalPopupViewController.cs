using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using HMUI;
using IPA.ModList.BeatSaber.Models;
using JetBrains.Annotations;
using SiraUtil.Tools;
using UnityEngine;

namespace IPA.ModList.BeatSaber.UI.ViewControllers
{
    internal class ModalPopupViewController : INotifyPropertyChanged
    {
        private readonly SiraLog siraLog;

        internal ModalPopupViewController(SiraLog siraLog)
        {
            this.siraLog = siraLog;
        }

        internal void SetData(GameObject parentGo)
        {
            ParserParams = BSMLParser.instance.Parse(
                BeatSaberMarkupLanguage.Utilities.GetResourceContent(
                    Assembly.GetExecutingAssembly(),
                    "IPA.ModList.BeatSaber.UI.Views.ModalPopupView.bsml"),
                parentGo,
                this);
        }

        [UIParams]
        internal BSMLParserParams ParserParams = null!;

        private readonly Queue<ChangeQueueItem> changeQueue = new Queue<ChangeQueueItem>();

        internal class ChangeQueueItem
        {
            public PluginInformation Plugin { get; }

            public string ChangeType { get; }

            [UIValue("pre-text")]
            public string PreText => $"To {ChangeType} {Plugin?.Plugin?.Name}, the following plugins must also be {ChangeType}d:";

            [UIValue("post-text")]
            public string PostText => $"Are you sure you want to {ChangeType} this plugin?";

            public IReadOnlyList<string> LineEntries { get; }

            public Action<bool>? OnCompletion { get; }

            public ChangeQueueItem(PluginInformation plugin, string changeType, IEnumerable<string> lines, Action<bool> onComplete)
            {
                Plugin = plugin;
                ChangeType = changeType;
                LineEntries = lines.ToList();
                OnCompletion = onComplete;
            }
        }

        public void QueueChange(PluginInformation plugin, string type, IEnumerable<string> lines, Action<bool> completion)
        {
            siraLog.Debug($"Change queued: {plugin.Plugin.Name} {type} {string.Join(",", lines)}");
            changeQueue.Enqueue(new ChangeQueueItem(plugin, type, lines, completion));
            TryProcessNextChange();
        }

        [UIValue("current-change")]
        internal ChangeQueueItem? CurrentChange { get; private set; }

        [UIValue("current-change-pre-text")]
        internal string? CurrentChangePreText => CurrentChange?.PreText;

        [UIValue("current-change-post-text")]
        internal string? CurrentChangePostText => CurrentChange?.PostText;

        [UIValue("has-current-change-lines")]
        internal bool HasCurrentChangeLines => CurrentChange?.LineEntries.Any() ?? false;

        [UIValue("current-change-lines")]
        internal string CurrentChangeLines
            => CurrentChange?.LineEntries.Aggregate("<color=\"yellow\">", (accumulator, modName) => $"{accumulator}{modName}\n") ?? string.Empty;

        [UIComponent("change-modal")]
        internal ModalView ChangeModal = null!;

        private void RefreshChangeItem()
        {
            OnPropertyChanged(nameof(CurrentChange));
            OnPropertyChanged(nameof(CurrentChangePreText));
            OnPropertyChanged(nameof(CurrentChangePostText));
            OnPropertyChanged(nameof(HasCurrentChangeLines));
            OnPropertyChanged(nameof(CurrentChangeLines));
        }

        [UIAction(nameof(ConfirmChange))]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void ConfirmChange()
        {
            siraLog.Debug("Confirmed");
            CurrentChange?.OnCompletion?.Invoke(true);

            FinishCurrentChange();
        }

        [UIAction(nameof(DenyChange))]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void DenyChange()
        {
            siraLog.Debug("Declined");
            CurrentChange?.OnCompletion?.Invoke(false);

            FinishCurrentChange();
        }

        private void FinishCurrentChange()
        {
            ChangeModal.Hide(true, () =>
            {
                siraLog.Debug("Hiding change");

                CurrentChange = null;
                TryProcessNextChange();
            });
        }

        private void TryProcessNextChange()
        {
            if (CurrentChange != null || changeQueue.Count == 0 || changeQueue.Peek() == null)
            {
                return;
            }

            siraLog.Debug("Presenting change");

            CurrentChange = changeQueue.Dequeue();
            RefreshChangeItem();

            // Why are Unity and/or modals like this D:
            // Setting the animated parameter to true, prevents it from appearing a second time...
            ChangeModal.Show(false, true, () => siraLog.Debug("Change is being presented"));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}