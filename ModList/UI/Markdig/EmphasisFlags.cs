using System;

namespace IPA.ModList.BeatSaber.UI.Markdig
{
    [Flags]
    internal enum EmphasisFlags
    {
        None, Italic = 1, Bold = 2, Strike = 4, Underline = 8
    }
}
