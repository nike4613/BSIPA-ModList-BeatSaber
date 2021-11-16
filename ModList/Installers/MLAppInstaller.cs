using IPA.ModList.BeatSaber.Services;
using Zenject;

namespace IPA.ModList.BeatSaber.Installers
{
    internal class MLAppInstaller : Installer<ModListConfig, MLAppInstaller>
    {
        private readonly ModListConfig config;

        public MLAppInstaller(ModListConfig config)
        {
            this.config = config;
        }

        public override void InstallBindings()
        {
            Container.BindInstance(config).AsSingle();

            Container.BindInterfacesAndSelfTo<ModProviderService>().AsSingle().Lazy();
            Container.BindInterfacesTo<CustomTagControllerService>().AsSingle().Lazy();
        }
    }
}