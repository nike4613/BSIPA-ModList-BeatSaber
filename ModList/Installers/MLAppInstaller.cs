using IPA.Logging;
using IPA.ModList.BeatSaber.Services;
using SiraUtil;
using Zenject;

namespace IPA.ModList.BeatSaber.Installers
{
    internal class MLAppInstaller : Installer<Logger, ModListConfig, string, MLAppInstaller>
    {
        private readonly Logger logger;
        private readonly ModListConfig config;
        private readonly string name;

        public MLAppInstaller(Logger logger, ModListConfig config, string name)
        {
            this.logger = logger;
            this.config = config;
            this.name = name;
        }

        public override void InstallBindings()
        {
            Container.BindInstance(name).WithId("modListName");

            Container.BindInstance(logger).AsSingle();

            Container.BindInstance(config).AsSingle();

            Container.BindInterfacesAndSelfTo<ModProviderService>().AsSingle().Lazy();
            Container.BindInterfacesTo<CustomTagControllerService>().AsSingle().Lazy();
        }
    }
}