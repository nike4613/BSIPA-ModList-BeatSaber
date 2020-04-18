using BeatSaberMarkupLanguage.TypeHandlers;
using IPA.ModList.BeatSaber.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.ModList.BeatSaber.UI.BSML
{
    [ComponentHandler(typeof(MarkdownText))]
    public class MarkdownTextHandler : TypeHandler<MarkdownText>
    {
        public override Dictionary<string, string[]> Props { get; } = new Dictionary<string, string[]>
        {
            { "text", new[] { "text", "value" } },
            { "childText", new[] { "_children" } }
        };

        public override Dictionary<string, Action<MarkdownText, string>> Setters { get; } 
            = new Dictionary<string, Action<MarkdownText, string>>
            {
                { "text", (md, text) => md.Text = text },
                { "childText", (md, text) => md.Text = text }
            };
    }
}
