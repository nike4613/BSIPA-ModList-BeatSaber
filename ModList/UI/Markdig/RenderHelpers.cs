using Markdig.Syntax.Inlines;
using System.Text;

namespace IPA.ModList.BeatSaber.UI.Markdig
{
    internal static class RenderHelpers
    {
        public static EmphasisFlags GetEmphasisFlags(EmphasisInline em)
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
