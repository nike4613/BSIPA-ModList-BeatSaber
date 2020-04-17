using IPA.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace IPA.ModList.BeatSaber
{
    internal enum PluginState
    {
        Enabled,
        Disabled,
        Ignored
    }
    internal struct PluginInformation
    {
        public PluginMetadata Plugin { get; }
        public PluginState State { get; }

        private Texture2D icon;
        public Texture2D Icon => icon ??= Helpers.ReadPluginIcon(Plugin);

        public PluginInformation(PluginMetadata meta, PluginState state)
        {
            Plugin = meta;
            State = state;
            icon = null;
        }
    }
    internal static class PluginInformationExtensions
    {
        public static PluginInformation AsInfo(this PluginMetadata meta, PluginState state)
            => new PluginInformation(meta, state);
        public static IEnumerable<PluginInformation> AsInfos(this IEnumerable<PluginMetadata> metas, PluginState state)
            => metas.Select(m => m.AsInfo(state));
    }
}
