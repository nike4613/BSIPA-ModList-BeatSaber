using System;
using System.Linq;
using BeatSaberMarkupLanguage;
using HMUI;
using IPA.Loader;
using IPA.ModList.BeatSaber.Models;
using IPA.ModList.BeatSaber.UI.ViewControllers;
using SiraUtil.Tools;
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
        internal void Construct(SiraLog siraLog, [Inject(Id = "modListName")] string modName, ModListNavigationController navigationController, ModListViewController modListViewController,
            ModInfoViewController modInfoViewController, ModControlsViewController modControlsViewController, ModalPopupViewController modalPopupViewController,
            MenuTransitionsHelper menuTransitionsHelper)
        {
            this.menuTransitionsHelper = menuTransitionsHelper;
            this.modControlsViewController = modControlsViewController;
            this.siraLog = siraLog;
            this.modName = modName;

            this.navigationController = navigationController;
            this.modListViewController = modListViewController;
            this.modInfoViewController = modInfoViewController;
            this.modalPopupViewController = modalPopupViewController;
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
                    ProvideInitialViewControllers(navigationController, bottomScreenViewController: modControlsViewController);

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
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            modControlsViewController.OnChangeNeedsConfirmation -= modalPopupViewController.QueueChange;
            modControlsViewController.OnListNeedsRefresh -= HandleListNeedsRefresh;
            modListViewController.DidSelectPlugin -= HandleSelectPlugin;

            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        private void HandleSelectPlugin(PluginInformation plugin)
        {
            siraLog.Info($"Mod list selected plugin {plugin.Plugin} ({plugin.State})");
            modInfoViewController.SetPlugin(plugin);
            modControlsViewController.SetPlugin(plugin);
        }

        private void HandleListNeedsRefresh()
        {
            modListViewController.ReloadViewList();
        }

        protected override void BackButtonWasPressed(ViewController _)
        {
            // Check whether there's a transaction going on and commit :eyes:
            // TODO: Guess it's a good idea to add a confirmation here as well...
            if (modControlsViewController.CurrentTransaction != null && modControlsViewController.ChangedCount > 0)
            {
                modControlsViewController.CurrentTransaction
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