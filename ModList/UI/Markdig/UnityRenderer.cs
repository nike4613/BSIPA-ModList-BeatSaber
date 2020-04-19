using BeatSaberMarkupLanguage;
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
    internal class UnityRenderer : IMarkdownRenderer
    {
        public Material UIMaterial { get; set; }

        public ObjectRendererCollection ObjectRenderers { get; } = new ObjectRendererCollection();

        public event Action<IMarkdownRenderer, MarkdownObject> ObjectWriteBefore;
        public event Action<IMarkdownRenderer, MarkdownObject> ObjectWriteAfter;

        object IMarkdownRenderer.Render(MarkdownObject obj)
            => obj switch
            {
                Block block => RenderBlock(block).First(),
                Inline inline => RenderInline(inline, ParagraphFontSize),
                _ => throw new NotImplementedException("Unknown markdown object type")
            };

        #region Blocks
        private IEnumerable<RectTransform> RenderBlock(Block obj)
            => obj switch
            {
                MarkdownDocument doc => RenderDocument(doc).SingleEnumerable(),
                ParagraphBlock para => RenderParagraph(para),
                HeadingBlock heading => RenderHeading(heading),
                ThematicBreakBlock _ => RenderThematicBreak(),

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
            Logger.md.Debug("Rendering document");
            var (transform, layout) = Block("Document", .5f, true);
            layout.childForceExpandWidth = true;

            /* this is from the original
            if (isDoc)
            {
                vlayout.sizeDelta = new Vector2(rectTransform.rect.width, 0f);
                vlayout.anchorMin = new Vector2(0f, 1f);
                vlayout.anchorMax = new Vector2(1f, 1f);
            }
            */

            foreach (var block in doc)
            {
                var children = RenderBlock(block);
                foreach (var child in children)
                    child.SetParent(transform, false);
            }

            return transform;
        }

        private const float ParagraphFontSize = 3.5f;
        private const float BlockCodeSize = ParagraphFontSize - .5f;
        private const float H1FontSize = 5.5f;
        private const float HeaderLevelFontDecrease = 0.5f;
        private const float ThematicBreakHeight = .5f;
        private const int TextInset = 1;
        private const int BlockQuoteInset = TextInset * 2;
        private const int BlockCodeInset = BlockQuoteInset;
        private const int ListInset = TextInset;

        private IEnumerable<RectTransform> RenderParagraph(ParagraphBlock para)
        {
            Logger.md.Debug("Rendering paragraph");

            var (transform, layout) = Block("Paragraph", .1f, false);
            layout.padding = new RectOffset(TextInset, TextInset, 0, 0);

            if (para.Inline != null)
            {
                var inline = RenderInline(para.Inline, ParagraphFontSize);
                inline.SetParent(transform, false);
            }

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
                inline.SetParent(transform, false);
            }

            var result = Helpers.SingleEnumerable(transform);
            if (heading.Level <= 2)
                result = result.Concat(RenderThematicBreak(spacing: 2f));
            return result;
        }

        private IEnumerable<RectTransform> RenderThematicBreak(float spacing = 1.5f) // I don't need to take the block, because it never looks any different
        {
            var go = new GameObject("ThematicBreak");
            var transform = go.AddComponent<RectTransform>();
            Helpers.Zero(transform);

            var img = go.AddComponent<Image>();
            img.color = Color.white;
            // TODO: figure out a good way of making this not rely on a *new* sprite
            img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(Vector2.zero, Vector2.one), Vector2.zero);
            if (UIMaterial != null) img.material = UIMaterial;

            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = ThematicBreakHeight;

            return Helpers.SingleEnumerable(transform).Append(Spacer(spacing));
        }

        #endregion

        #region Inlines
        private RectTransform RenderInline(Inline inline, float fontSize, bool center = false)
        {
            Logger.md.Debug("Rendering inline from block");
            var text = RenderInlineToText(inline, new StringBuilder(inline.Span.Length)).ToString();
            Logger.md.Debug($"Inline rendered to '{text}'");

            var tmp = Helpers.CreateText(text, Vector2.zero, new Vector2(60f, 10f));
            tmp.enableWordWrapping = true;
            tmp.fontSize = fontSize;
            if (center) tmp.alignment = TextAlignmentOptions.Center;
            return tmp.rectTransform;
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
            return builder;
        }

        private StringBuilder RenderLiteralToText(LiteralInline lit, StringBuilder builder)
            => builder.Append("<noparse>").Append(lit.Content.ToString()).Append("</noparse>");

        private StringBuilder RenderLineBreakInlineToText(LineBreakInline lb, StringBuilder builder)
            => builder.Append(lb.IsHard ? "\n" : " ");

        private StringBuilder RenderCodeInlineToText(CodeInline code, StringBuilder builder)
            => builder.Append("<font=\"CONSOLAS\"><size=80%><mark=#A0A0C080><noparse>")
                      .Append(code.Content)
                      .Append("</noparse></mark></size></font>");

        private StringBuilder RenderHtmlInlineToText(HtmlInline tag, StringBuilder builder)
            => builder.Append(tag.Tag);

        private StringBuilder RenderHtmlEntityToText(HtmlEntityInline entity, StringBuilder builder)
            => builder.Append(entity.Transcoded.ToString());

        private static EmphasisFlags GetEmphasisFlags(EmphasisInline em)
            => em.DelimiterChar switch
            {
                '~' => em.DelimiterCount switch
                {
                    1 => EmphasisFlags.Underline,
                    2 => EmphasisFlags.Strike,
                    var i when i > 2 => EmphasisFlags.Underline | EmphasisFlags.Strike,
                    _ => EmphasisFlags.None
                },
                var c when c == '*' || c == '_' => em.DelimiterCount switch
                {
                    1 => EmphasisFlags.Italic,
                    2 => EmphasisFlags.Bold,
                    var i when i > 2 => EmphasisFlags.Italic | EmphasisFlags.Bold,
                    _ => EmphasisFlags.None
                },
                _ => EmphasisFlags.None
            };

        private StringBuilder RenderEmphasisToText(EmphasisInline em, StringBuilder builder)
        {
            Logger.md.Debug("Rendering inline emphasis");
            var flags = GetEmphasisFlags(em);
            builder.AppendEmOpenTags(flags);
            return RenderContainerInlineToText(em, builder)
                   .AppendEmCloseTags(flags);
        }
        #endregion
    }

    [Flags]
    internal enum EmphasisFlags
    {
        None, Italic = 1, Bold = 2, Strike = 4, Underline = 8
    }

    internal static class StringBuilderExtensions
    {
        public static StringBuilder AppendEmOpenTags(this StringBuilder builder, EmphasisFlags tags)
        {
            if ((tags & EmphasisFlags.Italic) != EmphasisFlags.None)
                builder.Append("<i>");
            if ((tags & EmphasisFlags.Bold) != EmphasisFlags.None)
                builder.Append("<b>");
            if ((tags & EmphasisFlags.Strike) != EmphasisFlags.None)
                builder.Append("<s>");
            if ((tags & EmphasisFlags.Underline) != EmphasisFlags.None)
                builder.Append("<u>");
            return builder;
        }
        public static StringBuilder AppendEmCloseTags(this StringBuilder builder, EmphasisFlags tags)
        {
            if ((tags & EmphasisFlags.Underline) != EmphasisFlags.None)
                builder.Append("</u>");
            if ((tags & EmphasisFlags.Strike) != EmphasisFlags.None)
                builder.Append("</s>");
            if ((tags & EmphasisFlags.Bold) != EmphasisFlags.None)
                builder.Append("</b>");
            if ((tags & EmphasisFlags.Italic) != EmphasisFlags.None)
                builder.Append("</i>");
            return builder;
        }
    }
}
