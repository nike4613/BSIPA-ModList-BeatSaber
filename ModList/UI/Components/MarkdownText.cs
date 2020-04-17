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
            }
        }

        private static MarkdownPipeline pipeline = null;
            
        public static MarkdownPipeline Pipeline 
            => pipeline ??= new MarkdownPipelineBuilder()
                    .UseAutoLinks().UseListExtras().DisableHtml().UsePreciseSourceLocation()
                    .WithLogger(Logger.md)
                    .Build();

        private void Render()
        {
            var root = Markdown.Convert(Text, new UnityRenderer(), Pipeline);
        }

        private void Clear()
        {
            gameObject.SetActive(false);
            static void Clear(Transform target)
            {
                foreach (Transform child in target)
                {
                    Clear(child);
                    Logger.md.Debug($"Destroying {child.name}");
                    child.SetParent(null);
                    Destroy(child.gameObject);
                }
            }
            Clear(transform);
            gameObject.SetActive(true);
        }
    }
}
