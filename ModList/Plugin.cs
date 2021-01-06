using System.Reflection;
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

            zenject.OnApp<AppInstaller>().WithParameters(log, ModListConfig.Instance, pluginMetadata.Name ?? Assembly.GetExecutingAssembly().GetName().Name);
            zenject.OnMenu<Installers.MenuInstaller>();
        }

        [OnEnable, OnDisable]
        public void OnStateChanged()
        {
            // Zenject is poggers
        }
    }
}