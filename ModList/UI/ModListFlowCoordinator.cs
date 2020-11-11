using BeatSaberMarkupLanguage;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.ModList.BeatSaber.UI
{
    internal class ModListFlowCoordinator : FlowCoordinator
    {
        public FlowCoordinator ParentFlowCoordinator { get; set; }
        private NavigationController naviController;
        private ModListViewController listController;
        private ListModalPopupViewController modalsController;
        private ModInfoViewController infoController;
        private ModControlsViewController controlsController;

        public void Awake()
        {
            if (naviController == null)
            {
                naviController = BeatSaberUI.CreateViewController<NavigationController>();
                listController = BeatSaberUI.CreateViewController<ModListViewController>();
                modalsController = BeatSaberUI.CreateViewController<ListModalPopupViewController>();
                infoController = BeatSaberUI.CreateViewController<ModInfoViewController>();
                controlsController = BeatSaberUI.CreateViewController<ModControlsViewController>();
            }
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (firstActivation)
                {
                    showBackButton = true;
                    SetTitle(CompileConstants.Manifest.Name);

                    SetViewControllersToNavigationController(naviController, listController);
                    ProvideInitialViewControllers(mainViewController: naviController, bottomScreenViewController: controlsController);
                }

                listController.DidSelectPlugin += HandleSelectPlugin;
                controlsController.OnListNeedsRefresh += HandleListNeedsRefresh;
                controlsController.OnChangeNeedsConfirmation += modalsController.QueueChange;

                PushViewControllerToNavigationController(naviController, modalsController, immediately: true);
            }
            catch (Exception e)
            {
                Logger.log.Error("Error activating TestFlowCoordinator");
                Logger.log.Error(e);
            }        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            controlsController.OnChangeNeedsConfirmation -= modalsController.QueueChange;
            controlsController.OnListNeedsRefresh -= HandleListNeedsRefresh;
            listController.DidSelectPlugin -= HandleSelectPlugin;

            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        private void HandleSelectPlugin(PluginInformation plugin)
        {
            Logger.log.Info($"Mod list selected plugin {plugin.Plugin} ({plugin.State})");

            infoController.SetPlugin(plugin);
            controlsController.SetPlugin(plugin);
            if (!infoController.Activated)
                PushViewControllerToNavigationController(naviController, infoController);
        }

        private void HandleListNeedsRefresh()
        {
            listController.ReloadViewList();
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            base.BackButtonWasPressed(topViewController);
            ParentFlowCoordinator.DismissFlowCoordinator(this);
        }
    }
}
