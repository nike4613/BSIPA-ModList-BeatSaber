using IPA.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BSMLUtils = BeatSaberMarkupLanguage.Utilities;

namespace IPA.ModList.BeatSaber
{
    internal class Helpers
    {
        private const string ResourcePrefix = "IPA.ModList.BeatSaber.Resources.";

        private static Texture2D defaultPluginIcon = null;
        public static Texture2D DefaultPluginIcon
            => defaultPluginIcon ??= ReadImageFromAssembly(typeof(Helpers).Assembly, ResourcePrefix + "mod_bsipa.png");

        private static Texture2D legacyPluginIcon = null;
        public static Texture2D LegacyPluginIcon
            => legacyPluginIcon ??= ReadImageFromAssembly(typeof(Helpers).Assembly, ResourcePrefix + "mod_ipa.png");

        public static Texture2D BareManifestIcon => DefaultPluginIcon;

        public static Texture2D ReadImageFromAssembly(Assembly assembly, string name)
        {
            if (assembly == null) return null;
            using var resourceStream = assembly.GetManifestResourceStream(name);
            if (resourceStream == null)
            {
                Logger.log.Warn($"Assembly {assembly.GetName().Name} does not have embedded resource {name}");
                return null;
            }
            var data = new byte[resourceStream.Length];
            int read = 0;
            while (read < data.Length)
                read += resourceStream.Read(data, read, data.Length - read);
            return BSMLUtils.LoadTextureRaw(data);
        }

        public static Texture2D ReadPluginIcon(PluginMetadata plugin)
        {
            if (plugin.IsBare) return BareManifestIcon;

            Texture2D icon = null;
            if (plugin.IconName != null)
                icon = ReadImageFromAssembly(plugin.Assembly, plugin.IconName);

            return icon ?? DefaultPluginIcon;
        }
    }
}
