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

        public Material BackgroundMaterial { get; set; }
        public Sprite BackgroundSprite { get; set; }
        public Image.Type BackgroundImageType { get; set; }
        public Color BackgroundImageColor { get; set; }

        /// <summary>
        /// The object pointed to by this property should have the same position and size as the object this is on
        /// </summary>
        public Transform BackgroundParent { get; set; }

        private IEnumerable<TMP_LinkInfo> highlightLinks;
        public IEnumerable<TMP_LinkInfo> HighlightedLinks 
        { 
            get => highlightLinks;
            private set
            {
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
            => Clear(true);

        private void Render()
        {
            var regions = CalculateHighlightedRegions();

#if DEBUG
            regions = regions.ToArray();
            Logger.log.Debug(string.Join(" ; ", regions.Select(e => e.ToString())));
#endif

            CreateHighlightObjects(regions);
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

        private void CreateHighlightObjects(IEnumerable<Extents> regions)
        {
            foreach (var region in regions)
            {
                var go = new GameObject("LinkHighlight");

                var transform = go.AddComponent<RectTransform>();
                transform.SetParent(BackgroundParent, false);
                transform.anchorMin = transform.anchorMax = new Vector2(.5f, .5f);
                transform.anchoredPosition = ExtentCenter(region);
                transform.sizeDelta = ExtentSize(region);

                var img = go.AddComponent<Image>();
                img.material = BackgroundMaterial;
                img.color = BackgroundImageColor;
                img.sprite = BackgroundSprite;
                img.type = BackgroundImageType;

                createdObjects.Add(go);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ExtentWidth(Extents extent)
            => extent.max.x - extent.min.x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ExtentHeight(Extents extent)
            => extent.max.y - extent.min.y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector2 ExtentCenter(Extents extent)
            => (extent.min + extent.max) / 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector2 ExtentSize(Extents extent)
            => extent.max - extent.min;

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
                if (go == null) continue;
                ClearObject(go.transform);
                Destroy(go);
            }
            createdObjects.Clear();
            if (!destroying) gameObject.SetActive(true);
        }
    }
}
