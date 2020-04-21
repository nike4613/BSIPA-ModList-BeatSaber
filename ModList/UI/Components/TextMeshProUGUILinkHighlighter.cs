using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IPA.ModList.BeatSaber.UI.Components
{
    [RequireComponent(typeof(TextMeshProUGUI), typeof(RectTransform))]
    public class TextMeshProUGUILinkHighlighter : MonoBehaviour
    {
        public TextMeshProUGUI TextMeshPro => GetComponent<TextMeshProUGUI>();
        public RectTransform RectTransform => GetComponent<RectTransform>();

        public Sprite BackgroundSprite { get; set; }
        public Image.Type BackgroundImageType { get; set; }
        public Color BackgroundImageColor { get; set; }

        private IEnumerable<TMP_LinkInfo> highlightLinks;
        public IEnumerable<TMP_LinkInfo> HighlightedLinks 
        { 
            get => highlightLinks;
            private set
            {
                if (!ValidateLinks(value))
                    throw new ArgumentException("Arugment must contain only links from the associated TextMeshPro object", nameof(value));
                highlightLinks = value;
                hasLinksChanged = true;
            } 
        }

        private Func<TMP_LinkInfo, bool> linkSelector;
        public Func<TMP_LinkInfo, bool> LinkSelector
        {
            get => linkSelector;
            set
            {
                linkSelector = value;
                IsDirty = true;
            }
        }

        public bool IsDirty { get; set; } = false;

        private bool hasLinksChanged = false;
        private readonly List<GameObject> createdObjects = new List<GameObject>();

        internal void Update()
        {
            if (IsDirty)
            {
                var tmp = TextMeshPro;
                HighlightedLinks = tmp.textInfo.linkInfo.Take(tmp.textInfo.linkCount);

                if (LinkSelector != null)
                    HighlightedLinks = HighlightedLinks.Where(LinkSelector);

                Logger.log.Debug($"Links are as follows: {string.Join(" ; ", HighlightedLinks.Select(l => $"'{l.GetLinkText()}' ({l.GetLinkID()})"))}");

                IsDirty = false;
            }
            if (hasLinksChanged && highlightLinks != null)
            {
                Clear();
                Render();
                hasLinksChanged = false;
            }
        }

        internal void OnDestroy()
            => Clear();

        private void Render()
        {
            var regions = CalculateHighlightedRegions();

            Logger.log.Debug(string.Join(" ; ", regions.Select(e => e.ToString())));
        }

        private IEnumerable<Extents> CalculateHighlightedRegions()
        {
            var tmp = TextMeshPro;

            Logger.log.Debug("Calculating highlighted regions");
            foreach (var link in HighlightedLinks)
            {
                Logger.log.Debug($"Looking at region '{link.GetLinkText()}' with ID '{link.GetLinkID()}'");

                var start = link.linkTextfirstCharacterIndex;
                var end = start + link.linkTextLength;

                var startLineIndex = FindLineContainingCharacter(start);
                var endLineIndex = FindLineContainingCharacter(end - 1, startLineIndex);

                var currentLineIndex = startLineIndex;
                var currentLine = tmp.textInfo.lineInfo[currentLineIndex];
                var lineExtent = currentLine.lineExtents;

                var currentExtent = new Extents(new Vector2(0, lineExtent.min.y), new Vector2(0, lineExtent.max.y));

                for (var charIdx = start; charIdx < end && charIdx < tmp.textInfo.characterCount; charIdx++)
                {
                    var charInfo = tmp.textInfo.characterInfo[charIdx];
                    var charExt = CharInfoExtent(charInfo);

                    if (!IsCharInLine(charIdx, currentLine))
                    {
                        currentLineIndex++;
                        currentLine = tmp.textInfo.lineInfo[currentLineIndex];
                        lineExtent = currentLine.lineExtents;

                        yield return currentExtent;
                        currentExtent = new Extents(new Vector2(0, lineExtent.min.y), new Vector2(0, lineExtent.max.y));
                    }

                    if (ExtentWidth(currentExtent) == 0f)
                        currentExtent.min = new Vector2(charExt.min.x, currentExtent.min.y);

                    currentExtent.max = new Vector2(charExt.max.x, currentExtent.max.y);
                }

                if (currentLineIndex != endLineIndex)
                    Logger.log.Warn("While calculating regions to highlight, ending line " +
                        $"{currentLineIndex} was not the expected end line {endLineIndex}");

                if (ExtentWidth(currentExtent) > 0f)
                    yield return currentExtent;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ExtentWidth(Extents extent)
            => extent.max.x - extent.min.x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ExtentHeight(Extents extent)
            => extent.max.y - extent.min.y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Extents CharInfoExtent(TMP_CharacterInfo chr)
            => new Extents(chr.bottomLeft, chr.topRight);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsCharInLine(int charIdx, TMP_LineInfo line)
            => charIdx >= line.firstCharacterIndex && charIdx <= line.lastCharacterIndex;

        private int FindLineContainingCharacter(int charIdx, int startAt = 0)
        {
            var tmp = TextMeshPro;
            if (charIdx < 0 || charIdx > tmp.textInfo.characterCount)
                return -1;
            for (int i = startAt; i < tmp.textInfo.lineCount && i < tmp.textInfo.lineInfo.Length; i++)
            {
                var info = tmp.textInfo.lineInfo[i];
                if (info.firstCharacterIndex <= charIdx && info.lastCharacterIndex >= charIdx)
                    return i;
            }
            return -1;
        }

        private static void ClearObject(Transform target)
        {
            foreach (Transform child in target)
            {
                ClearObject(child);
                Logger.md.Debug($"Destroying {child.name}");
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }

        private void Clear(bool destroying = false)
        {
            if (!destroying) gameObject.SetActive(false);
            foreach (var go in createdObjects)
            {
                ClearObject(go.transform);
                Destroy(go);
            }
            createdObjects.Clear();
            if (!destroying) gameObject.SetActive(true);
        }

        private bool ValidateLinks(IEnumerable<TMP_LinkInfo> links)
            => links.All(l => l.textComponent == TextMeshPro);
    }
}
