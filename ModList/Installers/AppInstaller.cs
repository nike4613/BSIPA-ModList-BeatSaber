using IPA.Config.Stores;
using IPA.Logging;
using IPA.ModList.BeatSaber.Services;
using SemVer;
using SiraUtil;
using Zenject;

namespace IPA.ModList.BeatSaber.Installers
{
    public class AppInstaller : Installer<Logger, Config.Config, string, Version, AppInstaller>
    {
        private readonly Logger _logger;
        private readonly Config.Config _config;
        private readonly string _name;
        private readonly Version _version;

        public AppInstaller(Logger logger, Config.Config config, string name, Version version)
        {
            _logger = logger;
            _config = config;
            _name = name;
            _version = version;
        }

        public override void InstallBindings()
        {
            Container.BindInstance(_name).WithId("name").AsSingle();
            Container.BindInstance(_version).WithId("version").AsSingle();

            _logger.Debug("Binding logger");
            Container.BindLoggerAsSiraLogger(_logger);

            _logger.Debug($"Binding {nameof(ModListConfig)}");
            ModListConfig.Instance ??= _config.Generated<ModListConfig>();
            Container.BindInstance(ModListConfig.Instance).AsSingle();

            Container.BindInterfacesAndSelfTo<ModProviderService>().AsSingle().Lazy();
            Container.BindInterfacesTo<CustomTagControllerService>().AsSingle().Lazy();
        }
    }
}