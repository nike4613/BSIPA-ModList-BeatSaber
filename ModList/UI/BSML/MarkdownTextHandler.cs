using BeatSaberMarkupLanguage.TypeHandlers;
using IPA.ModList.BeatSaber.UI.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace IPA.ModList.BeatSaber.UI.BSML
{
    [ComponentHandler(typeof(MarkdownText))]
    public class MarkdownTextHandler : TypeHandler<MarkdownText>
    {
        public override Dictionary<string, string[]> Props { get; } = new Dictionary<string, string[]>
        {
            { "text", new[] { "text", "value" } },
            { "childText", new[] { "_children" } }
        };

        public override Dictionary<string, Action<MarkdownText, string>> Setters { get; } 
            = new Dictionary<string, Action<MarkdownText, string>>
            {
                { "text", (md, text) => md.Text = text },
                { "childText", HandleTextAsChildren }
            };

        private static void HandleTextAsChildren(MarkdownText obj, string content)
        {
            var doc = new XmlDocument();
            try
            {
                doc.Load(XmlReader.Create(new StringReader($"<xmlWrapper>{content}</xmlWrapper>"), new XmlReaderSettings()));
                var node = doc.DocumentElement.FirstChild;
                if (node is XmlCharacterData cdata)
                    content = cdata.Data;
            }
            catch (Exception) { }
            finally
            {
                obj.Text = content;
            }
        }
    }
}
