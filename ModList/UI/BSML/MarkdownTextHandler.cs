using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.TypeHandlers;
using IPA.ModList.BeatSaber.UI.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

namespace IPA.ModList.BeatSaber.UI.BSML
{
    [ComponentHandler(typeof(MarkdownText))]
    public class MarkdownTextHandler : TypeHandler<MarkdownText>
    {
        public override Dictionary<string, string[]> Props { get; } = new Dictionary<string, string[]>
        {
            {"text", new[] {"text", "value"}},
            {"childText", new[] {"_children"}},
            {"linkPressed", new[] {"link-pressed"}},
            {"linkColor", new[] {"link-color"}},
            {"autolinkColor", new[] {"autolink-color"}}
        };

        public override Dictionary<string, Action<MarkdownText, string>> Setters { get; } = new Dictionary<string, Action<MarkdownText, string>>
        {
            {"text", (md, text) => md.Text = text},
            {"childText", HandleTextAsChildren},
            {"linkColor", (md, col) => md.LinkColor = GetColor(col, md.LinkColor)},
            {"autolinkColor", (md, col) => md.AutolinkColor = GetColor(col, md.AutolinkColor)}
        };

        private static void HandleTextAsChildren(MarkdownText obj, string content)
        {
            var doc = new XmlDocument();
            try
            {
                doc.Load(XmlReader.Create(new StringReader($"<xmlWrapper>{content}</xmlWrapper>"), new XmlReaderSettings()));
                var node = doc.DocumentElement.FirstChild;
                if (node is XmlCharacterData cdata)
                {
                    content = cdata.Data;
                }
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                obj.Text = content;
            }
        }

        public override void HandleType(BSMLParser.ComponentTypeWithData componentType, BSMLParserParams parserParams)
        {
            base.HandleType(componentType, parserParams);

            var markdownText = componentType.component as MarkdownText;

            // run this manually once to ensure it overwrites the children
            if (componentType.data.TryGetValue("text", out string text))
            {
                markdownText.Text = text;
            }

            if (componentType.data.TryGetValue("linkPressed", out string selectCell))
            {
                markdownText.OnLinkPressed += (url, title) =>
                {
                    if (!parserParams.actions.TryGetValue(selectCell, out BSMLAction action))
                    {
                        throw new Exception("link-pressed action '" + componentType.data["onClick"] + "' not found");
                    }

                    action.Invoke(url, title);
                };
            }
        }

        private static Color GetColor(string colorStr, Color defaultCol)
        {
            if (ColorUtility.TryParseHtmlString(colorStr, out var color))
            {
                return color;
            }

            // TODO: Inject logger for this
            // Logger.md.Warn($"Color {colorStr}, is not a valid color.");
            return defaultCol;
        }
    }
}