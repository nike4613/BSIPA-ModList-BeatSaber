using IPA.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
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
            textMesh.font = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(t => t.name == "Teko-Medium SDF No Glow"));
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

        public static IEnumerable<T> AppendIf<T>(this IEnumerable<T> enumerable, bool append, T item)
            => append ? enumerable.Append(item) : enumerable;
    }
}
