using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.ModList.BeatSaber.UI
{
    [HotReload(Aliases = new[] { "C:", CompileConstants.SolutionDirectory })]
    internal class TestViewController : BSMLAutomaticViewController
    {
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            Logger.log.Debug("Test VC activated");
        }
    }
}
