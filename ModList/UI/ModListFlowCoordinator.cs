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
        private ModInfoViewController infoController;
        private ModControlsViewController controlsController;

        public void Awake()
        {
            if (naviController == null)
            {
                naviController = BeatSaberUI.CreateViewController<NavigationController>();
                listController = BeatSaberUI.CreateViewController<ModListViewController>();
                infoController = BeatSaberUI.CreateViewController<ModInfoViewController>();
                controlsController = BeatSaberUI.CreateViewController<ModControlsViewController>();
            }
        }

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            try
            {
                if (firstActivation)
                {
                    showBackButton = true;
                    title = CompileConstants.Manifest.Name;

                    SetViewControllersToNavigationController(naviController, listController);
                    ProvideInitialViewControllers(mainViewController: naviController, bottomScreenViewController: controlsController);
                }

                listController.DidSelectPlugin += HandleSelectPlugin;
            }
            catch (Exception e)
            {
                Logger.log.Error("Error activating TestFlowCoordinator");
                Logger.log.Error(e);
            }
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            listController.DidSelectPlugin -= HandleSelectPlugin;
            base.DidDeactivate(deactivationType);
        }

        private void HandleSelectPlugin(PluginInformation plugin)
        {
            Logger.log.Info($"Mod list selected plugin {plugin.Plugin} ({plugin.State})");

            infoController.SetPlugin(plugin);
            if (!infoController.Activated)
                PushViewControllerToNavigationController(naviController, infoController);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            base.BackButtonWasPressed(topViewController);
            ParentFlowCoordinator.DismissFlowCoordinator(this);
        }
    }
}
