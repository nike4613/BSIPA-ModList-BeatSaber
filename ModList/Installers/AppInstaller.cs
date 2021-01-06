using IPA.Logging;
using IPA.ModList.BeatSaber.Services;
using SiraUtil;
using Zenject;

namespace IPA.ModList.BeatSaber.Installers
{
    internal class AppInstaller : Installer<Logger, ModListConfig, string, AppInstaller>
    {
        private readonly Logger logger;
        private readonly ModListConfig config;
        private readonly string name;

        public AppInstaller(Logger logger, ModListConfig config, string name)
        {
            this.logger = logger;
            this.config = config;
            this.name = name;
        }

        public override void InstallBindings()
        {
            Container.BindInstance(name).WithId("modListName");

            logger.Debug("Binding logger");
            Container.BindLoggerAsSiraLogger(logger);

            logger.Debug($"Binding {nameof(ModListConfig)}");
            Container.BindInstance(config).AsSingle();

            Container.BindInterfacesAndSelfTo<ModProviderService>().AsSingle().Lazy();
            Container.BindInterfacesTo<CustomTagControllerService>().AsSingle().Lazy();
        }
    }
}