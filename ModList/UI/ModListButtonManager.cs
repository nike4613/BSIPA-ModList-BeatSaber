using System;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using Zenject;

namespace IPA.ModList.BeatSaber.UI
{
    internal class ModListButtonManager : IInitializable, IDisposable
    {
        private readonly ModListFlowCoordinator _modListFlowCoordinator;
        private MenuButton? _modListButton;

        [Inject]
        public ModListButtonManager(ModListFlowCoordinator modListFlowCoordinator, [Inject(Id = "name")] string name)
        {
            _modListFlowCoordinator = modListFlowCoordinator;
            _modListButton = new MenuButton(name, "Select the config you want.", OnClick);
        }

        public void Initialize()
        {
            MenuButtons.instance.RegisterButton(_modListButton);
        }

        private void OnClick()
        {
            if (_modListFlowCoordinator == null)
            {
                return;
            }

            BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(_modListFlowCoordinator);
        }

        public void Dispose()
        {
            if (_modListButton == null)
            {
                return;
            }

            if (MenuButtons.IsSingletonAvailable)
            {
                MenuButtons.instance.UnregisterButton(_modListButton);
            }

            _modListButton = null!;
        }
    }
}