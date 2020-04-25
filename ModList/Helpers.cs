using IPA.Loader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BSMLUtils = BeatSaberMarkupLanguage.Utilities;

namespace IPA.ModList.BeatSaber
{
    internal static class Helpers
    {
        private const string ResourcePrefix = "IPA.ModList.BeatSaber.Resources.";

        private static Texture2D defaultPluginIcon = null;
        public static Texture2D DefaultPluginIcon
            => defaultPluginIcon ??= ReadImageFromSelf(ResourcePrefix + "mod_bsipa.png");

        private static Texture2D legacyPluginIcon = null;
        public static Texture2D LegacyPluginIcon
            => legacyPluginIcon ??= ReadImageFromSelf(ResourcePrefix + "mod_ipa.png");

        private static Texture2D libraryIcon = null;
        public static Texture2D LibraryIcon
            => libraryIcon ??= ReadImageFromSelf(ResourcePrefix + "library.png");

        public static Texture2D BareManifestIcon => LibraryIcon;

        private static Sprite librarySprite = null;
        public static Sprite LibrarySprite => librarySprite ??= LibraryIcon.AsSprite();

        private static Sprite xSprite = null;
        public static Sprite XSprite
            => xSprite ??= ReadImageFromSelf(ResourcePrefix + "x.png").AsSprite();
        private static Sprite oSprite = null;
        public static Sprite OSprite
            => oSprite ??= ReadImageFromSelf(ResourcePrefix + "o.png").AsSprite();
        private static Sprite warnSprite = null;
        public static Sprite WarnSprite
            => warnSprite ??= ReadImageFromSelf(ResourcePrefix + "!.png").AsSprite();

        private static Sprite roundedBackgroundSprite = null;
        public static Sprite RoundedBackgroundSprite
            => roundedBackgroundSprite ??= Resources.FindObjectsOfTypeAll<Image>()
                    .Last(x => x.gameObject.name == "MinScoreInfo" && x.sprite?.name == "RoundRectPanel").sprite;

        private static Sprite smallRoundedRectSprite = null;
        public static Sprite SmallRoundedRectSprite
            => smallRoundedRectSprite ??= LoadSmallRoundedRectSprite();

        private static Sprite tinyRoundedRectSprite = null;
        public static Sprite TinyRoundedRectSprite
            => tinyRoundedRectSprite ??= LoadTinyRoundedRectSprite();

        private static TMP_FontAsset tekoMediumFont = null;
        public static TMP_FontAsset TekoMediumFont
            => tekoMediumFont ??= Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(t => t.name == "Teko-Medium SDF No Glow");

        private static TMP_FontAsset tekoMediumArialFallback = null;
        public static TMP_FontAsset TekoMediumArialFallback
            => tekoMediumArialFallback ??= CreateTekoWithArialFallback();

        public static Texture2D ReadImageFromSelf(string name)
            => ReadImageFromAssembly(typeof(Helpers).Assembly, name);
        public static Texture2D ReadImageFromAssembly(Assembly assembly, string name)
        {
            if (assembly == null) return null;
            using var resourceStream = assembly.GetManifestResourceStream(name);
            if (resourceStream == null)
            {
                Logger.log.Warn($"Assembly {assembly.GetName().Name} does not have embedded resource {name}");
                return null;
            }
            var data = new byte[resourceStream.Length];
            int read = 0;
            while (read < data.Length)
                read += resourceStream.Read(data, read, data.Length - read);
            return BSMLUtils.LoadTextureRaw(data);
        }

        public static Sprite AsSprite(this Texture2D tex, float PixelsPerUnit = 100.0f, float? width = null, float? height = null)
        {
            if (tex != null)
                return Sprite.Create(tex, new Rect(0, 0, width ?? tex.width, height ?? tex.height), new Vector2(0, 0), PixelsPerUnit);
            return null;
        }

