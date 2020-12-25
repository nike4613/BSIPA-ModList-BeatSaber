using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IPA.ModList.BeatSaber.UI.Components
{
    [RequireComponent(typeof(CurvedTextMeshPro), typeof(RectTransform))]
    public class TextMeshProUGUILinkBackgroundGenerator : MonoBehaviour
    {
        public TextMeshProUGUI TextMeshPro => GetComponent<TextMeshProUGUI>();
        public RectTransform RectTransform => GetComponent<RectTransform>();

        /// <summary>
        /// The object pointed to by this property should have the same position and size as the object this is on
        /// </summary>
        public Transform BackgroundParent { get; set; }

        private bool _useLineHeight = false;

        public bool UseLineHeight
        {
            get => _useLineHeight;
            set
            {
                _useLineHeight = value;
                _needsRerender = true;
            }
        }

        private bool _createSingleObjectForLinks = true;

        public bool CreateSingleObjectForLinks
        {
            get => _createSingleObjectForLinks;
            set
            {
                _createSingleObjectForLinks = value;
                _needsRerender = true;
            }
        }

        private readonly List<LinkType> linkTypes = new List<LinkType>();
        public IReadOnlyList<LinkType> LinkTypes => linkTypes;

        public void AddLinkType(LinkType type)
        {
            linkTypes.Add(type);
            IsDirty = true;
        }

        public void RemoveLinkType(LinkType type)
        {
            linkTypes.Remove(type);
            IsDirty = true;
        }

        public class LinkType : IEquatable<LinkType>
        {
            public object Data { get; }
            public Func<TMP_LinkInfo, bool> Selector { get; }

            public bool ShowBackground { get; set; } = false;
            public Material BackgroundMaterial { get; set; }
            public Sprite BackgroundSprite { get; set; }
            public Image.Type BackgroundImageType { get; set; }
            public Color BackgroundImageColor { get; set; }

            /// <summary>
            /// The values are left, right, top, bottom
            /// </summary>
            public Vector4 Padding { get; set; } = Vector4.zero;

            public float LineBreakPadding { get; set; } = 0f;

            public LinkType(Func<TMP_LinkInfo, bool> selector, object data)
            {
                Selector = selector;
                Data = data;
            }

            public override bool Equals(object other)
                => other is LinkType link && Equals(link);

            public bool Equals(LinkType other)
                => Selector.Equals(other.Selector);

            public override int GetHashCode()
                => -649874820 + EqualityComparer<Func<TMP_LinkInfo, bool>>.Default.GetHashCode(Selector);
        }

        public LinkType CreateLinkType(Func<TMP_LinkInfo, bool> selector, object data) => new LinkType(selector, data);

        public delegate void BackgroundObjectRendered(TMP_LinkInfo link, GameObject gameObject, object linkData);

        public event BackgroundObjectRendered? OnLinkSingleObjectRendered;
        public event BackgroundObjectRendered? OnLinkBackgroundRendered;

        public bool IsDirty { get; set; } = false;

        private bool _needsRerender = false;
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        private readonly struct LinkInfo
        {
            public readonly TMP_LinkInfo Link;
            public readonly LinkType Type;

            public LinkInfo(TMP_LinkInfo link, LinkType type)
            {
                Link = link;
                Type = type;
            }
        }

        private IEnumerable<LinkInfo> GetLinkInfosFor(IEnumerable<TMP_LinkInfo> tmpLinks, IEnumerable<LinkType> types)
        {
            foreach (var link in tmpLinks)
            {
                var type = types.FirstOrDefault(t => t.Selector(link));
                if (type != null)
                    yield return new LinkInfo(link, type);
            }
        }

        private IEnumerable<LinkInfo> renderedLinks;

        internal void Update()
        {
            if (IsDirty)
            {
                _needsRerender = true;

                var tmp = TextMeshPro;
                var allLinks = tmp.textInfo.linkInfo.Take(tmp.textInfo.linkCount);

                renderedLinks = GetLinkInfosFor(allLinks, LinkTypes);

                IsDirty = false;
            }

            if (_needsRerender && renderedLinks != null)
            {
                Clear();
                Render(renderedLinks);
                _needsRerender = false;
            }
        }

        internal void OnDestroy() => Clear(true);

        private struct LinkRenderInfo
        {
            public Extents[] HighlightRegions;
            public LinkType Type;
            public TMP_LinkInfo Link;
            public Extents FullExtents;

            public LinkRenderInfo(TMP_LinkInfo link, Extents singleExt, LinkType type)
            {
                Link = link;
                FullExtents = singleExt;
                HighlightRegions = null;
                Type = type;
            }

            public LinkRenderInfo(TMP_LinkInfo link, IEnumerable<Extents> extents, LinkType type)
            {
                Link = link;
                HighlightRegions = extents.ToArray();
                Type = type;

                var fullExt = new Extents(new Vector2(float.MaxValue, float.MaxValue), new Vector2(float.MinValue, float.MinValue));
                foreach (var region in HighlightRegions)
                {
                    fullExt.min = new Vector2(Math.Min(fullExt.min.x, region.min.x), Math.Min(fullExt.min.y, region.min.y));
                    fullExt.max = new Vector2(Math.Max(fullExt.max.x, region.max.x), Math.Max(fullExt.max.y, region.max.y));
                }

                FullExtents = fullExt;
            }

            public override string ToString() => $"{string.Join(":", HighlightRegions?.Select(r => r.ToString()) ?? Enumerable.Empty<string>())} over {FullExtents}";
        }

        private void Render(IEnumerable<LinkInfo> linkInfos)
        {
            var links = CalculateHighlightedRegions(linkInfos);
            CreateHighlightObjects(links);
        }

        private IEnumerable<Extents> FindExtentsForLink(TextMeshProUGUI tmp, LinkType type, TMP_LinkInfo link)
        {
            var start = link.linkTextfirstCharacterIndex;
            var end = start + link.linkTextLength;

            var startLineIndex = FindLineContainingCharacter(start);
            var endLineIndex = FindLineContainingCharacter(end - 1, startLineIndex);

            var currentLineIndex = startLineIndex;
            var currentLine = tmp.textInfo.lineInfo[currentLineIndex];
            var lineExtent = currentLine.lineExtents;

            Extents GetZeroedExtents(Extents lineExtent)
                => UseLineHeight
                    ? new Extents(new Vector2(0, lineExtent.min.y), new Vector2(0, lineExtent.max.y))
                    : new Extents(new Vector2(0, float.MaxValue), new Vector2(0, float.MinValue));

            static Extents PadExtents(Extents extent, Vector4 padding)
                => new Extents( // x = left, y = right, z = top, w = bottom
                    new Vector2(extent.min.x - padding.x, extent.min.y - padding.w),
                    new Vector2(extent.max.x + padding.y, extent.max.y + padding.z)
                );

            var currentExtent = GetZeroedExtents(lineExtent);

            bool hitLineBreak = false;

            for (var charIdx = start; charIdx < end && charIdx < tmp.textInfo.characterCount; charIdx++)
            {
                var charInfo = tmp.textInfo.characterInfo[charIdx];
                var charExt = CharInfoExtent(charInfo);

                if (!IsCharInLine(charIdx, currentLine))
                {
                    hitLineBreak = true;
                    currentLineIndex++;
                    currentLine = tmp.textInfo.lineInfo[currentLineIndex];
                    lineExtent = currentLine.lineExtents;

                    var pad = type.Padding;
                    pad.y += type.LineBreakPadding;
                    yield return PadExtents(currentExtent, pad);
                    currentExtent = GetZeroedExtents(lineExtent);
                }

                if (ExtentWidth(currentExtent) == 0f)
                    currentExtent.min = new Vector2(charExt.min.x - (hitLineBreak ? type.LineBreakPadding : 0), currentExtent.min.y);

                currentExtent.min = new Vector2(currentExtent.min.x, Math.Min(currentExtent.min.y, charExt.min.y));
                currentExtent.max = new Vector2(charExt.max.x, Math.Max(currentExtent.max.y, charExt.max.y));
            }

            if (currentLineIndex != endLineIndex)
            {
                // TODO: Inject logger for this
                // Logger.log.Warn($"While calculating regions to highlight, ending line {currentLineIndex} was not the expected end line {endLineIndex}");
            }

            if (ExtentWidth(currentExtent) > 0f)
            {
                yield return PadExtents(currentExtent, type.Padding);
            }
        }

        private IEnumerable<LinkRenderInfo> CalculateHighlightedRegions(IEnumerable<LinkInfo> links)
        {
            var tmp = TextMeshPro;

            if (CreateSingleObjectForLinks)
            {
                foreach (var link in links)
                    yield return new LinkRenderInfo(link.Link, FindExtentsForLink(tmp, link.Type, link.Link), link.Type);
            }
            else
            {
                foreach (var link in links)
                foreach (var ext in FindExtentsForLink(tmp, link.Type, link.Link))
                    yield return new LinkRenderInfo(link.Link, ext, link.Type);
            }
        }

        private GameObject CreateObjectForExtent(Extents ext, LinkType type, bool addImage)
        {
            var go = new GameObject(addImage ? "LinkBackground" : "LinkRegion");

            var transform = go.AddComponent<RectTransform>();
            transform.SetParent(BackgroundParent, false);
            transform.anchorMin = transform.anchorMax = new Vector2(.5f, .5f);
            transform.anchoredPosition = ExtentCenter(ext);
            transform.sizeDelta = ExtentSize(ext);

            if (addImage && type.ShowBackground)
            {
                var img = go.AddComponent<Image>();
                img.material = type.BackgroundMaterial;
                img.color = type.BackgroundImageColor;
                img.sprite = type.BackgroundSprite;
                img.type = type.BackgroundImageType;
            }

            _createdObjects.Add(go);

            return go;
        }

        private void CreateHighlightObjects(IEnumerable<LinkRenderInfo> links)
        {
            if (CreateSingleObjectForLinks)
            {
                foreach (var link in links)
                {
                    var bigGo = CreateObjectForExtent(link.FullExtents, link.Type, false);
                    foreach (var ext in link.HighlightRegions)
                    {
                        var go = CreateObjectForExtent(ext, link.Type, true);
                        OnLinkBackgroundRendered?.Invoke(link.Link, go, link.Type.Data);
                    }

                    OnLinkSingleObjectRendered?.Invoke(link.Link, bigGo, link.Type.Data);
                }
            }
            else
            {
                foreach (var link in links)
                {
                    var go = CreateObjectForExtent(link.FullExtents, link.Type, true);
                    OnLinkSingleObjectRendered?.Invoke(link.Link, go, link.Type.Data);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ExtentWidth(Extents extent)
            => extent.max.x - extent.min.x;

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
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }

        private void Clear(bool destroying = false)
        {
            if (!destroying) gameObject.SetActive(false);
            foreach (var go in _createdObjects)
            {
                if (go == null) continue;
                ClearObject(go.transform);
                Destroy(go);
            }

            _createdObjects.Clear();
            if (!destroying) gameObject.SetActive(true);
        }
    }
}