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
using UnityEngine.EventSystems;

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
            var asset = Helpers.TMPFontFromUnityFont(font);

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
                        new GlyphMetrics(InlineCodePadding, 1f, 0, 0, InlineCodePadding),
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
                .UI.Font(Helpers.TekoMediumArialFallback)
                .Link.UseColor(Color.cyan)
                .Link.UseAutoColor(Color.red)
                .Quote.UseBackground(Helpers.SmallRoundedRectSprite, Image.Type.Sliced)
                .Quote.UseColor(new Color(30f / 255, 109f / 255, 178f / 255, .25f))
                .Code.UseBackground(Helpers.SmallRoundedRectSprite, Image.Type.Sliced)
                .Code.UseColor(new Color(135f / 255, 135f / 255, 135f / 255, .25f))
                .Code.UseFont(font)
                .Code.Inline.UseBackground(Helpers.TinyRoundedRectSprite, Image.Type.Sliced)
                .Code.Inline.UseColor(new Color(135f / 255, 135f / 255, 135f / 255, .1f))
                .Code.Inline.UsePadding(padding)
                .UseObjectRenderedCallback((obj, go) =>
                {
                    if (obj is HeadingBlock || obj is ThematicBreakBlock || obj is ListItemBlock)
                        go.AddComponent<ItemForFocussedScrolling>();

                    var tmp = go.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) // explicitly disable TMP raycasting on TMP objects
                        tmp.raycastTarget = false;
                })
                .UseLinkRenderedCallback(OnLinkRendered)
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

        #region Links
        public delegate void LinkPressed(string url, string title);
        public event LinkPressed OnLinkPressed;

        private void OnLinkRendered(IEnumerable<GameObject> hoverableGOs, GameObject fullBgGO, string url, string title)
        {
            fullBgGO.AddComponent<LinkHoverHint>();
            var hoverManager = fullBgGO.AddComponent<LinkHoverManager>();
            hoverManager.HoverHint.Controller = Resources.FindObjectsOfTypeAll<HoverHintController>().First();
            hoverManager.MarkdownText = this;
            hoverManager.TitleText = title;
            hoverManager.Url = url;

            foreach (var go in hoverableGOs)
            {
                var hover = go.AddComponent<LinkPartHover>();
                hover.Manager = hoverManager;
                go.AddComponent<Interactable>(); // gives you a little buzz when hovering
            }
        }

        private void InvokeLinkPressed(string url, string title)
            => OnLinkPressed?.Invoke(url, title);
        
        [RequireComponent(typeof(LinkHoverHint))]
        private class LinkHoverManager : MonoBehaviour
        {
            public LinkHoverHint HoverHint => GetComponent<LinkHoverHint>();

            public HoverHintController HintController => HoverHint.Controller;

            public string TitleText { get; set; }
            public string Url { get; set; }
            public MarkdownText MarkdownText { get; set; }

            public void BeginHover()
            {
                if (!string.IsNullOrEmpty(TitleText))
                {
                    var hint = HoverHint;
                    hint.text = TitleText;
                    hint.Controller.ShowHint(hint);
                }
            }

            public void EndHover()
            {
                HintController.HideHint();
            }

            public void Click()
            {
                MarkdownText.InvokeLinkPressed(Url, TitleText);
            }
        }

        private class LinkPartHover : Graphic, 
            IPointerEnterHandler,
            IPointerExitHandler,
            IPointerClickHandler,
            IEventSystemHandler
        {
            public LinkHoverManager Manager { get; set; }

            protected override void Awake()
                => raycastTarget = true;

            public void OnPointerEnter(PointerEventData eventData)
                => Manager.BeginHover();

            public void OnPointerExit(PointerEventData eventData)
                => Manager.EndHover();
            public void OnPointerClick(PointerEventData eventData)
                => Manager.Click();

            protected override void UpdateMaterial() { }
            protected override void UpdateGeometry() { }

        }

        private class LinkHoverHint : HoverHint
        {
            public HoverHintController Controller
            {
                get => _hoverHintController;
                set => _hoverHintController = value;
            }

            internal void Start()
                => enabled = false; // always force

            internal void Update()
                => enabled = false;
        }
        #endregion
    }
}
