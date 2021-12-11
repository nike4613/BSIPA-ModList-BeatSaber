using System;
using System.Linq;
using BeatSaberMarkupLanguage;
using HMUI;
using IPA.Loader;
using IPA.ModList.BeatSaber.Models;
using IPA.ModList.BeatSaber.UI.ViewControllers;
using SiraUtil.Logging;
using SiraUtil.Zenject;
using Zenject;

namespace IPA.ModList.BeatSaber.UI
{
    internal class ModListFlowCoordinator : FlowCoordinator
    {
        private SiraLog siraLog = null!;
        private string modName = string.Empty;

        private MenuTransitionsHelper menuTransitionsHelper = null!;

        private ModListNavigationController navigationController = null!;
        private ModListViewController modListViewController = null!;
        private ModInfoViewController modInfoViewController = null!;
        private ModControlsViewController modControlsViewController = null!;
        private ModalPopupViewController modalPopupViewController = null!;

        [Inject]
        internal void Construct(SiraLog siraLog, UBinder<Plugin, PluginMetadata> pluginMetadata, ModListNavigationController navigationController, ModListViewController modListViewController,
            ModInfoViewController modInfoViewController, ModControlsViewController modControlsViewController, ModalPopupViewController modalPopupViewController,
            MenuTransitionsHelper menuTransitionsHelper)
        {
            this.siraLog = siraLog;
            modName = pluginMetadata.Value.Name;

            this.navigationController = navigationController;
            this.modListViewController = modListViewController;
            this.modInfoViewController = modInfoViewController;
            this.modControlsViewController = modControlsViewController;
            this.modalPopupViewController = modalPopupViewController;

            this.menuTransitionsHelper = menuTransitionsHelper;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (firstActivation)
                {
                    SetTitle(modName);
                    showBackButton = true;
                    SetViewControllersToNavigationController(navigationController, modListViewController, modInfoViewController);
                    ProvideInitialViewControllers(navigationController);

                    modalPopupViewController.SetData(navigationController.gameObject);
                }

                modListViewController.DidSelectPlugin += HandleSelectPlugin;
                modControlsViewController.OnListNeedsRefresh += HandleListNeedsRefresh;
                modControlsViewController.OnChangeNeedsConfirmation += modalPopupViewController.QueueChange;
            }
            catch (Exception ex)
            {
                siraLog.Error(ex);
            }
            modControlsViewController.PresentFloatingScreen();
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            modControlsViewController.OnChangeNeedsConfirmation -= modalPopupViewController.QueueChange;
            modControlsViewController.OnListNeedsRefresh -= HandleListNeedsRefresh;
            modListViewController.DidSelectPlugin -= HandleSelectPlugin;

            modControlsViewController.HideFloatingScreen();

            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        internal void OnAnimationFinish()
        {
            modListViewController.OnAnimationFinish();
        }

        private void HandleSelectPlugin(PluginInformation? plugin)
        {
            if (plugin != null)
            {
                siraLog.Info($"Mod list selected plugin {plugin.Plugin} ({plugin.State})");
            }
            modInfoViewController.SetPlugin(plugin);
            modControlsViewController.SetPlugin(plugin);
        }

        private void HandleListNeedsRefresh()
        {
            modListViewController.ReloadViewList();
        }

        protected override void BackButtonWasPressed(ViewController viewController)
        {
            // If there is a change pending (and modal open) we have to deny it
            // If we don't, the change will remain pending when dismissing the view and coming back
            if (modalPopupViewController.CurrentChange != null)
            {
                modalPopupViewController.DenyChange();
            }

            // Check whether there's a transaction going on and commit :eyes:
            // TODO: Guess it's a good idea to add a confirmation here as well...
            // TODO: Should also show an additional warning when the user tries to disable ModList itself.
            if (modControlsViewController.CurrentTransaction != null && modControlsViewController.ChangedCount > 0)
            {
                _ = modControlsViewController
                    .CurrentTransaction
                    .Commit()
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            siraLog.Error(t.Exception);
                        }

                        modControlsViewController.CurrentTransaction = null;

                        // No need to dismiss ourselves as the scene will be restarted "automagically".
                        // That is if BS_Utils is installed and enabled... however... some people might wanna disable it for one reason or another.
                        // So in that case, we'll restart the scene ourselves... less "automagically" though...
                        if (PluginManager.EnabledPlugins.All(x => x.Id != "BS Utils"))
                        {
                            menuTransitionsHelper.RestartGame();
                        }
                    });
            }
            else
            {
                // Dismiss ourselves
                BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
            }
        }
    }
}