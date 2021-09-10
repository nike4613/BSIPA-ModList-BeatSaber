using IPA.ModList.BeatSaber.UI;
using IPA.ModList.BeatSaber.UI.ViewControllers;
using SiraUtil;
using Zenject;

namespace IPA.ModList.BeatSaber.Installers
{
    internal class MLMenuInstaller : Installer<MLMenuInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<ModListNavigationController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<ModListViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<ModInfoViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<ModControlsViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<ModalPopupViewController>().AsSingle().Lazy();
            Container.Bind<ModListFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();

            Container.BindInterfacesAndSelfTo<ModListButtonManager>().AsSingle().Lazy();
        }
    }
}