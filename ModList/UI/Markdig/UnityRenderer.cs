using BeatSaberMarkupLanguage;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace IPA.ModList.BeatSaber.UI.Markdig
{
    internal class UnityRenderer : IMarkdownRenderer
    {
        public ObjectRendererCollection ObjectRenderers { get; } = new ObjectRendererCollection();

        public event Action<IMarkdownRenderer, MarkdownObject> ObjectWriteBefore;
        public event Action<IMarkdownRenderer, MarkdownObject> ObjectWriteAfter;

        object IMarkdownRenderer.Render(MarkdownObject obj)
            => obj switch
            {
                Block block => RenderBlock(block).transform,
                Inline inline => RenderInline(inline),
                _ => throw new NotImplementedException("Unknown markdown object type")
            };

        private (RectTransform transform, float? space) RenderBlock(Block obj)
            => obj switch
            {
                MarkdownDocument doc => RenderDocument(doc),
                ParagraphBlock para => RenderParagraph(para),

                _ => throw new NotImplementedException($"Unknown markdown block type {obj.GetType()}")
            };

        private (RectTransform, HorizontalOrVerticalLayoutGroup) Block(string name, float spacing, bool vertical)
        {
            Logger.md.Debug($"Creating {(vertical ? "vertical" : "horizontal")} block node {name} with spacing {spacing}");

            var go = new GameObject(name);
            var transform = go.AddComponent<RectTransform>();
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
            transform.anchoredPosition = Vector2.zero;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            transform.sizeDelta = Vector2.zero;

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

        private (RectTransform, float?) RenderDocument(MarkdownDocument doc)
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
                var (child, space) = RenderBlock(block);
                child.SetParent(transform, false);
                if (space.HasValue)
                    Spacer(space.Value).SetParent(transform, false);
            }

            return (transform, null);
        }

        private const int TextInset = 1;

        private (RectTransform, float?) RenderParagraph(ParagraphBlock para)
        {
            Logger.md.Debug("Rendering paragraph");

            var (transform, layout) = Block("Paragraph", .1f, false);
            layout.padding = new RectOffset(TextInset, TextInset, 0, 0);

            if (para.Inline != null)
            {
                var inline = RenderInline(para.Inline);
                inline.SetParent(transform, false);
            }

            return (transform, 1.5f);
        }

        private RectTransform RenderInline(Inline inline)
        {
            Logger.md.Debug("Rendering inline from block");
            var builder = new StringBuilder(inline.Span.Length);
            RenderInlineToText(inline, builder);
            var text = builder.ToString();
            Logger.md.Debug($"Inline rendered to '{text}'");

            var tmp = Helpers.CreateText(text, Vector2.zero, new Vector2(60f, 10f));
            tmp.enableWordWrapping = true;
            return tmp.rectTransform;
        }

        private void RenderInlineToText(Inline inline, StringBuilder builder)
        {
            switch (inline)
            {
                case LiteralInline lit:
                    RenderLiteralToText(lit, builder);
                    return;
                case EmphasisInline em:
                    RenderEmphasisToText(em, builder);
                    return;
                case LineBreakInline lb:
                    RenderLineBreakInlineToText(lb, builder);
                    return;

                case ContainerInline container:
                    RenderContainerInlineToText(container, builder);
                    return;
                default:
                    throw new NotImplementedException($"Unknown inline type {inline.GetType()}");
            }
        }

        private void RenderContainerInlineToText(ContainerInline container, StringBuilder builder)
        {
            Logger.md.Debug("Rendering ContainerInline");
            foreach (var inline in container)
                RenderInlineToText(inline, builder);
        }

        private void RenderLiteralToText(LiteralInline lit, StringBuilder builder)
            => builder.Append("<noparse>").Append(lit.Content.ToString()).Append("</noparse>");

        private void RenderLineBreakInlineToText(LineBreakInline lb, StringBuilder builder)
            => builder.Append(lb.IsHard ? "\n" : " ");

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

        private void RenderEmphasisToText(EmphasisInline em, StringBuilder builder)
        {
            Logger.md.Debug("Rendering inline emphasis");
            var flags = GetEmphasisFlags(em);
            builder.AppendEmOpenTags(flags);
            RenderContainerInlineToText(em, builder);
            builder.AppendEmCloseTags(flags);
        }
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
