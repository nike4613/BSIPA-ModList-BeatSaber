using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using System;
using System.Collections;
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
            (transform as RectTransform).localScale = new Vector2(0, 0);
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            
            Setup();

            CurrentChange = null;
        }

        private Queue<ChangeQueueItem> changeQueue = new Queue<ChangeQueueItem>();

        internal class ChangeQueueItem
        {
            public PluginInformation Plugin { get; }

            public string ChangeType { get; }

            [UIValue("pre-text")]
            public string PreText
                => $"To {ChangeType} {Plugin?.Plugin?.Name}, the following plugins must also be {ChangeType}d:";
            [UIValue("post-text")]
            public string PostText
                => $"Are you sure you want to {ChangeType} this plugin?";

            public IReadOnlyList<string> LineEntries { get; }

            [UIValue("lines")]
            internal IEnumerable<LineItem> Lines => LineEntries?.Select(s => new LineItem { Value = s });

            internal struct LineItem
            {
                [UIValue("value")]
                public string Value;
            }

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
        internal ChangeQueueItem CurrentChange { get; private set; } = null;

        [UIValue("current-change-pre-text")]
        internal string CurrentChangePreText => CurrentChange?.PreText;
        [UIValue("current-change-post-text")]
        internal string CurrentChangePostText => CurrentChange?.PostText;
        [UIValue("current-change-lines")]
        internal IEnumerable<ChangeQueueItem.LineItem> CurrentChangeLines 
            => CurrentChange?.Lines ?? Enumerable.Empty<ChangeQueueItem.LineItem>();

        [UIComponent("change-modal")]
        internal ModalView ChangeModal;

        private void RefreshChangeItem()
        {
            IEnumerator Coro()
            {
                yield return null;
                NotifyPropertyChanged(nameof(CurrentChange));
                NotifyPropertyChanged(nameof(CurrentChangePreText));
                NotifyPropertyChanged(nameof(CurrentChangePostText));
                NotifyPropertyChanged(nameof(CurrentChangeLines));
            }

            StartCoroutine(Coro());
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
            if (changeQueue.Peek() == null) return false;

            Logger.log.Debug("Presenting change");

            CurrentChange = changeQueue.Dequeue();
            RefreshChangeItem();
            ChangeModal.Show(true);
            return true;
        }
    }
}
