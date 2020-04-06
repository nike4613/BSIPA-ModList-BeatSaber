using IPA.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IPALogger = IPA.Logging.Logger;

namespace IPA.ModList.BeatSaber
{
    [Plugin(RuntimeOptions.DynamicInit)]
    internal class ModListPlugin
    {
        [Init]
        public ModListPlugin(IPALogger logger)
        {
            Logger.log = logger;
        }

        [OnEnable]
        public void OnEnable()
        {
            Logger.log.Debug("Enabled");
        }

        [OnDisable]
        public void OnDisable()
        {
            Logger.log.Debug("Disabled");
        }
    }

    internal static class Logger
    {
#pragma warning disable IDE1006 // Naming Styles
        public static IPALogger log { get; set; }

        private static IPALogger _md = null;
        public static IPALogger md => _md ??= log.GetChildLogger("Markdown");
#pragma warning restore IDE1006 // Naming Styles
    }
}
