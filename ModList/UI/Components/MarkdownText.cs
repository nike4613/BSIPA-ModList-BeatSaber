using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using IPA.ModList.BeatSaber.UI.Markdig;
using Markdig.Extensions.EmphasisExtras;
using BSMLUtils = BeatSaberMarkupLanguage.Utilities;
using HMUI;
using TMPro;

namespace IPA.ModList.BeatSaber.UI.Components
{
    [RequireComponent(typeof(RectTransform))]
    public class MarkdownText : MonoBehaviour
    {
        public RectTransform RectTransform => gameObject.GetComponent<RectTransform>();

        public bool IsDirty { get; private set; } = false;

        private string text = null;
        public string Text 
        { 
            get => text;
            set
            {
                text = value;
                IsDirty = true;
            } 
        }

        internal void Update() 
        {
            if (IsDirty && text != null)
            {
                Clear();
                Render();
                IsDirty = false;
            }
        }

        internal void OnDestroy()
            => Clear();

        private static MarkdownPipeline pipeline = null;
        public static MarkdownPipeline Pipeline 
            => pipeline ??= new MarkdownPipelineBuilder()
                    .UseAutoLinks().UseListExtras().UsePreciseSourceLocation()
                    // the renderer treats the Subscript `~` as underline
                    .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough | EmphasisExtraOptions.Subscript)
                    .WithLogger(Logger.md)
                    .Build();

        private static UnityRenderer renderer = null;
        public static UnityRenderer Renderer 
            => renderer ??= CreateRenderer();

        private static TMP_FontAsset LoadConfigFont(ModListConfig config)
        {
            Font GetUnityFont()
            {
                if (config.MonospaceFontPath != null)
                    return new Font(config.MonospaceFontPath);

                var fonts = Font.GetOSInstalledFontNames();
                Logger.log.Debug($"fonts: {string.Join(" ; ", fonts)}");
                Logger.log.Debug($"select {config.MonospaceFontName}");
                if (fonts.Contains(config.MonospaceFontName))
                    return new Font(config.MonospaceFontName);

                Logger.log.Warn($"Could not find font of name '{config.MonospaceFontName}'");
                return new Font("Consolas");
            }

            var asset = TMP_FontAsset.CreateFontAsset(GetUnityFont());
            return Helpers.CreateFixedUIFontClone(asset);
        }

        private static UnityRenderer CreateRenderer()
        {
            return new UnityRendererBuilder()
                .UI.Material(BSMLUtils.ImageResources.NoGlowMat)
                .Quote.UseBackground(Helpers.RoundedBackgroundSprite, UnityEngine.UI.Image.Type.Sliced)
                .Quote.UseColor(new Color(30f / 255, 109f / 255, 178f / 255, .25f))
                .Code.UseColor(new Color(135f / 255, 135f / 255, 135f / 255, .25f))
                .Code.UseFont(LoadConfigFont(ModListConfig.Instance))
                .UseObjectRendererCallback((obj, go) =>
                {
                    Logger.md.Debug($"Rendered markdown object of type {obj.GetType()}");
                    if (obj is HeadingBlock || obj is ThematicBreakBlock)
                        go.AddComponent<ItemForFocussedScrolling>();
                })
                .Build();
        }

        private void Render()
        {
            Logger.md.Debug($"Rendering markdown:\n{string.Join("\n", Text.Split('\n').Select(s => "| " + s))}");
            var root = Markdown.Convert(Text, Renderer, Pipeline) as RectTransform;
            root.SetParent(RectTransform, false);
            root.anchorMin = new Vector2(0, 1);
            root.anchorMax = Vector2.one;
            root.anchoredPosition = Vector2.zero;
        }

        private static void ClearObject(Transform target)
        {
            foreach (Transform child in target)
            {
                ClearObject(child);
                Logger.md.Debug($"Destroying {child.name}");
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }

        private void Clear()
        {
            gameObject.SetActive(false);
            ClearObject(transform);
            gameObject.SetActive(true);
        }
    }
}
