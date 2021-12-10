using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using BeatSaberMarkupLanguage;
using HMUI;
using IPA.Loader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BSMLUtils = BeatSaberMarkupLanguage.Utilities;

namespace IPA.ModList.BeatSaber.Utilities
{
    internal static class Helpers
    {
        private const string ResourcePrefix = "IPA.ModList.BeatSaber.Resources.";

        private static Sprite? defaultPluginIcon;
        public static Sprite DefaultPluginIcon => defaultPluginIcon ??= ReadImageFromSelf(ResourcePrefix + "mod_bsipa.png").AsSprite()!;

        private static Sprite? legacyPluginIcon;
        public static Sprite LegacyPluginIcon => legacyPluginIcon ??= ReadImageFromSelf(ResourcePrefix + "mod_ipa.png").AsSprite()!;

        private static Sprite? libraryIcon;
        public static Sprite LibraryIcon => libraryIcon ??= ReadImageFromSelf(ResourcePrefix + "library.png").AsSprite()!;

        public static Sprite BareManifestIcon => LibraryIcon;

        private static Sprite? librarySprite;
        public static Sprite LibrarySprite => librarySprite ??= LibraryIcon;

        private static Sprite? xSprite;
        public static Sprite XSprite => xSprite ??= ReadImageFromSelf(ResourcePrefix + "x.png").AsSprite()!;

        private static Sprite? oSprite;
        public static Sprite OSprite => oSprite ??= ReadImageFromSelf(ResourcePrefix + "o.png").AsSprite()!;

        private static Sprite? warnSprite;
        public static Sprite WarnSprite => warnSprite ??= ReadImageFromSelf(ResourcePrefix + "!.png").AsSprite()!;

        private static Sprite? roundedBackgroundSprite;

        public static Sprite RoundedBackgroundSprite => roundedBackgroundSprite ??=
            Resources.FindObjectsOfTypeAll<Image>().Last(x => x.gameObject.name == "MinScoreInfo" && x.sprite != null && x.sprite.name == "RoundRectPanel").sprite;

        private static Sprite? smallRoundedRectSprite;
        public static Sprite SmallRoundedRectSprite => smallRoundedRectSprite ??= LoadSmallRoundedRectSprite(false);

        private static Sprite? smallRoundedRectFlatSprite;
        public static Sprite SmallRoundedRectFlatSprite => smallRoundedRectFlatSprite ??= LoadSmallRoundedRectSprite(true);

        private static Sprite? tinyRoundedRectSprite;
        public static Sprite TinyRoundedRectSprite => tinyRoundedRectSprite ??= LoadTinyRoundedRectSprite();

        public static Texture2D? ReadImageFromSelf(string name) => ReadImageFromAssembly(typeof(Helpers).Assembly, name);

        public static Texture2D? ReadImageFromAssembly(Assembly assembly, string name)
        {
            if (assembly == null)
            {
                return null;
            }

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

        private static readonly ConcurrentQueue<Action> iconQueue = new();
        private static int coroutineShouldBeRunning = 0;
        private static int coroutineRunning = 0;

        public static void QueueReadPluginIcon(this PluginMetadata plugin, Action<Sprite> OnCompletion)
        {
            if (plugin.IsBare)
            {
                OnCompletion?.Invoke(BareManifestIcon);
                return;
            }

            iconQueue.Enqueue(() =>
            {
                Sprite? icon = null;
                if (plugin.IconName != null)
                {
                    icon = ReadImageFromAssembly(plugin.Assembly, plugin.IconName).AsSprite();
                }

                OnCompletion?.Invoke(icon is not null ? icon : DefaultPluginIcon);
            });

            if (coroutineRunning == 0)
            {
                _ = Interlocked.Exchange(ref coroutineShouldBeRunning, 1);
                _ = IPA.Utilities.Async.Coroutines.AsTask(IconLoadCoroutine());
            }
        }

        private static readonly YieldInstruction loadWait = new WaitForEndOfFrame();
        private static IEnumerator IconLoadCoroutine()
        {
            do
            {
                if (Interlocked.Exchange(ref coroutineRunning, 1) != 0)
                    yield break;
                _ = Interlocked.Exchange(ref coroutineShouldBeRunning, 0);

                while (iconQueue.TryDequeue(out var loader))
                {
                    loader?.Invoke();
                    yield return loadWait;
                }

                _ = Interlocked.Exchange(ref coroutineRunning, 0);
            }
            while (Interlocked.Exchange(ref coroutineShouldBeRunning, 0) == 1);
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

            //gameObj.SetActive(true);
            return textMesh;
        }

        public static TMP_FontAsset TMPFontFromUnityFont(Font font)
        {
            var asset = TMP_FontAsset.CreateFontAsset(font);
            asset.name = font.name;
            asset.hashCode = TMP_TextUtilities.GetSimpleHashCode(asset.name);
            return asset;
        }

        public static IEnumerable<T> SingleEnumerable<T>(T item) => Enumerable.Empty<T>().Append(item);

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