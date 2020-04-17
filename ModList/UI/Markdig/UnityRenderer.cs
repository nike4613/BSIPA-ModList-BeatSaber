using Markdig.Renderers;
using Markdig.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.ModList.BeatSaber.UI.Markdig
{
    internal class UnityRenderer : IMarkdownRenderer
    {
        public ObjectRendererCollection ObjectRenderers => throw new NotImplementedException();

        public event Action<IMarkdownRenderer, MarkdownObject> ObjectWriteBefore;
        public event Action<IMarkdownRenderer, MarkdownObject> ObjectWriteAfter;

        public object Render(MarkdownObject markdownObject)
        {
            throw new NotImplementedException();
        }
    }
}
