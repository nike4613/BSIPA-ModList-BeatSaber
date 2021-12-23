using System;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using IPA.Loader;
using SiraUtil.Zenject;
using Zenject;

namespace IPA.ModList.BeatSaber.UI
{
    internal class ModListButtonManager : IInitializable, IDisposable
    {
        private readonly ModListFlowCoordinator modListFlowCoordinator;
        private MenuButton? modListButton;

        [Inject]
        public ModListButtonManager(ModListFlowCoordinator flowCoordinator, UBinder<Plugin, PluginMetadata> pluginMetadata)
        {
            modListFlowCoordinator = flowCoordinator;
            modListButton = new MenuButton(pluginMetadata.Value.Name, "Show list of installed mods", OnClick);
        }

        public void Initialize()
        {
            MenuButtons.instance.RegisterButton(modListButton);
        }

        private void OnClick()
        {
            if (modListFlowCoordinator == null)
            {
                return;
            }

            BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(modListFlowCoordinator, modListFlowCoordinator.OnAnimationFinish);
        }

        public void Dispose()
        {
            if (modListButton == null)
            {
                return;
            }

            if (BSMLParser.IsSingletonAvailable && MenuButtons.IsSingletonAvailable)
            {
                MenuButtons.instance.UnregisterButton(modListButton);
            }

            modListButton = null!;
        }
    }
}