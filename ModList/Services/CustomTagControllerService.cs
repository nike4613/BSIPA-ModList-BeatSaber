using System;
using IPA.ModList.BeatSaber.UI.BSML;
using Zenject;

namespace IPA.ModList.BeatSaber.Services
{
    internal class CustomTagControllerService : IInitializable, IDisposable
    {
        public void Initialize()
        {
            MarkdownTag.Register();
        }

        public void Dispose()
        {
            MarkdownTag.Unregister();
        }
    }
}