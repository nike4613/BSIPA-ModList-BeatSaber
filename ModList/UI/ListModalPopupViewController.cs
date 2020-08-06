using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace IPA.ModList.BeatSaber.UI
{
    [HotReload(PathMap = new[] { "C:\\", CompileConstants.SolutionDirectory })]
    internal class ListModalPopupViewController : BSMLAutomaticViewController
    {
        [UIParams]
        internal BSMLParserParams ParserParams;

        [UIAction("#post-parse")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void Setup()
        {
            (transform as RectTransform).sizeDelta = new Vector2(0, 0);
            (transform as RectTransform).anchorMin = new Vector2(0, 0);
            (transform as RectTransform).anchorMax = new Vector2(0, 0);
        }

        private Queue<ChangeQueueItem> changeQueue = new Queue<ChangeQueueItem>();

        internal class ChangeQueueItem
        {
            public PluginInformation Plugin { get; }

            [UIValue("plugin-name")]
            internal string PluginName => Plugin.Plugin.Name;

            [UIValue("change-type")]
            public string ChangeType { get; }

            [UIValue("lines")]
            public IReadOnlyList<string> LineEntries { get; }

            public Action<bool> OnCompletion { get; }

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
            Logger.log.Debug($"Change queued: {plugin.Plugin.Name} {type} {string.Join(",", lines)}");
            changeQueue.Enqueue(new ChangeQueueItem(plugin, type, lines, completion));
            TryProcessNextChange();
        }

        [UIValue("current-change")]
        internal ChangeQueueItem CurrentChange { get; private set; }

        [UIComponent("change-modal")]
        internal ModalView ChangeModal;

        private void RefreshChangeItem()
        {
            NotifyPropertyChanged(nameof(CurrentChange));
        }

        [UIAction(nameof(ConfirmChange))]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void ConfirmChange()
        {
            Logger.log.Debug("Confirmed");
            CurrentChange.OnCompletion?.Invoke(true);

            FinishCurrentChange();
        }

        [UIAction(nameof(DenyChange))]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "BSML calls this")]
        private void DenyChange()
        {
            Logger.log.Debug("Declined");
            CurrentChange.OnCompletion?.Invoke(false);

            FinishCurrentChange();
        }

        private void FinishCurrentChange()
        {
            ChangeModal.Hide(true, () =>
            {
                CurrentChange = null;
                TryProcessNextChange();
            });
        }

        private bool TryProcessNextChange()
        {
            if (CurrentChange != null) return false;
            if (!changeQueue.TryDequeue(out var item)) return false;

            Logger.log.Debug("Presenting change");

            CurrentChange = item;
            RefreshChangeItem();
            ChangeModal.Show(true);
            return true;
        }
    }
}
