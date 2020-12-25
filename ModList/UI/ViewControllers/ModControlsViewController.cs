using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using IPA.Loader;
using IPA.ModList.BeatSaber.Models;
using IPA.ModList.BeatSaber.Services;
using ModestTree;
using SiraUtil.Tools;
using Zenject;

namespace IPA.ModList.BeatSaber.UI.ViewControllers
{
    [HotReload(RelativePathToLayout = @"..\Views\ModControlsView.bsml")]
    [ViewDefinition("IPA.ModList.BeatSaber.UI.Views.ModControlsView.bsml")]
    internal class ModControlsViewController : BSMLAutomaticViewController
    {
        private PluginInformation? plugin;
        private readonly Dictionary<PluginInformation, PluginState> changedStates = new Dictionary<PluginInformation, PluginState>();

        private SiraLog siraLog = null!;
        private ModProviderService modProviderService = null!;

        internal StateTransitionTransaction? CurrentTransaction { get; set; }


        [Inject]
        internal void Construct(SiraLog siraLog, ModProviderService modProviderService)
        {
            this.modProviderService = modProviderService;
            this.siraLog = siraLog;
        }

        internal void SetPlugin(PluginInformation info)
        {
            plugin = info;
            PanelActive = info.State != PluginState.Ignored;

            RefreshModInfo();
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            RefreshChanges();
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            CurrentTransaction?.Dispose();
            CurrentTransaction = null;

            changedStates.Clear();

            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            PanelActive = false;
        }

        [UIParams]
        internal BSMLParserParams ParserParams = null!;

        [UIValue("panel-active")]
        internal bool PanelActive { get; private set; } = false;

        [UIValue("mod-requires-restart")]
        internal bool ModRequiresRestart => plugin?.Plugin?.RuntimeOptions != RuntimeOptions.DynamicInit;

        private void RefreshModInfo()
        {
            NotifyPropertyChanged(nameof(PanelActive));
            NotifyPropertyChanged(nameof(ModRequiresRestart));

            RefreshEnabled();
        }

        [UIValue("mod-enabled")]
        internal bool ModEnabled => plugin?.State == PluginState.Enabled;

        [UIValue("mod-disabled")]
        internal bool ModDisabled => !ModEnabled;

        private void RefreshEnabled()
        {
            NotifyPropertyChanged(nameof(ModEnabled));
            NotifyPropertyChanged(nameof(ModDisabled));
        }

        internal event Action? OnListNeedsRefresh;
        private void RefreshList() => OnListNeedsRefresh?.Invoke();


        [UIValue("changing-mods")]
        internal bool ChangingMods => CurrentTransaction != null;

        [UIValue("changed-count")]
        internal int ChangedCount => changedStates.Count;

        [UIValue("requires-restart")]
        internal bool ChangesRequireRestart => CurrentTransaction?.WillNeedRestart ?? true;

        private void RefreshChanges()
        {
            NotifyPropertyChanged(nameof(ChangingMods));
            NotifyPropertyChanged(nameof(ChangedCount));
            NotifyPropertyChanged(nameof(ChangesRequireRestart));
        }

        private const string EnableType = "enable";
        private const string DisableType = "disable";

        internal delegate void ChangeNeedsConfirmationEvent(PluginInformation plugin, string type, IEnumerable<string> lines, Action<bool> completion);

        internal event ChangeNeedsConfirmationEvent? OnChangeNeedsConfirmation;

        [UIAction(nameof(EnableMod))]
        public void EnableMod()
        {
            var transaction = StartOrGetTransaction();

            if (transaction.Enable(plugin!.Plugin, out var deps))
            {
                // ChangedCount++;
            }
            else
            {
                // this should only happen when we need to turn on deps
                if (deps != null)
                {
                    // we have deps that need to be processed
                    // TODO: send this off somewhere to present to the user
                    foreach (var dep in deps)
                    {
                        siraLog.Debug($"needs {dep.Name}");
                    }

                    OnChangeNeedsConfirmation?.Invoke(plugin, EnableType, BuildMetadataLines(deps),
                        GetEnableConfirmCallback(plugin, deps, transaction, () => StartCoroutine(RefreshOnModStateChange())));

                    return;
                }

                siraLog.Debug($"{plugin.Plugin.Name} already enabled");
            }

            UpdatePluginTo(plugin, PluginState.Enabled);
            StartCoroutine(RefreshOnModStateChange());
        }

        private Action<bool> GetEnableConfirmCallback(PluginInformation plugin, IEnumerable<PluginMetadata> deps, StateTransitionTransaction transaction, Action? afterConfirm = null)
        {
            IEnumerator<(PluginInformation plugin, string type, IEnumerable<string> lines, Action<bool> completion)> StateMachine()
            {
                // this is first called *after* the initial confirmation
                // here, we want to go throught the deps and try enabling or request a confirmation for it

                foreach (var dep in deps)
                {
                    var depInfo = GetPluginInformation(dep);

                    if (transaction.Enable(dep, out var depDeps))
                    {
                        UpdatePluginTo(depInfo, PluginState.Enabled);
                    }
                    else
                    {
                        if (depDeps == null)
                        {
                            siraLog.Warning($"{dep.Name} is already enabled; how did we get here?");
                        }
                        else
                        {
                            yield return (depInfo, EnableType, BuildMetadataLines(depDeps), GetEnableConfirmCallback(depInfo, depDeps, transaction));
                        }
                    }
                }

                // once we've processed everything, we're done, we can enable our thing now
                transaction.Enable(plugin.Plugin, true);
                UpdatePluginTo(plugin, PluginState.Enabled);

                afterConfirm?.Invoke();
            }

            return CreateConfirmationStateMachineExecutor(StateMachine());
        }

