using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage;
using HMUI;
using IPA.Loader;
using IPA.ModList.BeatSaber.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BSMLUtils = BeatSaberMarkupLanguage.Utilities;

namespace IPA.ModList.BeatSaber.Helpers
{
    // TODO: Split this class up some Sprite cache and other util methods are separated.
    internal static class Helpers
    {
        private const string ResourcePrefix = "IPA.ModList.BeatSaber.Resources.";

        private const string DefaultPluginIconResourcePath = ResourcePrefix + "mod_bsipa.png";
        private static Sprite? _defaultPluginIcon;
        public static Sprite DefaultPluginIcon => _defaultPluginIcon ??= ReadImageFromSelf(DefaultPluginIconResourcePath).AsSprite()!;

        private const string LegacyPluginIconResourcePath = ResourcePrefix + "mod_ipa.png";
        private static Sprite? _legacyPluginIcon;
        public static Sprite LegacyPluginIcon => _legacyPluginIcon ??= ReadImageFromSelf(LegacyPluginIconResourcePath).AsSprite()!;

        private const string LibraryIconResourcePath = ResourcePrefix + "library.png";
        private static Sprite? _libraryIcon;
        public static Sprite LibraryIcon => _libraryIcon ??= ReadImageFromSelf(LibraryIconResourcePath).AsSprite()!;

        private static string BareManifestIconResourcePath => LibraryIconResourcePath;
        public static Sprite BareManifestIcon => LibraryIcon;

        private static Sprite? _librarySprite;
        public static Sprite LibrarySprite => _librarySprite ??= LibraryIcon;

        private const string XSpriteResourcePath = ResourcePrefix + "x.png";
        private static Sprite? _xSprite;
        public static Sprite XSprite => _xSprite ??= ReadImageFromSelf(XSpriteResourcePath).AsSprite()!;

        private const string OSpriteResourcePath = ResourcePrefix + "o.png";
        private static Sprite? _oSprite;
        public static Sprite OSprite => _oSprite ??= ReadImageFromSelf(ResourcePrefix + "o.png").AsSprite()!;

        private const string WarnSpriteResourcePath = ResourcePrefix + "!.png";
        private static Sprite? _warnSprite;
        public static Sprite WarnSprite => _warnSprite ??= ReadImageFromSelf(ResourcePrefix + "!.png").AsSprite()!;

        private static Sprite? _roundedBackgroundSprite;

        public static Sprite RoundedBackgroundSprite => _roundedBackgroundSprite ??=
            Resources.FindObjectsOfTypeAll<Image>().Last(x => x.gameObject.name == "MinScoreInfo" && x.sprite != null && x.sprite.name == "RoundRectPanel").sprite;

        private static Sprite? _smallRoundedRectSprite;

        public static Sprite SmallRoundedRectSprite => _smallRoundedRectSprite ??= LoadSmallRoundedRectSprite(false);

        private static Sprite? _smallRoundedRectFlatSprite;

        public static Sprite SmallRoundedRectFlatSprite => _smallRoundedRectFlatSprite ??= LoadSmallRoundedRectSprite(true);

        private static Sprite? _tinyRoundedRectSprite;

        public static Sprite TinyRoundedRectSprite => _tinyRoundedRectSprite ??= LoadTinyRoundedRectSprite();

        public static Texture2D? ReadImageFromSelf(string name) => ReadImageFromAssembly(typeof(Helpers).Assembly, name);

        public static Texture2D? ReadImageFromAssembly(Assembly assembly, string name)
        {
            if (assembly == null) return null;
            using var resourceStream = assembly.GetManifestResourceStream(name);
            if (resourceStream == null)
            {
                Plugin.Logger?.Warn($"Assembly {assembly.GetName().Name} does not have embedded resource {name}");
                return null;
            }

            var data = new byte[resourceStream.Length];
            var read = 0;
            while (read < data.Length)
            {
                read += resourceStream.Read(data, read, data.Length - read);
            }

            return BSMLUtils.LoadTextureRaw(data);
        }

        public static Sprite? AsSprite(this Texture2D? tex, float pixelsPerUnit = 100.0f, float? width = null, float? height = null)
        {
            return tex != null ? Sprite.Create(tex, new Rect(0, 0, width ?? tex.width, height ?? tex.height), new Vector2(0, 0), pixelsPerUnit) : null;
        }

        private static Sprite LoadSmallRoundedRectSprite(bool flatBottom = false)
        {
            var tex = ReadImageFromSelf(ResourcePrefix + "small-rounded-rect.png")!;
            return Sprite.Create(tex, new Rect(0, (flatBottom ? 32 : 0), tex.width, tex.height - (flatBottom ? 32 : 0)),
                pivot: Vector2.zero,
                border: new Vector4(32, flatBottom ? 1 : 32, 32, 32),
                pixelsPerUnit: 100f,
                extrude: 0,
                meshType: SpriteMeshType.FullRect);
        }

        private static Sprite LoadTinyRoundedRectSprite()
        {
            var tex = ReadImageFromSelf(ResourcePrefix + "tiny-rounded-rect.png")!;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                pivot: Vector2.zero,
                border: new Vector4(8, 8, 8, 8),
                pixelsPerUnit: 100f,
                extrude: 0,
                meshType: SpriteMeshType.FullRect);
        }

        public static Sprite ReadPluginIcon(this PluginInformation plugin) => ReadPluginIcon(plugin.Plugin);

        public static Sprite ReadPluginIcon(this PluginMetadata plugin)
        {
            if (plugin.IsBare)
            {
                return BareManifestIcon;
            }

            Sprite? icon = null;
            if (plugin.IconName != null)
            {
                icon = ReadImageFromAssembly(plugin.Assembly, plugin.IconName).AsSprite();
            }

            return icon != null ? icon : DefaultPluginIcon;
        }

        public static CurvedTextMeshPro CreateText(string text, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var gameObj = new GameObject("TextElement");
            gameObj.SetActive(false);

            var textMesh = gameObj.AddComponent<CurvedTextMeshPro>();
            textMesh.font = BeatSaberUI.MainTextFont;
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

        public static TMP_FontAsset TMPFontFromUnityFont(Font font)
        {
            var asset = TMP_FontAsset.CreateFontAsset(font);
            asset.name = font.name;
            asset.hashCode = TMP_TextUtilities.GetSimpleHashCode(asset.name);
            return asset;
        }

        public static IEnumerable<T> SingleEnumerable<T>(this T item) => Enumerable.Empty<T>().Append(item);

        public static IEnumerable<T?> AsNullable<T>(this IEnumerable<T> items) where T : struct => items.Select(i => new T?(i));

        public static T? AsNullable<T>(this T item) where T : struct => item;

        public static void Zero(RectTransform transform)
        {
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
            transform.anchoredPosition = Vector2.zero;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            transform.sizeDelta = Vector2.zero;
        }

        public static IEnumerable<T> AppendIf<T>(this IEnumerable<T> enumerable, bool append, T item) => append ? enumerable.Append(item) : enumerable;
    }
}