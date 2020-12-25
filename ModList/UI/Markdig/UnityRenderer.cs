using IPA.ModList.BeatSaber.UI.Components;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HMUI;
using IPA.ModList.BeatSaber.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IPA.ModList.BeatSaber.UI.Markdig
{
    public class UnityRenderer : IMarkdownRenderer
    {
        public Material UIMaterial { get; }
        public Color UIColor { get; set; } = Color.white;
        public TMP_FontAsset UIFont { get; }
        public Color LinkColor { get; }
        public Color AutolinkColor { get; }
        public Color QuoteColor { get; }
        public Sprite QuoteBackground { get; }
        public Image.Type QuoteBackgroundType { get; }
        public Color CodeBackgroundColor { get; }
        public Sprite CodeBackground { get; }
        public Image.Type CodeBackgroundType { get; }
        public Color InlineCodeBackgroundColor { get; }
        public Sprite InlineCodeBackground { get; }
        public Image.Type InlineCodeBackgroundType { get; }
        public TMP_FontAsset CodeFont { get; set; }
        public string InlineCodePaddingText { get; set; } = "";

        public UnityRenderer(Material uiMat, TMP_FontAsset uiFont,
            Color linkColor, Color autolinkColor,
            Sprite quoteBg, Image.Type bgType, Color quoteColor,
            Sprite codeBg, Image.Type codeBgType, Color codeColor,
            Sprite inlineCodeBg, Image.Type inlineCodeBgType, Color inlineCodeColor)
        {
            UIMaterial = uiMat;
            UIFont = uiFont;

            LinkColor = linkColor;
            AutolinkColor = autolinkColor;

            QuoteBackground = quoteBg;
            QuoteBackgroundType = bgType;
            QuoteColor = quoteColor;

            CodeBackground = codeBg;
            CodeBackgroundType = codeBgType;
            CodeBackgroundColor = codeColor;
            InlineCodeBackground = inlineCodeBg;
            InlineCodeBackgroundType = inlineCodeBgType;
            InlineCodeBackgroundColor = inlineCodeColor;
        }

        ObjectRendererCollection IMarkdownRenderer.ObjectRenderers { get; } = new ObjectRendererCollection();

        event Action<IMarkdownRenderer, MarkdownObject> IMarkdownRenderer.ObjectWriteBefore
        {
            add { }
            remove { }
        }

        event Action<IMarkdownRenderer, MarkdownObject> IMarkdownRenderer.ObjectWriteAfter
        {
            add { }
            remove { }
        }

        public event Action<MarkdownObject, GameObject> AfterObjectRendered;

        object IMarkdownRenderer.Render(MarkdownObject obj) => obj switch
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
        private const int ParagraphInset = 1;
        private const int BlockQuoteInset = ParagraphInset * 2;
        private const int BlockCodeInset = BlockQuoteInset;
        private const int ListInset = ParagraphInset;
        private const float InlineCodePaddingSize = .4f;

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
                ListBlock list => RenderListBlock(list),
                LinkReferenceDefinition linkRef => RenderLinkReference(linkRef),
                LinkReferenceDefinitionGroup refs => RenderLinkReferenceGroup(refs),

                _ => throw new NotImplementedException($"Unknown markdown block type {obj.GetType()}")
            };

        private (RectTransform, HorizontalOrVerticalLayoutGroup) Block(string name, float spacing, bool vertical)
        {
            var go = new GameObject(name);
            var transform = go.AddComponent<RectTransform>();
            Helpers.Helpers.Zero(transform);

            var layout = vertical
                ? go.AddComponent<VerticalLayoutGroup>() as HorizontalOrVerticalLayoutGroup
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
                {
                    child.SetParent(transform, false);
                }
            }

            AfterObjectRendered?.Invoke(doc, transform.gameObject);

            return transform;
        }

        private IEnumerable<RectTransform> RenderParagraph(ParagraphBlock para)
        {
            var (transform, layout) = Block("Paragraph", .1f, false);
            layout.padding = new RectOffset(ParagraphInset, ParagraphInset, 0, 0);

            if (para.Inline != null)
            {
                var inline = RenderInline(para.Inline, ParagraphFontSize);
                foreach (var child in inline)
                {
                    child.SetParent(transform, false);
                }
            }

            AfterObjectRendered?.Invoke(para, transform.gameObject);

            return transform.SingleEnumerable().Append(Spacer(1.5f));
        }

        private IEnumerable<RectTransform> RenderHeading(HeadingBlock heading)
        {
            var (transform, layout) = Block("Heading", .1f, false);
            layout.padding = new RectOffset(ParagraphInset, ParagraphInset, 0, 0);
            if (heading.Level < 2)
            {
                layout.childAlignment = TextAnchor.UpperCenter;
            }

            if (heading.Inline != null)
            {
                var inline = RenderInline(heading.Inline,
                    fontSize: H1FontSize - (HeaderLevelFontDecrease * (heading.Level - 1)),
                    center: heading.Level < 2);
                foreach (var child in inline)
                {
                    child.SetParent(transform, false);
                }
            }

            AfterObjectRendered?.Invoke(heading, transform.gameObject);

            var result = transform.SingleEnumerable();
            if (heading.Level <= 2)
                result = result.Concat(RenderThematicBreak(null, spacing: 2f));
            return result;
        }

        private IEnumerable<RectTransform> RenderThematicBreak(ThematicBreakBlock? block, float spacing = 1.5f)
        {
            var go = new GameObject("ThematicBreak");
            var transform = go.AddComponent<RectTransform>();
            Helpers.Helpers.Zero(transform);

            var img = go.AddComponent<ImageView>();
            img.color = UIColor;
            // TODO: figure out a good way of making this not rely on a *new* sprite
            img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(Vector2.zero, Vector2.one), Vector2.zero);
            if (UIMaterial != null)
            {
                img.material = UIMaterial;
            }

            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = ThematicBreakHeight;

            if (block != null)
            {
                AfterObjectRendered?.Invoke(block, transform.gameObject);
            }

            return transform.SingleEnumerable().Append(Spacer(spacing));
        }

        private IEnumerable<RectTransform> RenderQuote(QuoteBlock quote)
        {
            var (transform, layout) = Block("Quote", .1f, true);
            transform.anchorMin = new Vector2(0, 1);
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(BlockQuoteInset, BlockQuoteInset, BlockQuoteInset, 0);

            var go = transform.gameObject;

            var img = go.AddComponent<ImageView>();
            img.color = QuoteColor;
            img.sprite = QuoteBackground;
            img.type = QuoteBackgroundType;
            img.material = UIMaterial;

            foreach (var child in quote.Select(block => RenderBlock(block)).SelectMany(children => children))
            {
                child.SetParent(transform, false);
            }

            AfterObjectRendered?.Invoke(quote, go);

            return transform.SingleEnumerable().Append(Spacer(1.5f));
        }

        private IEnumerable<RectTransform> RenderHtmlBlock(HtmlBlock html)
        {
            // TODO: Inject logger for this

            /*Logger.md.Warn("Found HtmlBlock when rendering Markdown document, cannot process");

            Logger.md.Debug($"Block type: {html.Type}");
            Logger.md.Debug($"Content: {(html.Inline == null ? "null" : RenderInlineToText(html.Inline, new StringBuilder()).ToString())}");*/

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

            var img = go.AddComponent<ImageView>();
            img.color = CodeBackgroundColor;
            img.sprite = CodeBackground;
            img.type = CodeBackgroundType;
            img.material = UIMaterial;

            var tmp = CreateText($"<noparse>{code.Lines}</noparse>", CodeFontSize, center: false);
            if (CodeFont != null)
            {
                tmp.font = CodeFont;
            }

            tmp.transform.SetParent(transform, false);

            AfterObjectRendered?.Invoke(code, go);

            return transform.SingleEnumerable().Append(Spacer(1.5f));
        }

        private const float ListBulletRegionSize = 5f;

        private IEnumerable<RectTransform> RenderListBlock(ListBlock list)
        {
            var (transform, layout) = Block("List", .1f, true);
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(ListInset, ListInset, 0, 0);

            foreach (var block in list)
            {
                if (!(block is ListItemBlock item))
                {
                    // TODO: Inject logger for this
                    // Logger.md.Warn("Block in list not a list item");
                }
                else
                {
                    var children = RenderListItem(item, list.IsLoose, list.IsOrdered, list.OrderedDelimiter, list.BulletType);
                    foreach (var child in children)
                        child.SetParent(transform, false);
                }
            }

            AfterObjectRendered?.Invoke(list, transform.gameObject);

            return transform.SingleEnumerable().Append(Spacer(1.5f));
        }

        private IEnumerable<RectTransform> RenderListItem(ListItemBlock item, bool isLoose, bool ordered, char orderedDelim, char bulletType)
        {
            var (transform, layout) = Block("ListItem", isLoose ? 0f : ParagraphInset, false);
            layout.childAlignment = TextAnchor.UpperLeft;

            var bulletTmp = CreateText(ordered ? $"{item.Order}{orderedDelim}" : "\u2022", ParagraphFontSize, false);
            bulletTmp.alignment = TextAlignmentOptions.Right;
            var bulletLayoutElement = bulletTmp.gameObject.AddComponent<LayoutElement>();
            bulletLayoutElement.minWidth = ListBulletRegionSize;
            bulletLayoutElement.preferredWidth = ListBulletRegionSize;
            bulletLayoutElement.layoutPriority = 100;
            bulletLayoutElement.flexibleWidth = 0;

            bulletTmp.transform.SetParent(transform, false);

            var (content, contentLayout) = Block("Content", .5f, true);
            contentLayout.childForceExpandWidth = isLoose;
            var contentLayoutElement = content.gameObject.AddComponent<LayoutElement>();
            contentLayoutElement.layoutPriority = 100;
            contentLayoutElement.flexibleWidth = 1;

            content.SetParent(transform, false);

            foreach (var block in item)
            {
                IEnumerable<RectTransform> children;

                if (isLoose)
                {
                    children = RenderBlock(block);
                }
                else
                {
                    children = block is ParagraphBlock {Inline: { }} para ? RenderInline(para.Inline, ParagraphFontSize) : RenderBlock(block);
                }

                foreach (var child in children)
                {
                    child.SetParent(content, false);
                }
            }

            AfterObjectRendered?.Invoke(item, transform.gameObject);

            return transform.SingleEnumerable();
        }

        private IEnumerable<RectTransform> RenderLinkReferenceGroup(LinkReferenceDefinitionGroup _)
        {
            // do nothing, because these are hidden
            return Enumerable.Empty<RectTransform>();
        }

        private IEnumerable<RectTransform> RenderLinkReference(LinkReferenceDefinition _)
        {
            // do nothing, because these are hidden
            return Enumerable.Empty<RectTransform>();
        }

        #endregion

        #region Inlines

        private IEnumerable<RectTransform> RenderInline(Inline inline, float fontSize, bool center = false)
        {
            _codeRegionLinkPostfix = 0;
            _linkDict = new Dictionary<string, LinkInfo>();

            var text = RenderInlineToText(inline, new StringBuilder(inline.Span.Length * 2)).ToString();
#if DEBUG
            // TODO: Inject logger for this
            // Logger.md.Debug($"Inline rendered to '{text}'");
#endif
            var tmp = CreateText(text, fontSize, center);

            if (CodeFont != null && !MaterialReferenceManager.instance.Contains(CodeFont))
                MaterialReferenceManager.AddFontAsset(CodeFont); // I wish there was a better way to do this

            var highlights = new GameObject("CodeBackgrounds");
            var highlightTransform = highlights.AddComponent<RectTransform>();
            Helpers.Helpers.Zero(highlightTransform);
            var highlightCopier = highlights.AddComponent<PositionSizeCopier>();
            highlightCopier.CopyFrom = tmp.rectTransform;
            var highlightLayout = highlights.AddComponent<LayoutElement>();
            highlightLayout.ignoreLayout = true;

            var highlight = CreateHighlighter(tmp.gameObject);
            highlight.BackgroundParent = highlightTransform;

            var codeLinkType = highlight.CreateLinkType(link => link.GetLinkID().StartsWith(CodeRegionLinkIDStart), null);
            SetCodeBackgroundLinkType(codeLinkType);
            highlight.AddLinkType(codeLinkType);

            var linkLinkType = highlight.CreateLinkType(link => link.GetLinkID().StartsWith(LinkIDStart), _linkDict);
            highlight.AddLinkType(linkLinkType);

            highlight.OnLinkBackgroundRendered += Highlight_OnLinkBackgroundRendered;
            highlight.OnLinkSingleObjectRendered += Highlight_OnLinkSingleObjectRendered;

            _linkDict = null;

            AfterObjectRendered?.Invoke(inline, tmp.gameObject);

            return highlightTransform.SingleEnumerable().Append(tmp.rectTransform);
        }

        private StringBuilder RenderInlineToText(Inline inline, StringBuilder builder) => inline switch
        {
            LiteralInline lit => RenderLiteralToText(lit, builder),
            EmphasisInline em => RenderEmphasisToText(em, builder),
            LineBreakInline lb => RenderLineBreakInlineToText(lb, builder),
            CodeInline code => RenderCodeInlineToText(code, builder),
            AutolinkInline link => RenderAutolinkInlineToText(link, builder),
            LinkInline link => RenderLinkInlineToText(link, builder),
            HtmlInline tag => RenderHtmlInlineToText(tag, builder),
            HtmlEntityInline entity => RenderHtmlEntityToText(entity, builder),

            ContainerInline container => RenderContainerInlineToText(container, builder),
            _ => throw new NotImplementedException($"Unknown inline type {inline.GetType()}")
        };

        private StringBuilder RenderContainerInlineToText(ContainerInline container, StringBuilder builder)
        {
            return container.Aggregate(builder, (current, inline) => RenderInlineToText(inline, current));
        }

        private StringBuilder RenderLiteralToText(LiteralInline lit, StringBuilder builder) => builder.Append("<noparse>").AppendSlice(lit.Content).Append("</noparse>");

        private StringBuilder RenderLineBreakInlineToText(LineBreakInline lb, StringBuilder builder)
            => builder.Append(lb.IsHard ? "\n" : " ");

        private const string CodeRegionLinkIDStart = "__CodeInline__";
        private int _codeRegionLinkPostfix = 0;

        private StringBuilder RenderCodeInlineToText(CodeInline code, StringBuilder builder)
            => builder.Append(CodeFont == null ? "" : $"<font=\"{CodeFont.name}\">")
                .Append("<size=80%>")
                .Append($"<link=\"{CodeRegionLinkIDStart}{_codeRegionLinkPostfix++}\">")
                .Append(InlineCodePaddingText)
                .Append("<noparse>")
                .Append(code.Content)
                .Append("</noparse>")
                .Append(InlineCodePaddingText)
                .Append("</link></size>")
                .Append(CodeFont == null ? "" : "</font>");

        private StringBuilder RenderHtmlInlineToText(HtmlInline tag, StringBuilder builder) => builder.Append(tag.Tag);

        private StringBuilder RenderHtmlEntityToText(HtmlEntityInline entity, StringBuilder builder) => builder.AppendSlice(entity.Transcoded);

        private StringBuilder RenderEmphasisToText(EmphasisInline em, StringBuilder builder)
        {
            var flags = RenderHelpers.GetEmphasisFlags(em);
            builder.AppendEmOpenTags(flags);
            return RenderContainerInlineToText(em, builder)
                .AppendEmCloseTags(flags);
        }

        #endregion

        #region Links

        private const string LinkIDStart = "__Link__";
        private Dictionary<string, LinkInfo> _linkDict;

        private StringBuilder RenderLinkInlineToText(LinkInline link, StringBuilder builder)
        {
            // link inlines can also be images
            if (link.IsImage)
            {
                // TODO: implement images
                builder.Append("[<noparse>")
                    .Append(link.Title)
                    .Append("</noparse>]");

                return builder;
            }

            var linkInfo = new LinkInfo(link.GetDynamicUrl?.Invoke() ?? link.Url, link.Title);
            var linkName = AddLinkToDict(linkInfo);

            builder.Append($"<link=\"{linkName}\">")
                .Append("<color=#").AppendColorHex(LinkColor).Append(">");
            return RenderContainerInlineToText(link, builder)
                .Append("</color>")
                .Append("</link>");
        }

        private StringBuilder RenderAutolinkInlineToText(AutolinkInline link, StringBuilder builder)
        {
            var linkInfo = new LinkInfo((link.IsEmail ? "mailto:" : "") + link.Url, null);
            var linkName = AddLinkToDict(linkInfo);

            return builder
                .Append($"<link=\"{linkName}\">")
                .Append("<color=#").AppendColorHex(AutolinkColor).Append(">")
                .Append(link.Url)
                .Append("</color>")
                .Append("</link>");
        }

        private string AddLinkToDict(LinkInfo linkInfo)
        {
            // TODO: Inject logger for this
            // Logger.md.Debug($"Rendering inline link to {linkInfo.Url} ({linkInfo.Title})");
            var linkName = LinkIDStart + _linkDict.Count;
            _linkDict.Add(linkName, linkInfo);
            return linkName;
        }

        private struct LinkInfo
        {
            public string URL;
            public string Title;
            public List<GameObject> SelectableObjects;

            public LinkInfo(string url, string title)
            {
                this.URL = url;
                this.Title = title;
                SelectableObjects = new List<GameObject>();
            }
        }

        private void Highlight_OnLinkBackgroundRendered(TMP_LinkInfo link, GameObject gameObject, object linkData)
        {
            if (!(linkData is Dictionary<string, LinkInfo> linkDict)) return;
            if (!linkDict.TryGetValue(link.GetLinkID(), out var linkInfo)) return;

            linkInfo.SelectableObjects.Add(gameObject);
        }

        public delegate void LinkRendered(IEnumerable<GameObject> textRegions, GameObject fullExtent, string url, string title);

        public event LinkRendered OnLinkRendered;

        private void Highlight_OnLinkSingleObjectRendered(TMP_LinkInfo link, GameObject gameObject, object linkData)
        {
            if (!(linkData is Dictionary<string, LinkInfo> linkDict)) return;
            if (!linkDict.TryGetValue(link.GetLinkID(), out var linkInfo)) return;

            OnLinkRendered?.Invoke(linkInfo.SelectableObjects, gameObject, linkInfo.URL, linkInfo.Title);
        }

        #endregion

        private TextMeshProUGUI CreateText(string text, float fontSize, bool center)
        {
            var tmp = Helpers.Helpers.CreateText(text, Vector2.zero, new Vector2(60f, 10f));
            tmp.enableWordWrapping = true;
            tmp.font = UIFont;
            tmp.fontSize = fontSize;
            tmp.color = UIColor;
            if (center)
            {
                tmp.alignment = TextAlignmentOptions.Center;
            }

            return tmp;
        }

        private TextMeshProUGUILinkBackgroundGenerator CreateHighlighter(GameObject obj)
        {
            var highlighter = obj.AddComponent<TextMeshProUGUILinkBackgroundGenerator>();
            return highlighter;
        }

        private void SetCodeBackgroundLinkType(TextMeshProUGUILinkBackgroundGenerator.LinkType type)
        {
            type.ShowBackground = true;
            type.BackgroundImageColor = InlineCodeBackgroundColor;
            type.BackgroundSprite = InlineCodeBackground;
            type.BackgroundImageType = InlineCodeBackgroundType;
            type.BackgroundMaterial = UIMaterial;
            type.Padding = new Vector4(0, 0, InlineCodePaddingSize, InlineCodePaddingSize);
            type.LineBreakPadding = InlineCodePaddingSize;
        }
    }
}