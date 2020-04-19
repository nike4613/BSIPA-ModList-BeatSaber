using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Tags;
using IPA.ModList.BeatSaber.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace IPA.ModList.BeatSaber.UI.BSML
{
    public class MarkdownTag : BSMLTag
    {
        internal static void Register()
        {
            try
            {
                BSMLParser.instance.RegisterTag(new MarkdownTag());
                BSMLParser.instance.RegisterTypeHandler(new MarkdownTextHandler());
            }
            catch (Exception e)
            {
                Logger.md.Warn("Error when registering markdown tag:");
                Logger.md.Warn(e);
            }
        }

        internal static void Unregister()
        {
            Logger.md.Warn("Cannot correctly unregister because BSML does not yet support it!");
        }

        public override string[] Aliases { get; } = new[] { "markdown", "md" };

        public override bool AddChildren => false;

        public override GameObject CreateObject(Transform parent)
        {
            var go = new GameObject("BSMLMarkdown");
            go.SetActive(false);

            var md = go.AddComponent<MarkdownText>();
            md.RectTransform.SetParent(parent, false);

            md.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            md.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            /*var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;*/

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            go.SetActive(true);
            return go;
        }
    }
}
