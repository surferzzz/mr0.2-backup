using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace CPRSimulation.EditorTools
{
    /// <summary>
    /// 为 VR 训练 UI 烘 TMP 字体资源（含 CJK 字形）。
    ///
    /// 为什么必须做这一步：legacy UnityEngine.UI.Text 有 OS 字体回退，
    /// 所以哪怕字体是不含 CJK 的 LegacyRuntime.ttf，日文/中文也能靠系统字体显示。
    /// TMP 没有这个回退 —— 它只认字体资源里烘好的字形。
    /// 因此「先迁移 TMP，后配字体」会让 CJK 全变成豆腐块，比迁移前更糟。
    ///
    /// 字形清单是扫本项目 .cs 的「字符串字面量」得来的，不含注释 ——
    /// 注释里的汉字永远不会渲染，算进去只是白占图集（实测多出 209 个）。
    ///
    /// 双字体分工：Noto Sans JP 作主字体，Noto Sans SC 只补 JP 缺的简体字形。
    /// 不用单个泛 CJK 字体，是因为那样日文汉字会被渲染成简体字形，
    /// 对日语教材来说排版是错的。
    /// </summary>
    public static class TMPJapaneseFontBuilder
    {
        private const string CharsetPath = "Assets/Scripts/CPR/Editor/TMP_CharacterSet_JA.txt";
        private const string ScriptRoot = "Assets/Scripts";
        private const string OutputDir = "Assets/Resources/VRTrainingUI/Fonts";
        private const string PrimaryName = "VRTrainingJP SDF";
        private const string FallbackName = "VRTrainingSC Fallback SDF";

        // 采样 48px + 2048：约 1000 个字形占 35 行 / 1890px，装得下且留余量。
        private const int SamplingPointSize = 48;
        private const int AtlasPadding = 5;
        private const int PrimaryAtlasSize = 2048;

        // fallback 只补几百个缺口字形，1024 足够（199 个约 12 行 / 648px）。
        private const int FallbackAtlasSize = 1024;

        private static readonly string[] JapaneseFontNames =
        {
            "NotoSansJP", "NotoSerifJP", "MPLUS", "MPLUSRounded",
            "IBMPlexSansJP", "ZenKakuGothic", "Kosugi", "Sawarabi"
        };

        private static readonly string[] ChineseFontNames =
        {
            "NotoSansSC", "NotoSerifSC", "NotoSansHans", "NotoSansCJKsc",
            "SourceHanSansSC", "SourceHanSansCN", "AlibabaPuHuiTi", "HarmonyOSSans"
        };

        // 正文字重。Bold 交给 TMP 的 FontStyles.Bold（着色器伪粗），
        // 避免为一个字重再多烘一张图集 —— Quest 上是实打实的显存。
        private static readonly string[] RegularWeightMarkers = { "Regular", "Medium", "Book" };

        [MenuItem("Tools/VR Training/Bake CJK TMP Fonts", priority = 100)]
        public static void Bake()
        {
            string charset = BuildCharset();
            if (string.IsNullOrEmpty(charset))
            {
                return;
            }

            Font primarySource = ResolveFont(JapaneseFontNames, "日文");
            if (primarySource == null)
            {
                return;
            }

            TMP_FontAsset primary = BakeAsset(primarySource, charset, PrimaryName,
                                              PrimaryAtlasSize, out string missing);
            if (primary == null)
            {
                return;
            }

            TMP_FontAsset fallback = null;
            if (!string.IsNullOrEmpty(missing))
            {
                fallback = BakeFallback(missing);
            }

            // fallback 必须先存盘再被主字体引用，否则引用存不住
            if (fallback != null)
            {
                primary.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            }

            Save(primary, PrimaryName);

            Debug.Log($"[TMPFont] 完成。主字体 {Describe(primary)}" +
                      (fallback != null ? $"，fallback {Describe(fallback)}" : "，无 fallback"));
        }

        private static string Describe(TMP_FontAsset a)
        {
            int glyphs = a.glyphTable != null ? a.glyphTable.Count : 0;
            int atlases = a.atlasTextures != null ? a.atlasTextures.Length : 0;
            return $"「{a.name}」{glyphs} 字形 / {atlases} 图集";
        }

        // ---------------------------------------------------------------- 字形清单

        /// <summary>
        /// 扫全项目 .cs 的字符串字面量，收集 CJK 字形，写出清单并返回。
        /// 写出文件是为了可 diff / 可 review，同时避免「改了 UI 文案忘记重新生成」。
        /// </summary>
        private static string BuildCharset()
        {
            if (!Directory.Exists(ScriptRoot))
            {
                Debug.LogError($"[TMPFont] 找不到脚本目录 {ScriptRoot}");
                return null;
            }

            var cjk = new SortedSet<char>();
            int scanned = 0;

            foreach (string file in Directory.GetFiles(ScriptRoot, "*.cs", SearchOption.AllDirectories))
            {
                string norm = file.Replace('\\', '/');
                if (norm.Contains("Archive~") || norm.Contains("_Backup") || norm.Contains("OldScripts"))
                {
                    continue;
                }

                scanned++;
                foreach (string literal in ExtractStringLiterals(File.ReadAllText(file, Encoding.UTF8)))
                {
                    foreach (char c in literal)
                    {
                        if (IsCjk(c))
                        {
                            cjk.Add(c);
                        }
                    }
                }
            }

            // HUD 同时要显示数字、cm、/min、% 等，ASCII 可打印区一起收进来
            var all = new SortedSet<char>(cjk);
            for (char c = (char)0x20; c < (char)0x7f; c++)
            {
                all.Add(c);
            }

            WriteCharsetFile(all);

            Debug.Log($"[TMPFont] 扫了 {scanned} 个 .cs（仅字符串字面量，跳过注释）：" +
                      $"CJK {cjk.Count} + ASCII 95 = {all.Count} 个字形 -> {CharsetPath}");

            return new string(all.ToArray());
        }

        private static void WriteCharsetFile(SortedSet<char> chars)
        {
            var sb = new StringBuilder();
            int col = 0;
            foreach (char c in chars)
            {
                sb.Append(c);
                if (++col == 64)   // 折行只为 diff 好看，烘制时会去掉
                {
                    sb.Append('\n');
                    col = 0;
                }
            }
            if (col != 0)
            {
                sb.Append('\n');
            }

            string dir = Path.GetDirectoryName(CharsetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(CharsetPath, sb.ToString(), new UTF8Encoding(false));
        }

        private static bool IsCjk(char c)
        {
            int o = c;
            return (o >= 0x3000 && o <= 0x9fff) || (o >= 0xff00 && o <= 0xffef);
        }

        /// <summary>
        /// 逐字符走一遍源码，把字符串字面量取出来。
        /// 逐字符是为了让注释和字符串不会互相误判 —— 比如注释里的引号、
        /// 或字符串里的 "//"，用正则都会出错。
        /// 处理：// 行注释、/* */ 块注释、@"" 逐字字符串、\uXXXX 转义。
        /// </summary>
        private static List<string> ExtractStringLiterals(string src)
        {
            var result = new List<string>();
            int i = 0, n = src.Length;

            while (i < n)
            {
                char c = src[i];

                if (c == '/' && i + 1 < n && src[i + 1] == '/')
                {
                    int nl = src.IndexOf('\n', i);
                    i = nl < 0 ? n : nl + 1;
                    continue;
                }

                if (c == '/' && i + 1 < n && src[i + 1] == '*')
                {
                    int end = src.IndexOf("*/", i + 2, System.StringComparison.Ordinal);
                    i = end < 0 ? n : end + 2;
                    continue;
                }

                if (c == '@' && i + 1 < n && src[i + 1] == '"')
                {
                    i += 2;
                    var buf = new StringBuilder();
                    while (i < n)
                    {
                        if (src[i] == '"')
                        {
                            if (i + 1 < n && src[i + 1] == '"')   // "" 表示一个引号
                            {
                                buf.Append('"');
                                i += 2;
                                continue;
                            }
                            i++;
                            break;
                        }
                        buf.Append(src[i++]);
                    }
                    result.Add(buf.ToString());
                    continue;
                }

                if (c == '"')
                {
                    i++;
                    var buf = new StringBuilder();
                    while (i < n)
                    {
                        if (src[i] == '\\' && i + 1 < n)
                        {
                            // 本项目大量使用 \uXXXX 写日文，必须解出来
                            if (src[i + 1] == 'u' && i + 5 < n &&
                                int.TryParse(src.Substring(i + 2, 4),
                                             System.Globalization.NumberStyles.HexNumber,
                                             System.Globalization.CultureInfo.InvariantCulture,
                                             out int cp))
                            {
                                buf.Append((char)cp);
                                i += 6;
                                continue;
                            }
                            i += 2;
                            continue;
                        }
                        if (src[i] == '"')
                        {
                            i++;
                            break;
                        }
                        buf.Append(src[i++]);
                    }
                    result.Add(buf.ToString());
                    continue;
                }

                i++;
            }

            return result;
        }

        // ---------------------------------------------------------------- 烘制

        private static TMP_FontAsset BakeAsset(Font source, string charset, string assetName,
                                               int atlasSize, out string missing)
        {
            missing = null;

            Debug.Log($"[TMPFont] 用「{source.name}」烘 {charset.Length} 个字形 " +
                      $"（采样 {SamplingPointSize}px，图集 {atlasSize}x{atlasSize}）…");

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                source,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                atlasSize,
                atlasSize,
                AtlasPopulationMode.Dynamic,   // 保持 Dynamic：清单外的意外字形运行时仍能补
                enableMultiAtlasSupport: true);

            if (asset == null)
            {
                Debug.LogError($"[TMPFont] CreateFontAsset 返回 null（{source.name}）");
                return null;
            }

            asset.name = assetName;

            // 预填图集，避免运行时逐字光栅化造成掉帧
            asset.TryAddCharacters(charset, out missing);

            if (!string.IsNullOrEmpty(missing))
            {
                Debug.LogWarning($"[TMPFont] 「{source.name}」缺 {missing.Length} 个字形：" +
                                 missing.Substring(0, Mathf.Min(80, missing.Length)));
            }

            return asset;
        }

        private static TMP_FontAsset BakeFallback(string missingFromPrimary)
        {
            Font source = ResolveFont(ChineseFontNames, "简体中文", quiet: true);
            if (source == null)
            {
                Debug.LogWarning(
                    $"[TMPFont] 主字体缺 {missingFromPrimary.Length} 个字形（基本都是简体字形，中文界面要用）。\n" +
                    "补法：下载 Noto Sans SC 放进 Assets/Fonts/ 再重跑本命令。\n" +
                    "  https://fonts.google.com/noto/specimen/Noto+Sans+SC （OFL）\n" +
                    "  用 static/NotoSansSC-Regular.ttf，不要用 VariableFont 版本。\n" +
                    "在补上之前，中文界面里这些字会显示成豆腐块。");
                return null;
            }

            TMP_FontAsset fb = BakeAsset(source, missingFromPrimary, FallbackName,
                                         FallbackAtlasSize, out string stillMissing);
            if (fb == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(stillMissing))
            {
                Debug.LogWarning($"[TMPFont] 连 fallback 也缺 {stillMissing.Length} 个字形：" +
                                 stillMissing.Substring(0, Mathf.Min(80, stillMissing.Length)));
            }

            Save(fb, FallbackName);
            return fb;
        }

        // ---------------------------------------------------------------- 字体查找

        private static Font ResolveFont(string[] familyNames, string label, bool quiet = false)
        {
            var candidates = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Font"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/TextMesh Pro/") || path.Contains("Archive~"))
                {
                    continue;
                }
                candidates.Add(path);
            }

            // 两趟：先「字族 + Regular 字重」，再退到「任意该字族」。
            // 分两趟是因为项目里同时放了 Regular 和 Bold，
            // 单趟匹配会依赖 FindAssets 的返回顺序，结果不确定。
            Font hit = Match(candidates, familyNames, true, label)
                       ?? Match(candidates, familyNames, false, label);
            if (hit != null)
            {
                return hit;
            }

            if (!quiet)
            {
                Debug.LogError(
                    $"[TMPFont] 项目里没有{label}字体。请下载一个 OFL 许可的放进 Assets/Fonts/ 再重跑：\n" +
                    "  · Noto Sans JP  https://fonts.google.com/noto/specimen/Noto+Sans+JP\n" +
                    "  · Noto Sans SC  https://fonts.google.com/noto/specimen/Noto+Sans+SC\n" +
                    "用 static/ 里的静态字重，不要用 VariableFont —— Unity 对可变字体只按默认实例处理。\n" +
                    $"（项目现有字体：{(candidates.Count == 0 ? "无" : string.Join(", ", candidates))}）");
            }

            return null;
        }

        private static Font Match(List<string> candidates, string[] familyNames,
                                  bool requireRegularWeight, string label)
        {
            foreach (string path in candidates)
            {
                string file = Path.GetFileNameWithoutExtension(path).Replace("-", "").Replace("_", "");

                bool family = familyNames.Any(f =>
                    file.IndexOf(f, System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (!family)
                {
                    continue;
                }

                if (requireRegularWeight &&
                    !RegularWeightMarkers.Any(w =>
                        file.IndexOf(w, System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    continue;
                }

                Debug.Log($"[TMPFont] {label}字体：{path}" +
                          (requireRegularWeight ? "" : "（无 Regular 字重，用了这个）"));
                return AssetDatabase.LoadAssetAtPath<Font>(path);
            }

            return null;
        }

        // ---------------------------------------------------------------- 存盘

        private static void Save(TMP_FontAsset fontAsset, string assetName)
        {
            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }

            string assetPath = $"{OutputDir}/{assetName}.asset";

            if (File.Exists(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);   // 重烘时避免子资源残留
            }

            AssetDatabase.CreateAsset(fontAsset, assetPath);

            // 图集贴图和材质必须作为子资源存进去，否则重启 Unity 后引用会丢
            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    Texture2D atlas = fontAsset.atlasTextures[i];
                    if (atlas == null)
                    {
                        continue;
                    }
                    atlas.name = $"{assetName} Atlas {i}";
                    AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                }
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = $"{assetName} Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
