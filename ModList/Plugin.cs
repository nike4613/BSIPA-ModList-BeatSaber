using IPA.Config.Stores;
using IPA.Loader;
using IPA.Logging;
using IPA.ModList.BeatSaber.Installers;
using SiraUtil.Zenject;

namespace IPA.ModList.BeatSaber
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Logger? Logger { get; private set; }

        [Init]
        public void Init(Logger log, Config.Config config, PluginMetadata pluginMetadata, Zenjector zenject)
        {
            Logger = log;
            ModListConfig.Instance ??= config.Generated<ModListConfig>();

            zenject.UseLogger(log);

            zenject.Install<MLAppInstaller>(Location.App, ModListConfig.Instance, pluginMetadata.Name);
            zenject.Install<MLMenuInstaller>(Location.Menu);
        }

        [OnEnable, OnDisable]
        public void OnStateChanged()
        {
            // Zenject is poggers
        }
    }
}