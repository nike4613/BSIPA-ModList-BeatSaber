using IPA.Config.Stores;
using IPA.Logging;
using IPA.ModList.BeatSaber.Services;
using SiraUtil;
using Zenject;

namespace IPA.ModList.BeatSaber.Installers
{
    public class AppInstaller : Installer<Logger, Config.Config, string, AppInstaller>
    {
        private readonly Logger logger;
        private readonly Config.Config config;
        private readonly string name;

        public AppInstaller(Logger logger, Config.Config config, string name)
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
            ModListConfig.Instance ??= config.Generated<ModListConfig>();
            Container.BindInstance(ModListConfig.Instance).AsSingle();

            Container.BindInterfacesAndSelfTo<ModProviderService>().AsSingle().Lazy();
            Container.BindInterfacesTo<CustomTagControllerService>().AsSingle().Lazy();
        }
    }
}