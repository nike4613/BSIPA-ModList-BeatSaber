using IPA.ModList.BeatSaber.OpenType;
using IPA.Utilities.Async;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace IPA.ModList.BeatSaber
{
    public static class FontManager
    {
        private struct FontInfo
        {
            public string Path;
            public OpenTypeFont Info;
            public FontInfo(string path, OpenTypeFont info)
            {
                Path = path;
                Info = info;
            }
        }

        // family -> list of fonts in family
        private static Dictionary<string, List<FontInfo>> fontInfoLookup;
        // path -> loaded font object
        private static readonly Dictionary<string, Font> loadedFontsCache = new Dictionary<string, Font>();

        public static Task SystemFontLoadTask { get; private set; }

        public static Task AsyncLoadSystemFonts()
        {
            if (IsInitialized) return Task.CompletedTask;
            if (SystemFontLoadTask != null) return SystemFontLoadTask;
            var task = Task.Factory.StartNew(LoadSystemFonts).Unwrap();
            SystemFontLoadTask = task.ContinueWith(t => 
            {
                Logger.log.Debug("Font loading complete");
                Interlocked.CompareExchange(ref fontInfoLookup, t.Result, null);
                return Task.CompletedTask;
            }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
            task.ContinueWith(t => Logger.log.Error($"Font loading errored: {t.Exception}"), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnRanToCompletion);
            return task;
        }

        public static Task Destroy()
        {
            fontInfoLookup = null;
            return UnityMainThreadTaskScheduler.Factory.StartNew(() => DestroyDict(loadedFontsCache)).Unwrap()
                .ContinueWith(_ => loadedFontsCache.Clear());
        }

        private static async Task DestroyDict(IReadOnlyDictionary<string, Font> dict)
        {
            foreach (var pair in dict)
            {
                Font.Destroy(pair.Value);
                await Task.Yield(); // yield back to the scheduler to allow more things to happen
            }
        }

        private static async Task<Dictionary<string, List<FontInfo>>> LoadSystemFonts()
        {
            var paths = Font.GetPathsToOSFonts();

            var fonts = new Dictionary<string, List<FontInfo>>(paths.Length, StringComparer.InvariantCultureIgnoreCase);

            List<FontInfo> GetListForFamily(string family)
            {
                if (!fonts.TryGetValue(family, out var list))
                    fonts.Add(family, list = new List<FontInfo>());
                return list;
            }

            foreach (var path in paths)
            {
                // TODO: support TTC
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".ttf" || ext == ".otf")
                {
                    /*try
                    {
                        Logger.log.Debug($"In file {path}");
                        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                        using var reader = new OpenTypeFontReader(fileStream);
                        var tables = reader.ReadAllTables();
                        var nameTableDef = tables.First(t => t.TableTag == OpenTypeTag.NAME);
                        var nameTable = reader.TryReadTable(nameTableDef) as OpenTypeNameTable;

                        if (nameTable == null) 
                            Logger.log.Warn("Could not read name table of font");
                        else
                        {
                            foreach (var name in nameTable.NameRecords)
                            {
                                Logger.log.Debug($"record {name.PlatformID} {name.EncodingID} {name.LanguageID:X4} {name.NameID} = '{name.Value}'");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.log.Error(e);
                    }*/
                    try
                    {
                        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                        using var reader = new OpenTypeFontReader(fileStream);
                        var font = new OpenTypeFont(reader, lazyLoad: false);

                        Logger.log.Debug($"'{path}' = '{font.Family}' '{font.Subfamily}' ({font.FullName})");
                        var list = GetListForFamily(font.Family);
                        list.Add(new FontInfo(path, font));
                    }
                    catch (Exception e)
                    {
                        Logger.log.Error(e);
                    }
                }

                /*var font = new Font(path);
                Logger.log.Debug($"'{path}' = '{font.name}'");
                if (fonts.ContainsKey(font.name))
                {
                    Logger.log.Warn($"Font dictionary already contains name {font.name}, appending '(dupe)'");
                    font.name += "(Dupe)";
                }
                fonts.Add(font.name, font);*/
                await Task.Yield();
            }

            return fonts;
        }

        public static bool IsInitialized => fontInfoLookup != null;

        public static bool TryGetFont(string family, out Font font, string subfamily = null, bool fallbackIfNoSubfamily = false)
        {
            if (!IsInitialized) throw new InvalidOperationException("FontManager not initialized");

            if (subfamily == null) fallbackIfNoSubfamily = true;
            subfamily ??= "Regular"; // this is a typical standard default family name

            if (fontInfoLookup.TryGetValue(family, out var fonts))
            {
                var info = fonts.AsNullable().FirstOrDefault(p => p?.Info.Subfamily == subfamily);
                if (info == null)
                {
                    if (!fallbackIfNoSubfamily)
                    {
                        font = null;
                        return false;
                    }
                    else
                    {
                        info = fonts.First();
                    }
                }

                font = GetFontFromCacheOrLoad(info.Value);
                return true;
            }
            else
            {
                font = null;
                return false;
            }
        }

        private static Font GetFontFromCacheOrLoad(FontInfo info)
        {
            if (!loadedFontsCache.TryGetValue(info.Path, out var font))
            {
                font = new Font(info.Path);
                font.name = info.Info.FullName;
                loadedFontsCache.Add(info.Path, font);
            }
            return font;
        }
    }
}
