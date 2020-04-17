using Markdig.Renderers;
using Markdig.Syntax;
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

        public object Render(MarkdownObject markdownObject) => RenderObject(markdownObject);

        private RectTransform RenderObject(MarkdownObject obj)
            => obj switch
            {
                MarkdownDocument doc => RenderDocument(doc),
                _ => throw new NotImplementedException("Unknown markdown object type")
            };

        private (RectTransform, HorizontalOrVerticalLayoutGroup) Block(string name, float spacing, bool vertical)
        {
            Logger.md.Debug($"Creating {(vertical ? "vertical" : "horizontal")} block node {name} with spacing {spacing}");

            var go = new GameObject(name);
            var transform = go.AddComponent<RectTransform>();
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
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

            Logger.md.Debug("TODO: implement document children");

            return transform;
        }
    }
}
