using System;
using System.Collections.Generic;
using System.Linq;
using IPA.Loader;
using IPA.ModList.BeatSaber.Extensions;
using IPA.ModList.BeatSaber.Models;
using Zenject;

namespace IPA.ModList.BeatSaber.Services
{
    public class ModProviderService : IInitializable
    {
        private List<PluginInformation>? pluginList;

        internal List<PluginInformation> PluginList => pluginList ??= new List<PluginInformation>();

        public void Initialize()
        {
            // Calling the PluginList property lazily initializes the private static field
            if (PluginList.Any())
            {
                return;
            }

            // It's now safe to call the private pluginList field
            var ignoredPlugins = PluginManager.IgnoredPlugins.Keys.ToList();
            var disabledPlugins = PluginManager.DisabledPlugins.ToList();
            pluginList!.AddRange(
                PluginManager.EnabledPlugins.Except(disabledPlugins.Concat(ignoredPlugins)).AsInfos(PluginState.Enabled)
                             .Concat(disabledPlugins.AsInfos(PluginState.Disabled))
                             .Concat(ignoredPlugins.AsInfos(PluginState.Ignored)));
            pluginList.Sort((a, b) => string.Compare(a.Plugin.Name, b.Plugin.Name, StringComparison.OrdinalIgnoreCase)); // nah, we're sorting them by name so they can more easily be found
        }
    }
}