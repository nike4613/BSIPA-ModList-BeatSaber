using System.Collections.Generic;
using System.Linq;
using IPA.Loader;
using IPA.ModList.BeatSaber.Models;

namespace IPA.ModList.BeatSaber.Extensions
{
    internal static class PluginInformationExtensions
    {
        public static PluginInformation AsInfo(this PluginMetadata meta, PluginState state) => new PluginInformation(meta, state);
        public static IEnumerable<PluginInformation> AsInfos(this IEnumerable<PluginMetadata> metas, PluginState state) => metas.Select(m => m.AsInfo(state));
    }
}