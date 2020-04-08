using BeatSaberMarkupLanguage;
using BS_Utils.Utilities;
using HMUI;
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
        [Init]
        public ModListPlugin(IPALogger logger)
        {
            Logger.log = logger;
        }

        [OnEnable]
        public void OnEnable()
        {
            Logger.log.Debug($"{CompileConstants.Manifest.Name} Enabled");
            HMMainThreadDispatcher.instance.StartCoroutine(PresentTest());
        }

        [OnDisable]
        public void OnDisable()
        {
            Logger.log.Debug("Disabled");
        }

        private IEnumerator PresentTest()
        {
            Logger.log.Debug("Waiting for main flow coordinator");
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().Any());
            Logger.log.Debug("Main flow coordinator found");
            yield return new WaitForSeconds(2);
            var testViewController = BeatSaberUI.CreateViewController<TestViewController>();
            Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First()
                .InvokeMethod<object, FlowCoordinator>("PresentViewController", new object[] { testViewController, null, false });
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
