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
    public class ModListFlowCoordinator : FlowCoordinator
    {
        private SiraLog _siraLog = null!;
        private string _name = string.Empty;

        private MenuTransitionsHelper _menuTransitionsHelper = null!;

        private ModListNavigationController _navigationController = null!;
        private ModListViewController _modListViewController = null!;
        private ModInfoViewController _modInfoViewController = null!;
        private ModControlsViewController _modControlsViewController = null!;
        private ModalPopupViewController _modalPopupViewController = null!;

        [Inject]
        internal void Construct(SiraLog siraLog, [Inject(Id = "name")] string modName, ModListNavigationController navigationController, ModListViewController modListViewController,
            ModInfoViewController modInfoViewController, ModControlsViewController modControlsViewController, ModalPopupViewController modalPopupViewController,
            MenuTransitionsHelper menuTransitionsHelper)
        {
            _menuTransitionsHelper = menuTransitionsHelper;
            _modControlsViewController = modControlsViewController;
            _siraLog = siraLog;
            _name = modName;

            _navigationController = navigationController;
            _modListViewController = modListViewController;
            _modInfoViewController = modInfoViewController;
            _modalPopupViewController = modalPopupViewController;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (firstActivation)
                {
                    SetTitle(_name);
                    showBackButton = true;
                    SetViewControllersToNavigationController(_navigationController, _modListViewController, _modInfoViewController);
                    ProvideInitialViewControllers(_navigationController, bottomScreenViewController: _modControlsViewController);

                    _modalPopupViewController.SetData(_navigationController.gameObject);
                }

                _modListViewController.DidSelectPlugin += HandleSelectPlugin;
                _modControlsViewController.OnListNeedsRefresh += HandleListNeedsRefresh;
                _modControlsViewController.OnChangeNeedsConfirmation += _modalPopupViewController.QueueChange;
            }
            catch (Exception ex)
            {
                _siraLog.Error(ex);
            }
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _modControlsViewController.OnChangeNeedsConfirmation -= _modalPopupViewController.QueueChange;
            _modControlsViewController.OnListNeedsRefresh -= HandleListNeedsRefresh;
            _modListViewController.DidSelectPlugin -= HandleSelectPlugin;

            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        private void HandleSelectPlugin(PluginInformation plugin)
        {
            _siraLog.Info($"Mod list selected plugin {plugin.Plugin} ({plugin.State})");
            _modInfoViewController.SetPlugin(plugin);
            _modControlsViewController.SetPlugin(plugin);
        }

        private void HandleListNeedsRefresh()
        {
            _modListViewController.ReloadViewList();
        }

        protected override void BackButtonWasPressed(ViewController _)
        {
            // Check whether there's a transaction going on and commit :eyes:
            // TODO: Guess it's a good idea to add a confirmation here as well...
            if (_modControlsViewController.CurrentTransaction != null && _modControlsViewController.ChangedCount > 0)
            {
                _modControlsViewController.CurrentTransaction
                    .Commit()
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            _siraLog.Error(t.Exception);
                        }

                        _modControlsViewController.CurrentTransaction = null;

                        // No need to dismiss ourselves as the scene will be restarted "automagically".
                        // That is if BS_Utils is installed and enabled... however... some people might wanna disable it for one reason or another.
                        // So in that case, we'll restart the scene ourselves... less "automagically" though...
                        if (PluginManager.EnabledPlugins.All(x => x.Id != "BS Utils"))
                        {
                            _menuTransitionsHelper.RestartGame();
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