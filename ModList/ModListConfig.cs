using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace IPA.ModList.BeatSaber
{
    internal class ModListConfig
    {
        public static ModListConfig? Instance { get; set; }

        public virtual bool Reset { get; set; } = false;

        [NonNullable]
        public virtual string MonospaceFontName { get; set; } = "Consolas";

        public virtual string MonospaceFontPath { get; set; } = null!;

        [NonNullable]
        public virtual bool DummyProtection { get; set; } = true;

        public virtual void CopyFrom(ModListConfig config)
        {
        }

        public virtual void OnReload()
        {
            if (Reset)
            {
                CopyFrom(new ModListConfig());
            }
        }
    }
}