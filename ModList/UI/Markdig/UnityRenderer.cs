using IPA.ModList.BeatSaber.UI.Components;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IPA.ModList.BeatSaber.UI.Markdig
{
    public class UnityRenderer : IMarkdownRenderer
    {
        public Material UIMaterial { get; }
        public Color UIColor { get; set; } = Color.white;
        public Color QuoteColor { get; }
        public Sprite QuoteBackground { get; }
        public Image.Type QuoteBackgroundType { get; }
        public Color CodeBackgroundColor { get; }
        public Sprite CodeBackground { get; }
        public Image.Type CodeBackgroundType { get; }
        public TMP_FontAsset CodeFont { get; set; }

        public UnityRenderer(Material uiMat, Sprite quoteBg, Image.Type bgType, Color quoteColor,
                                             Sprite codeBg, Image.Type codeBgType, Color codeColor)
        {
            UIMaterial = uiMat;
            QuoteBackground = quoteBg;
            QuoteBackgroundType = bgType;
            QuoteColor = quoteColor;

            CodeBackground = codeBg;
            CodeBackgroundType = codeBgType;
            CodeBackgroundColor = codeColor;
        }

        ObjectRendererCollection IMarkdownRenderer.ObjectRenderers { get; } = new ObjectRendererCollection();

        event Action<IMarkdownRenderer, MarkdownObject> IMarkdownRenderer.ObjectWriteBefore { add { } remove { } }

        event Action<IMarkdownRenderer, MarkdownObject> IMarkdownRenderer.ObjectWriteAfter { add { } remove { } }

        public event Action<MarkdownObject, GameObject> AfterObjectRendered;

        object IMarkdownRenderer.Render(MarkdownObject obj)
            => obj switch
            {
                Block block => RenderBlock(block).First(),
                Inline inline => RenderInline(inline, ParagraphFontSize),
                _ => throw new NotImplementedException("Unknown markdown object type")
            };

        private const float ParagraphFontSize = 3.5f;
        private const float CodeFontSize = ParagraphFontSize - .5f;
        private const float H1FontSize = 5.5f;
        private const float HeaderLevelFontDecrease = 0.5f;
        private const float ThematicBreakHeight = .5f;
        private const int TextInset = 1;
        private const int BlockQuoteInset = TextInset * 2;
        private const int BlockCodeInset = BlockQuoteInset;
        private const int ListInset = TextInset;

        #region Blocks
        private IEnumerable<RectTransform> RenderBlock(Block obj)
            => obj switch
            {
                MarkdownDocument doc => RenderDocument(doc).SingleEnumerable(),
                ParagraphBlock para => RenderParagraph(para),
                HeadingBlock heading => RenderHeading(heading),
                ThematicBreakBlock block => RenderThematicBreak(block),
                QuoteBlock quote => RenderQuote(quote),
                HtmlBlock html => RenderHtmlBlock(html),
                // I don't render FencedCodeBlock and CodeBlock differently, so there is just the one case
                CodeBlock code => RenderCodeBlock(code),

                _ => throw new NotImplementedException($"Unknown markdown block type {obj.GetType()}")
            };


        private (RectTransform, HorizontalOrVerticalLayoutGroup) Block(string name, float spacing, bool vertical)
        {
            Logger.md.Debug($"Creating {(vertical ? "vertical" : "horizontal")} block node {name} with spacing {spacing}");

            var go = new GameObject(name);
            var transform = go.AddComponent<RectTransform>();
            Helpers.Zero(transform);

            var layout = vertical ? go.AddComponent<VerticalLayoutGroup>() as HorizontalOrVerticalLayoutGroup
                                  : go.AddComponent<HorizontalLayoutGroup>();

            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = layout.childForceExpandWidth = false;
            layout.spacing = spacing;

            return (transform, layout);
        }

        private RectTransform Spacer(float size = 1.5f)
        {
            var go = new GameObject("Spacer");
            var transform = go.AddComponent<RectTransform>();
            transform.anchorMin = new Vector2(.5f, .5f);
            transform.anchorMax = new Vector2(.5f, .5f);
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;

            var l = go.AddComponent<LayoutElement>();
            l.minHeight = l.preferredHeight = size;

            return transform;
        }

        private RectTransform RenderDocument(MarkdownDocument doc)
        {
            var (transform, layout) = Block("Document", .5f, true);
            layout.childForceExpandWidth = true;

            foreach (var block in doc)
            {
                var children = RenderBlock(block);
                foreach (var child in children)
                    child.SetParent(transform, false);
            }

            AfterObjectRendered?.Invoke(doc, transform.gameObject);

            return transform;
        }

        private IEnumerable<RectTransform> RenderParagraph(ParagraphBlock para)
        {
            var (transform, layout) = Block("Paragraph", .1f, false);
            layout.padding = new RectOffset(TextInset, TextInset, 0, 0);

            if (para.Inline != null)
            {
                var inline = RenderInline(para.Inline, ParagraphFontSize);
                foreach (var child in inline)
                    child.SetParent(transform, false);
            }

            AfterObjectRendered?.Invoke(para, transform.gameObject);

            return Helpers.SingleEnumerable(transform).Append(Spacer(1.5f));
        }

        private IEnumerable<RectTransform> RenderHeading(HeadingBlock heading)
        {
            var (transform, layout) = Block("Heading", .1f, false);
            layout.padding = new RectOffset(TextInset, TextInset, 0, 0);
            if (heading.Level < 2)
                layout.childAlignment = TextAnchor.UpperCenter;

            if (heading.Inline != null)
            {
                var inline = RenderInline(heading.Inline, 
                    fontSize: H1FontSize - (HeaderLevelFontDecrease * (heading.Level - 1)),
                    center: heading.Level < 2);
                foreach (var child in inline)
                    child.SetParent(transform, false);
            }

            AfterObjectRendered?.Invoke(heading, transform.gameObject);

            var result = Helpers.SingleEnumerable(transform);
            if (heading.Level <= 2)
                result = result.Concat(RenderThematicBreak(null, spacing: 2f));
            return result;
        }

        private IEnumerable<RectTransform> RenderThematicBreak(ThematicBreakBlock block, float spacing = 1.5f)
        {
            var go = new GameObject("ThematicBreak");
            var transform = go.AddComponent<RectTransform>();
            Helpers.Zero(transform);

            var img = go.AddComponent<Image>();
            img.color = UIColor;
            // TODO: figure out a good way of making this not rely on a *new* sprite
            img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(Vector2.zero, Vector2.one), Vector2.zero);
            if (UIMaterial != null) img.material = UIMaterial;

            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = ThematicBreakHeight;

            if (block != null)
                AfterObjectRendered?.Invoke(block, transform.gameObject);

            return Helpers.SingleEnumerable(transform).Append(Spacer(spacing));
        }

        private IEnumerable<RectTransform> RenderQuote(QuoteBlock quote)
        {
            var (transform, layout) = Block("Quote", .1f, true);
            transform.anchorMin = new Vector2(0, 1);
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(BlockQuoteInset, BlockQuoteInset, BlockQuoteInset, 0);

            var go = transform.gameObject;

            var img = go.AddComponent<Image>();
            img.color = QuoteColor;
            img.sprite = QuoteBackground;
            img.type = QuoteBackgroundType;
            img.material = UIMaterial;

            foreach (var block in quote)
            {
                var children = RenderBlock(block);
                foreach (var child in children)
                    child.SetParent(transform, false);
            }

            AfterObjectRendered?.Invoke(quote, go);

            return Helpers.SingleEnumerable(transform).Append(Spacer(1.5f));
        }

        private IEnumerable<RectTransform> RenderHtmlBlock(HtmlBlock html)
        {
            Logger.md.Warn("Found HtmlBlock when rendering Markdown document, cannot process");

            Logger.md.Debug($"Block type: {html.Type}");
            Logger.md.Debug($"Content: {(html.Inline == null ? "null" : RenderInlineToText(html.Inline, new StringBuilder()).ToString())}");

            AfterObjectRendered?.Invoke(html, null);

            return Enumerable.Empty<RectTransform>();
        }

        private IEnumerable<RectTransform> RenderCodeBlock(CodeBlock code)
        {
            var (transform, layout) = Block("Code", .1f, true);
            transform.anchorMin = new Vector2(0, 1);
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(BlockCodeInset, BlockCodeInset, BlockCodeInset, BlockCodeInset);

            var go = transform.gameObject;

            var img = go.AddComponent<Image>();
            img.color = CodeBackgroundColor;
            img.sprite = CodeBackground;
            img.type = CodeBackgroundType;
            img.material = UIMaterial;

            var tmp = CreateText($"<noparse>{code.Lines}</noparse>", CodeFontSize, center: false);
            if (CodeFont != null) tmp.font = CodeFont;
            tmp.transform.SetParent(transform, false);

            AfterObjectRendered?.Invoke(code, go);

            return Helpers.SingleEnumerable(transform).Append(Spacer(1.5f));
        }
        #endregion

        #region Inlines
        private IEnumerable<RectTransform> RenderInline(Inline inline, float fontSize, bool center = false)
        {
            Logger.md.Debug("Rendering inline from block");
            codeRegionLinkPostfix = 0;
            var text = RenderInlineToText(inline, new StringBuilder(inline.Span.Length)).ToString();
            Logger.md.Debug($"Inline rendered to '{text}'");
            var tmp = CreateText(text, fontSize, center);

            if (CodeFont != null && !MaterialReferenceManager.instance.Contains(CodeFont))
                MaterialReferenceManager.AddFontAsset(CodeFont); // I wish there was a better way to do this

            var highlights = new GameObject("CodeBackgrounds");
            var highlightTransform = highlights.AddComponent<RectTransform>();
            Helpers.Zero(highlightTransform);
            var highlightLayout = highlights.AddComponent<LayoutElement>();
            highlightLayout.ignoreLayout = true;

            var highlight = AddHighlighter(tmp.gameObject, link => link.GetLinkID().StartsWith(CodeRegionLinkIdStart));
            highlight.BackgroundParent = highlightTransform;

            AfterObjectRendered?.Invoke(inline, tmp.gameObject);

            return Helpers.SingleEnumerable(highlightTransform).Append(tmp.rectTransform);
        }

        private StringBuilder RenderInlineToText(Inline inline, StringBuilder builder)
            => inline switch
            {
                LiteralInline lit => RenderLiteralToText(lit, builder),
                EmphasisInline em => RenderEmphasisToText(em, builder),
                LineBreakInline lb => RenderLineBreakInlineToText(lb, builder),
                CodeInline code => RenderCodeInlineToText(code, builder),
                AutolinkInline link => throw new NotImplementedException(), // TODO: implement this
                LinkInline link => throw new NotImplementedException(), // TODO: implement this
                HtmlInline tag => RenderHtmlInlineToText(tag, builder),
                HtmlEntityInline entity => RenderHtmlEntityToText(entity, builder),

                ContainerInline container => RenderContainerInlineToText(container, builder),
                _ => throw new NotImplementedException($"Unknown inline type {inline.GetType()}")
            };

        private StringBuilder RenderContainerInlineToText(ContainerInline container, StringBuilder builder)
        {
            Logger.md.Debug("Rendering ContainerInline");
            foreach (var inline in container)
                builder = RenderInlineToText(inline, builder);
            Logger.md.Debug("Rendered ContainerInline");
            return builder;
        }

        private StringBuilder RenderLiteralToText(LiteralInline lit, StringBuilder builder)
            => builder.Append("<noparse>").Append(lit.Content.ToString()).Append("</noparse>");

        private StringBuilder RenderLineBreakInlineToText(LineBreakInline lb, StringBuilder builder)
            => builder.Append(lb.IsHard ? "\n" : " ");

        private const string CodeRegionLinkIdStart = "__CodeInline__";
        private int codeRegionLinkPostfix = 0;
        private StringBuilder RenderCodeInlineToText(CodeInline code, StringBuilder builder)
            => builder.Append($"{(CodeFont == null ? "" : $"<font=\"{CodeFont?.name}\">")}<size=80%><link=\"{CodeRegionLinkIdStart}{codeRegionLinkPostfix++}\"><nobr> </nobr><noparse>")
                      .Append(code.Content)
                      .Append($"</noparse><nobr> </nobr></link></size>{(CodeFont == null ? "" : "</font>")}");

        private StringBuilder RenderHtmlInlineToText(HtmlInline tag, StringBuilder builder)
            => builder.Append(tag.Tag);

        private StringBuilder RenderHtmlEntityToText(HtmlEntityInline entity, StringBuilder builder)
            => builder.Append(entity.Transcoded.ToString());

        private StringBuilder RenderEmphasisToText(EmphasisInline em, StringBuilder builder)
        {
            Logger.md.Debug("Rendering inline emphasis");
            var flags = RenderHelpers.GetEmphasisFlags(em);
            builder.AppendEmOpenTags(flags);
            return RenderContainerInlineToText(em, builder)
                   .AppendEmCloseTags(flags);
        }
        #endregion

        private TextMeshProUGUI CreateText(string text, float fontSize, bool center)
        {
            var tmp = Helpers.CreateText(text, Vector2.zero, new Vector2(60f, 10f));
            tmp.enableWordWrapping = true;
            tmp.fontSize = fontSize;
            tmp.color = UIColor;
            if (center) tmp.alignment = TextAlignmentOptions.Center;

            return tmp;
        }

        private TextMeshProUGUILinkHighlighter AddHighlighter(GameObject obj, Func<TMP_LinkInfo, bool> linkSelector)
        {
            var highlighter = obj.AddComponent<TextMeshProUGUILinkHighlighter>();
            highlighter.BackgroundImageColor = CodeBackgroundColor;
            highlighter.BackgroundSprite = CodeBackground;
            highlighter.BackgroundImageType = CodeBackgroundType;
            highlighter.BackgroundMaterial = UIMaterial;
            highlighter.LinkSelector = linkSelector;
            return highlighter;
        }
    }
}