        [UIAction(nameof(DisableMod))]
        public void DisableMod()
        {
            var transaction = StartOrGetTransaction();

            if (transaction.Disable(plugin!.Plugin, out var users))
            {
                // ChangedCount++;
            }
            else
            {
                // this should only happen when we need to turn off users
                if (users != null)
                {
                    // TODO: send this off somewhere to present to the user
                    foreach (var dep in users)
                    {
                        siraLog.Debug($"needs {dep.Name}");
                    }

                    OnChangeNeedsConfirmation?.Invoke(plugin, DisableType, BuildMetadataLines(users),
                        GetDisableConfirmCallback(plugin, users, transaction, () => StartCoroutine(RefreshOnModStateChange())));

                    return;
                }

                siraLog.Logger.Notice($"{plugin.Plugin.Name} already disabled");
            }

            UpdatePluginTo(plugin, PluginState.Disabled);
            StartCoroutine(RefreshOnModStateChange());
        }

        private Action<bool> GetDisableConfirmCallback(PluginInformation plugin, IEnumerable<PluginMetadata> deps, StateTransitionTransaction transaction, Action? afterConfirm = null)
        {
            IEnumerator<(PluginInformation plugin, string type, IEnumerable<string> lines, Action<bool> completion)> StateMachine()
            {
                // this is first called *after* the initial confirmation
                // here, we want to go throught the deps and try enabling or request a confirmation for it

                foreach (var dep in deps)
                {
                    var depInfo = GetPluginInformation(dep);

                    if (transaction.Disable(dep, out var depDeps))
                    {
                        UpdatePluginTo(depInfo, PluginState.Disabled);
                    }
                    else
                    {
                        if (depDeps == null)
                        {
                            siraLog.Warning($"{dep.Name} is already enabled; how did we get here?");
                        }
                        else
                        {
                            yield return (depInfo, DisableType, BuildMetadataLines(depDeps), GetDisableConfirmCallback(depInfo, depDeps, transaction));
                        }
                    }
                }

                // once we've processed everything, we're done, we can enable our thing now
                transaction.Disable(plugin.Plugin, true);
                UpdatePluginTo(plugin, PluginState.Disabled);

                afterConfirm?.Invoke();
            }

            return CreateConfirmationStateMachineExecutor(StateMachine());
        }

        private PluginInformation GetPluginInformation(PluginMetadata dep)
        {
            return modProviderService.PluginList.First(x => x.Plugin.Id == dep.Id);
        }

        private void UpdatePluginTo(PluginInformation plugin, PluginState newState)
        {
            /*ChangedCount++;

            if (!_changedStates.ContainsKey(plugin))
            {
                _changedStates.Add(plugin, plugin.State);
            }*/

            if (changedStates.TryGetValue(plugin, out var originalState))
            {
                if (originalState == newState)
                {
                    changedStates.Remove(plugin);
                }
            }
            else
            {
                changedStates.Add(plugin, plugin.State);
            }

            plugin.State = newState;

            Assert.IsEqual(plugin.State, modProviderService.PluginList.First(x => x.Plugin.Id == plugin.Plugin.Id).State);
            RefreshList();
        }

        private IEnumerable<string> BuildMetadataLines(IEnumerable<PluginMetadata> plugins)
        {
            return plugins.Select(plugin => plugin.Name);
        }

        private Action<bool> CreateConfirmationStateMachineExecutor(IEnumerator<(PluginInformation plugin, string type, IEnumerable<string> lines, Action<bool> completion)> stateMachine,
            Action? finished = null)
        {
            void ContinueAction(bool confirmed)
            {
                if (!confirmed)
                {
                    stateMachine.Dispose();
                    return; // exit the sequence
                }

                try
                {
                    if (stateMachine.MoveNext())
                    {
                        var (plugin, type, lines, completion) = stateMachine.Current;
                        OnChangeNeedsConfirmation?.Invoke(plugin, type, lines, succ =>
                        {
                            completion?.Invoke(succ);
                            ContinueAction(succ);
                        });

                        return; // once we queue the confirmation, we want to exit
                    }
                }
                catch
                {
                    // we only want to *always* dispose if we catch here
                    stateMachine.Dispose();
                    throw;
                }

                // we've finished, so we can dispose stateMachine and invoke the finished callback
                finished?.Invoke();
                stateMachine.Dispose();
            }

            return ContinueAction;
        }

        private IEnumerator RefreshOnModStateChange()
        {
            yield return null;
            RefreshChanges();
            RefreshEnabled();
        }

        private StateTransitionTransaction StartOrGetTransaction()
        {
            CurrentTransaction ??= PluginManager.PluginStateTransaction();
            RefreshChanges();
            return CurrentTransaction;
        }
    }
}