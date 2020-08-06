using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using IPA.Loader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.ModList.BeatSaber.UI
{
    [HotReload(PathMap = new[] { "C:\\", CompileConstants.SolutionDirectory })]
    public class ModControlsViewController : BSMLAutomaticViewController
    {
        private PluginInformation plugin = null;
        private Dictionary<PluginInformation, PluginState> changedStates = new Dictionary<PluginInformation, PluginState>();

        internal void SetPlugin(PluginInformation info)
        {
            plugin = info;
            PanelActive = info.State != PluginState.Ignored;

            RefreshModInfo();
        }

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            ChangedCount = 0;

            RefreshChanges();
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            currentTransaction?.Dispose();
            currentTransaction = null;

            foreach (var kvp in changedStates)
                kvp.Key.State = kvp.Value;
            changedStates.Clear();

            base.DidDeactivate(deactivationType);

            PanelActive = false;
        }

        [UIParams]
        internal BSMLParserParams ParserParams;

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

        internal event Action OnListNeedsRefresh;
        private void RefreshList() => OnListNeedsRefresh?.Invoke();


        [UIValue("changing-mods")]
        internal bool ChangingMods => currentTransaction != null;

        [UIValue("changed-count")]
        internal int ChangedCount { get; private set; } = 0;

        [UIValue("requires-restart")]
        internal bool ChangesRequireRestart => currentTransaction?.WillNeedRestart ?? true;

        private void RefreshChanges()
        {
            NotifyPropertyChanged(nameof(ChangingMods));
            NotifyPropertyChanged(nameof(ChangedCount));
            NotifyPropertyChanged(nameof(ChangesRequireRestart));
        }

        private StateTransitionTransaction currentTransaction = null;

        internal delegate void ChangeNeedsConfirmationEvent(PluginInformation plugin, string type, IEnumerable<string> lines, Action<bool> completion);
        internal event ChangeNeedsConfirmationEvent OnChangeNeedsConfirmation;

        [UIAction(nameof(EnableMod))]
        public void EnableMod()
        {
            var transaction = StartOrGetTransaction();

            if (transaction.Enable(plugin.Plugin, out var deps))
            {
                ChangedCount++;
            }
            else
            { // this should only happen when we need to turn on deps
                if (deps != null)
                { // we have deps that need to be processed
                     // TODO: send this off somewhere to present to the user
                    foreach (var dep in deps)
                    {
                        Logger.log.Debug($"needs {dep.Name}");
                    }

                    OnChangeNeedsConfirmation(plugin, "enable", BuildMetadataLines(deps),
                        GetEnableConfirmCallback(plugin, deps, transaction));

                    return;
                }
                else
                {
                    Logger.log.Notice($"{plugin.Plugin.Name} already enabled");
                }
            }

            if (!changedStates.ContainsKey(plugin))
                changedStates.Add(plugin, plugin.State);
            plugin.State = PluginState.Enabled;
            StartCoroutine(RefreshOnModStateChange());
            RefreshList();
        }

        private Action<bool> GetEnableConfirmCallback(PluginInformation plugin, IEnumerable<PluginMetadata> deps, StateTransitionTransaction transaction, Action afterConfirm = null)
        {
            IEnumerator<(PluginInformation plugin, string type, IEnumerable<string> lines)> StateMachine()
            {
                // this is first called *after* the initial confirmation


                yield break;
            }

            return CreateConfirmationStateMachineExecutor(StateMachine(), () =>
            {

                afterConfirm?.Invoke();
            });
        }

        [UIAction(nameof(DisableMod))]
        public void DisableMod()
        {
            var transaction = StartOrGetTransaction();

            if (transaction.Disable(plugin.Plugin, out var users))
            {
                ChangedCount++;
            }
            else
            { // this should only happen when we need to turn off users
                if (users != null)
                {
                    // TODO: send this off somewhere to present to the user
                    foreach (var dep in users)
                    {
                        Logger.log.Debug($"needs {dep.Name}");
                    }

                    return;
                }
                else
                {
                    Logger.log.Notice($"{plugin.Plugin.Name} already disabled");
                }
            }

            if (!changedStates.ContainsKey(plugin))
                changedStates.Add(plugin, plugin.State);
            plugin.State = PluginState.Disabled;
            StartCoroutine(RefreshOnModStateChange());
            RefreshList();
        }

        private IEnumerable<string> BuildMetadataLines(IEnumerable<PluginMetadata> plugins)
        {
            foreach (var plugin in plugins)
            {
                yield return plugin.Name;
            }
        }

        private Action<bool> CreateConfirmationStateMachineExecutor(IEnumerator<(PluginInformation plugin, string type, IEnumerable<string> lines)> stateMachine, Action finished = null)
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
                        var nextInvocation = stateMachine.Current;
                        OnChangeNeedsConfirmation(nextInvocation.plugin, nextInvocation.type, nextInvocation.lines, ContinueAction);
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
            };

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
            currentTransaction ??= PluginManager.PluginStateTransaction();
            RefreshChanges();
            return currentTransaction;
        }
    }
}
