using IPA.Config.Stores;
using IPA.Logging;
using IPA.ModList.BeatSaber.Installers;
using SiraUtil.Zenject;

namespace IPA.ModList.BeatSaber
{
    [Plugin(RuntimeOptions.DynamicInit), NoEnableDisable]
    public class Plugin
    {
        internal static Logger? Logger { get; private set; }

        [Init]
        public void Init(Logger log, Config.Config config, Zenjector zenject)
        {
            Logger = log;
            ModListConfig.Instance ??= config.Generated<ModListConfig>();

            zenject.UseLogger(log);
            zenject.UseMetadataBinder<Plugin>();

            zenject.Install<MLAppInstaller>(Location.App, ModListConfig.Instance);
            zenject.Install<MLMenuInstaller>(Location.Menu);
        }
    }
}