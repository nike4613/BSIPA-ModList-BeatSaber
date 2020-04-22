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
        private static IReadOnlyDictionary<string, Font> fontDict;

        public static Task SystemFontLoadTask { get; private set; }

        public static Task AsyncLoadSystemFonts()
        {
            if (IsInitialized) return Task.CompletedTask;
            if (SystemFontLoadTask != null) return SystemFontLoadTask;
            var task = UnityMainThreadTaskScheduler.Factory.StartNew(LoadSystemFonts).Unwrap();
            SystemFontLoadTask = task.ContinueWith(t => 
            {
                Logger.log.Debug("Font loading complete");
                if (!(Interlocked.CompareExchange(ref fontDict, t.Result, null) is null))
                    return DestroyDict(t.Result);
                return Task.CompletedTask;
            }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
            task.ContinueWith(t => Logger.log.Error($"Font loading errored: {t.Exception}"), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnRanToCompletion);
            return task;
        }

        public static void Destroy()
        {
            var fonts = Interlocked.Exchange(ref fontDict, null);
            UnityMainThreadTaskScheduler.Factory.StartNew(() => DestroyDict(fonts));
        }

        private static async Task DestroyDict(IReadOnlyDictionary<string, Font> dict)
        {
            foreach (var pair in dict)
            {
                Font.Destroy(pair.Value);
                await Task.Yield(); // yield back to the scheduler to allow more things to happen
            }
        }

        private static async Task<IReadOnlyDictionary<string, Font>> LoadSystemFonts()
        {
            var paths = Font.GetPathsToOSFonts();

            var fonts = new Dictionary<string, Font>(paths.Length, StringComparer.InvariantCultureIgnoreCase);

            foreach (var path in paths)
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".ttf" || ext == ".otf")
                {
                    try
                    {
                        Logger.log.Debug($"In file {path}");
                        var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                        var reader = new OpenTypeFontReader(fileStream);
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
                    }
                }

                var font = new Font(path);
                Logger.log.Debug($"'{path}' = '{font.name}'");
                if (fonts.ContainsKey(font.name))
                {
                    Logger.log.Warn($"Font dictionary already contains name {font.name}, appending '(dupe)'");
                    font.name += "(Dupe)";
                }
                fonts.Add(font.name, font);
                await Task.Yield();
            }

            return fonts;
        }

        public static bool IsInitialized => fontDict != null;

        public static bool TryGetFont(string name, out Font font)
        {
            if (!IsInitialized) throw new InvalidOperationException("FontManager not initialized");
            return fontDict.TryGetValue(name, out font);
        }
    }
}
