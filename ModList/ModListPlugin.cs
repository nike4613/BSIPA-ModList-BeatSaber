using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using BS_Utils.Utilities;
using HMUI;
using IPA.Loader;
using IPA.Logging;
using IPA.ModList.BeatSaber.UI;
using IPA.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;

namespace IPA.ModList.BeatSaber
{
    [Plugin(RuntimeOptions.DynamicInit)]
    internal class ModListPlugin
    {
        private readonly MenuButton menuBtn;

        [Init]
        public ModListPlugin(IPALogger logger)
        {
            Logger.log = logger;
            menuBtn = new MenuButton(CompileConstants.Manifest.Name, PresentModList);
        }

        [OnEnable]
        public void OnEnable()
        {
            Logger.log.Debug($"{CompileConstants.Manifest.Name} Enabled");
            MenuButtons.instance.RegisterButton(menuBtn);
        }

        [OnDisable]
        public void OnDisable()
        {
            MenuButtons.instance.UnregisterButton(menuBtn);
            Logger.log.Debug("Disabled");
        }

        private UI.ModListFlowCoordinator flowCoord;

        private void PresentModList()
        {
            if (flowCoord == null)
                flowCoord = BeatSaberUI.CreateFlowCoordinator<ModListFlowCoordinator>();
            flowCoord.ParentFlowCoordinator = BeatSaberUI.MainFlowCoordinator;
            BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(flowCoord);
        }
    }

    internal static class Logger
    {
#pragma warning disable IDE1006 // Naming Styles
        public static IPALogger log { get; set; }

        public static IPALogger md => _md ??= log.GetChildLogger("Markdown");
        private static IPALogger _md = null;
#pragma warning restore IDE1006 // Naming Styles
    }
}