        private static Sprite LoadSmallRoundedRectSprite()
        {
            var tex = ReadImageFromSelf(ResourcePrefix + "small-rounded-rect.png");
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                pivot: Vector2.zero,
                border: new Vector4(32, 32, 32, 32),
                pixelsPerUnit: 100f,
                extrude: 0,
                meshType: SpriteMeshType.FullRect);
        }
        private static Sprite LoadTinyRoundedRectSprite()
        {
            var tex = ReadImageFromSelf(ResourcePrefix + "tiny-rounded-rect.png");
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                pivot: Vector2.zero,
                border: new Vector4(8, 8, 8, 8),
                pixelsPerUnit: 100f,
                extrude: 0,
                meshType: SpriteMeshType.FullRect);
        }

        public static Texture2D ReadPluginIcon(PluginInformation plugin) => ReadPluginIcon(plugin.Plugin);
        public static Texture2D ReadPluginIcon(PluginMetadata plugin)
        {
            if (plugin.IsBare) return BareManifestIcon;

            Texture2D icon = null;
            if (plugin.IconName != null)
                icon = ReadImageFromAssembly(plugin.Assembly, plugin.IconName);

            return icon ?? DefaultPluginIcon;
        }

        public static TextMeshProUGUI CreateText(/*RectTransform parent, */string text, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var gameObj = new GameObject("TextElement");
            gameObj.SetActive(false);

            var textMesh = gameObj.AddComponent<TextMeshProUGUI>();
            textMesh.font = TekoMediumFont;
            //textMesh.rectTransform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = 4;
            textMesh.color = Color.white;

            textMesh.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            textMesh.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            textMesh.rectTransform.sizeDelta = sizeDelta;
            textMesh.rectTransform.anchoredPosition = anchoredPosition;

            gameObj.SetActive(true);
            return textMesh;
        }

        public static TMP_FontAsset CreateFixedUIFontClone(TMP_FontAsset font)
        {
            var matCopy = GameObject.Instantiate(TekoMediumFont.material);
            matCopy.mainTexture = font.material.mainTexture;
            matCopy.mainTextureOffset = font.material.mainTextureOffset;
            matCopy.mainTextureScale = font.material.mainTextureScale;
            font.material = matCopy;
            var copy = GameObject.Instantiate(font);
            copy.name = font.name;
            copy.hashCode = font.hashCode;
            return copy;
        }

        private static TMP_FontAsset CreateTekoWithArialFallback(bool fixForUI = true)
        {
            var teko = GameObject.Instantiate(TekoMediumFont);
            teko.name = $"{TekoMediumFont.name} With Arial Fallback";
            teko.hashCode = TMP_TextUtilities.GetSimpleHashCode(teko.name);

            if (!FontManager.TryGetFont("Arial", out var ufont))
            {
                Logger.log.Warn("Cannot find system font Arial to use as a fallback!");
                return teko;
            }

            var fallback = TMPFontFromUnityFont(ufont);
            if (fixForUI)
                fallback = CreateFixedUIFontClone(fallback);

            teko.fallbackFontAssetTable.Add(fallback);
            return teko;
        }

        public static TMP_FontAsset TMPFontFromUnityFont(Font font)
        {
            var asset = TMP_FontAsset.CreateFontAsset(font);
            asset.name = font.name;
            asset.hashCode = TMP_TextUtilities.GetSimpleHashCode(asset.name);
            return asset;
        }

        public static IEnumerable<T> SingleEnumerable<T>(this T item)
            => new SingleValueEnumerable<T>(item);

        public static IEnumerable<T?> AsNullable<T>(this IEnumerable<T> items) where T : struct
            => items.Select(i => new T?(i));

        public static T? AsNullable<T>(this T item) where T : struct => item;

        private class SingleValueEnumerable<T> : IEnumerable<T>
        {
            private readonly T value;
            public SingleValueEnumerable(T val)
                => value = val;
            public IEnumerator<T> GetEnumerator() => new Enumerator(value);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private class Enumerator : IEnumerator<T>
            {
                private byte state = 0;

                public Enumerator(T val)
                    => Current = val;

                public T Current { get; }

                object IEnumerator.Current => Current;

                public void Dispose() { }

                public bool MoveNext()
                    => state++ < 1;

                public void Reset()
                    => state = 0;
            }
        }

        public static void Zero(RectTransform transform)
        {
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
            transform.anchoredPosition = Vector2.zero;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            transform.sizeDelta = Vector2.zero;
        }

        public static IEnumerable<T> AppendIf<T>(this IEnumerable<T> enumerable, bool append, T item)
            => append ? enumerable.Append(item) : enumerable;
    }
}
