using IPA.ModList.BeatSaber.Services;
using Zenject;

namespace IPA.ModList.BeatSaber.Installers
{
    internal class MLAppInstaller : Installer<ModListConfig, string, MLAppInstaller>
    {
        private readonly ModListConfig config;
        private readonly string name;

        public MLAppInstaller(ModListConfig config, string name)
        {
            this.config = config;
            this.name = name;
        }

        public override void InstallBindings()
        {
            Container.BindInstance(name).WithId("modListName");

            Container.BindInstance(config).AsSingle();

            Container.BindInterfacesAndSelfTo<ModProviderService>().AsSingle().Lazy();
            Container.BindInterfacesTo<CustomTagControllerService>().AsSingle().Lazy();
        }
    }
}