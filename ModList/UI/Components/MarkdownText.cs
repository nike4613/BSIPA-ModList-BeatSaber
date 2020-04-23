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
using IPA.Utilities.Async;
using System.Collections;
using UnityEngine.TextCore;
using IPA.Utilities;
using UnityEngine.UI;

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
                StartCoroutine(Render());
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

        private UnityRenderer renderer = null;
        public UnityRenderer Renderer 
            => renderer ??= CreateRenderer();

        private const uint UnicodePrivateUseStart = 0xE000;
        private const uint UnicodePrivateUseEnd = 0xF8FF;
        private const float InlineCodePadding = 20f;

        private delegate bool TryAddCharacterInternalDelegate(TMP_FontAsset font, uint codepoint, out TMP_Character character);

        private (TMP_FontAsset font, string padding) LoadConfigFont(ModListConfig config)
        {
            Font GetUnityFont()
            {
                if (config.MonospaceFontPath != null)
                    return new Font(config.MonospaceFontPath);

                if (FontManager.TryGetFont(config.MonospaceFontName, out var font))
                    return font;
                else if (FontManager.TryGetFont("Consolas", out font))
                    return font;
                else
                    return null;
            }

            var font = GetUnityFont();
            var asset = TMP_FontAsset.CreateFontAsset(font);
            asset.name = font.name;
            asset.hashCode = TMP_TextUtilities.GetSimpleHashCode(asset.name);

            var paddingChar = UnicodePrivateUseStart;
            var TryAddCharacterInternal = MethodAccessor<TMP_FontAsset, TryAddCharacterInternalDelegate>
                .GetDelegate("TryAddCharacterInternal");

            while (TryAddCharacterInternal(asset, paddingChar, out _) && paddingChar <= UnicodePrivateUseEnd)
                paddingChar++;

            if (paddingChar > UnicodePrivateUseEnd)
            {
                Logger.md.Error("Could not find open character in private use segment");
                paddingChar = ' '; // fall back to a space
            }
            else
            {
                Logger.md.Debug($"Using unicode codepoint {paddingChar:X}");
                var glyph = new Glyph(paddingChar,
                        new GlyphMetrics(InlineCodePadding, 10f, 0, 0, InlineCodePadding),
                        new GlyphRect(Rect.zero)
                    );
                var character = new TMP_Character(paddingChar, glyph);
                asset.glyphTable.Add(glyph);
                asset.characterTable.Add(character);
                asset.characterLookupTable.Add(paddingChar, character);
            }

            return (Helpers.CreateFixedUIFontClone(asset), char.ConvertFromUtf32((int)paddingChar));
        }

        private UnityRenderer CreateRenderer()
        {
            var (font, padding) = LoadConfigFont(ModListConfig.Instance);
            return new UnityRendererBuilder()
                .UI.Material(BSMLUtils.ImageResources.NoGlowMat)
                .Quote.UseBackground(Helpers.RoundedBackgroundSprite, Image.Type.Sliced)
                .Quote.UseColor(new Color(30f / 255, 109f / 255, 178f / 255, .25f))
                .Code.UseBackground(Helpers.SmallRoundedRectSprite, Image.Type.Sliced)
                .Code.UseColor(new Color(135f / 255, 135f / 255, 135f / 255, .25f))
                .Code.UseFont(font)
                .Code.Inline.UseBackground(Helpers.TinyRoundedRectSprite, Image.Type.Sliced)
                .Code.Inline.UsePadding(padding)
                .UseObjectRendererCallback((obj, go) =>
                {
                    Logger.md.Debug($"Rendered markdown object of type {obj.GetType()}");
                    if (obj is HeadingBlock || obj is ThematicBreakBlock)
                        go.AddComponent<ItemForFocussedScrolling>();
                })
                .Build();
        }

        private IEnumerator Render()
        {
            yield return Coroutines.WaitForTask(FontManager.AsyncLoadSystemFonts());

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
