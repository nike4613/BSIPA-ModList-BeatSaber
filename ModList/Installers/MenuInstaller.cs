using IPA.ModList.BeatSaber.UI;
using IPA.ModList.BeatSaber.UI.ViewControllers;
using SiraUtil;
using SiraUtil.Tools;
using Zenject;

namespace IPA.ModList.BeatSaber.Installers
{
    internal class MenuInstaller : Installer<MenuInstaller>
    {
        private readonly SiraLog siraLog;

        public MenuInstaller(SiraLog siraLog)
        {
            this.siraLog = siraLog;
        }

        public override void InstallBindings()
        {
            siraLog.Debug($"Running {nameof(InstallBindings)} of {nameof(MenuInstaller)}");

            Container.Bind<ModListNavigationController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<ModListViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<ModInfoViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<ModControlsViewController>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<ModalPopupViewController>().AsSingle().Lazy();
            Container.BindFlowCoordinator<ModListFlowCoordinator>();

            Container.BindInterfacesAndSelfTo<ModListButtonManager>().AsSingle().Lazy();
        }
    }
}