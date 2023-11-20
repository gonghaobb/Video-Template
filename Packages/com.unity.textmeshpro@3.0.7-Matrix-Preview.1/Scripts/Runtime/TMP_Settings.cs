using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Serialization;
using UnityEngine.TextCore.LowLevel;


#pragma warning disable 0649 // Disabled warnings related to serialized fields not assigned in this script but used in the editor.

namespace TMPro
{
    /// <summary>
    /// Scaling options for the sprites
    /// </summary>
    //public enum SpriteRelativeScaling
    //{
    //    RelativeToPrimary   = 0x1,
    //    RelativeToCurrent   = 0x2,
    //}

    [Serializable]
    struct CharRange
    {
        [SerializeField]
        public uint first;
        [SerializeField]
        public uint last;
    }

    [System.Serializable][ExcludeFromPresetAttribute]
    public class TMP_Settings : ScriptableObject
    {
        private static TMP_Settings s_Instance;
        
        //PicoVideo;TextMeshProExtension;WuJunLin;Begin
#if UNITY_EDITOR
        TMP_Settings()
        {
            //编辑器结束运行时重置一下fallback字体
            Application.quitting += ResetOSFallbackFontAssets;
        }
#endif
        //PicoVideo;TextMeshProExtension;WuJunLin;End

        /// <summary>
        /// Returns the release version of the product.
        /// </summary>
        public static string version
        {
            get { return "1.4.0"; }
        }

        /// <summary>
        /// Controls if Word Wrapping will be enabled on newly created text objects by default.
        /// </summary>
        public static bool enableWordWrapping
        {
            get { return instance.m_enableWordWrapping; }
        }
        [SerializeField]
        private bool m_enableWordWrapping;

        /// <summary>
        /// Controls if Kerning is enabled on newly created text objects by default.
        /// </summary>
        public static bool enableKerning
        {
            get { return instance.m_enableKerning; }
        }
        [SerializeField]
        private bool m_enableKerning;

        /// <summary>
        /// Controls if Extra Padding is enabled on newly created text objects by default.
        /// </summary>
        public static bool enableExtraPadding
        {
            get { return instance.m_enableExtraPadding; }
        }
        [SerializeField]
        private bool m_enableExtraPadding;

        /// <summary>
        /// Controls if TintAllSprites is enabled on newly created text objects by default.
        /// </summary>
        public static bool enableTintAllSprites
        {
            get { return instance.m_enableTintAllSprites; }
        }
        [SerializeField]
        private bool m_enableTintAllSprites;

        /// <summary>
        /// Controls if Escape Characters will be parsed in the Text Input Box on newly created text objects.
        /// </summary>
        public static bool enableParseEscapeCharacters
        {
            get { return instance.m_enableParseEscapeCharacters; }
        }
        [SerializeField]
        private bool m_enableParseEscapeCharacters;

        /// <summary>
        /// Controls if Raycast Target is enabled by default on newly created text objects.
        /// </summary>
        public static bool enableRaycastTarget
        {
            get { return instance.m_EnableRaycastTarget; }
        }
        [SerializeField]
        private bool m_EnableRaycastTarget = true;

        /// <summary>
        /// Determines if OpenType Font Features should be retrieved at runtime from the source font file.
        /// </summary>
        public static bool getFontFeaturesAtRuntime
        {
            get { return instance.m_GetFontFeaturesAtRuntime; }
        }
        [SerializeField]
        private bool m_GetFontFeaturesAtRuntime = true;

        /// <summary>
        /// The character that will be used as a replacement for missing glyphs in a font asset.
        /// </summary>
        public static uint missingGlyphCharacter
        {
            get { return instance.m_missingGlyphCharacter; }
            set { instance.m_missingGlyphCharacter = value; }
        }
        [SerializeField]
        private uint m_missingGlyphCharacter;

        /// <summary>
        /// Controls the display of warning message in the console.
        /// </summary>
        public static bool warningsDisabled
        {
            get { return instance.m_warningsDisabled; }
        }
        [SerializeField]
        private bool m_warningsDisabled;

        /// <summary>
        /// Returns the Default Font Asset to be used by newly created text objects.
        /// </summary>
        public static TMP_FontAsset defaultFontAsset
        {
            get { return instance.m_defaultFontAsset; }
        }
        [SerializeField]
        private TMP_FontAsset m_defaultFontAsset;

        /// <summary>
        /// The relative path to a Resources folder in the project.
        /// </summary>
        public static string defaultFontAssetPath
        {
            get { return instance.m_defaultFontAssetPath; }
        }
        [SerializeField]
        private string m_defaultFontAssetPath;

        /// <summary>
        /// The Default Point Size of newly created text objects.
        /// </summary>
        public static float defaultFontSize
        {
            get { return instance.m_defaultFontSize; }
        }
        [SerializeField]
        private float m_defaultFontSize;

        /// <summary>
        /// The multiplier used to computer the default Min point size when Text Auto Sizing is used.
        /// </summary>
        public static float defaultTextAutoSizingMinRatio
        {
            get { return instance.m_defaultAutoSizeMinRatio; }
        }
        [SerializeField]
        private float m_defaultAutoSizeMinRatio;

        /// <summary>
        /// The multiplier used to computer the default Max point size when Text Auto Sizing is used.
        /// </summary>
        public static float defaultTextAutoSizingMaxRatio
        {
            get { return instance.m_defaultAutoSizeMaxRatio; }
        }
        [SerializeField]
        private float m_defaultAutoSizeMaxRatio;

        /// <summary>
        /// The Default Size of the Text Container of a TextMeshPro object.
        /// </summary>
        public static Vector2 defaultTextMeshProTextContainerSize
        {
            get { return instance.m_defaultTextMeshProTextContainerSize; }
        }
        [SerializeField]
        private Vector2 m_defaultTextMeshProTextContainerSize;

        /// <summary>
        /// The Default Width of the Text Container of a TextMeshProUI object.
        /// </summary>
        public static Vector2 defaultTextMeshProUITextContainerSize
        {
            get { return instance.m_defaultTextMeshProUITextContainerSize; }
        }
        [SerializeField]
        private Vector2 m_defaultTextMeshProUITextContainerSize;

        /// <summary>
        /// Set the size of the text container of newly created text objects to match the size of the text.
        /// </summary>
        public static bool autoSizeTextContainer
        {
            get { return instance.m_autoSizeTextContainer; }
        }
        [SerializeField]
        private bool m_autoSizeTextContainer;

        /// <summary>
        /// Disables InternalUpdate() calls when true. This can improve performance when the scale of the text object is static.
        /// </summary>
        public static bool isTextObjectScaleStatic
        {
            get { return instance.m_IsTextObjectScaleStatic; }
            set { instance.m_IsTextObjectScaleStatic = value; }
        }
        [SerializeField]
        private bool m_IsTextObjectScaleStatic;


        /// <summary>
        /// Returns the list of Fallback Fonts defined in the TMP Settings file.
        /// </summary>
        public static List<TMP_FontAsset> fallbackFontAssets
        {
            get { return instance.m_fallbackFontAssets; }
        }
        [SerializeField]
        private List<TMP_FontAsset> m_fallbackFontAssets;

        /// <summary>
        /// Controls whether or not TMP will create a matching material preset or use the default material of the fallback font asset.
        /// </summary>
        public static bool matchMaterialPreset
        {
            get { return instance.m_matchMaterialPreset; }
        }
        [SerializeField]
        private bool m_matchMaterialPreset;

        /// <summary>
        /// The Default Sprite Asset to be used by default.
        /// </summary>
        public static TMP_SpriteAsset defaultSpriteAsset
        {
            get { return instance.m_defaultSpriteAsset; }
        }
        [SerializeField]
        private TMP_SpriteAsset m_defaultSpriteAsset;

        /// <summary>
        /// The relative path to a Resources folder in the project.
        /// </summary>
        public static string defaultSpriteAssetPath
        {
            get { return instance.m_defaultSpriteAssetPath; }
        }
        [SerializeField]
        private string m_defaultSpriteAssetPath;

        /// <summary>
        /// Determines if Emoji support is enabled in the Input Field TouchScreenKeyboard.
        /// </summary>
        public static bool enableEmojiSupport
        {
            get { return instance.m_enableEmojiSupport; }
            set { instance.m_enableEmojiSupport = value; }
        }
        [SerializeField]
        private bool m_enableEmojiSupport;

        /// <summary>
        /// The unicode value of the sprite that will be used when the requested sprite is missing from the sprite asset and potential fallbacks.
        /// </summary>
        public static uint missingCharacterSpriteUnicode
        {
            get { return instance.m_MissingCharacterSpriteUnicode; }
            set { instance.m_MissingCharacterSpriteUnicode = value; }
        }
        [SerializeField]
        private uint m_MissingCharacterSpriteUnicode;

        /// <summary>
        /// Determines if sprites will be scaled relative to the primary font asset assigned to the text object or relative to the current font asset.
        /// </summary>
        //public static SpriteRelativeScaling spriteRelativeScaling
        //{
        //    get { return instance.m_SpriteRelativeScaling; }
        //    set { instance.m_SpriteRelativeScaling = value; }
        //}
        //[SerializeField]
        //private SpriteRelativeScaling m_SpriteRelativeScaling = SpriteRelativeScaling.RelativeToCurrent;

        /// <summary>
        /// The relative path to a Resources folder in the project that contains Color Gradient Presets.
        /// </summary>
        public static string defaultColorGradientPresetsPath
        {
            get { return instance.m_defaultColorGradientPresetsPath; }
        }
        [SerializeField]
        private string m_defaultColorGradientPresetsPath;

        /// <summary>
        /// The Default Style Sheet used by the text objects.
        /// </summary>
        public static TMP_StyleSheet defaultStyleSheet
        {
            get { return instance.m_defaultStyleSheet; }
        }
        [SerializeField]
        private TMP_StyleSheet m_defaultStyleSheet;

        /// <summary>
        /// The relative path to a Resources folder in the project that contains the TMP Style Sheets.
        /// </summary>
        public static string styleSheetsResourcePath
        {
            get { return instance.m_StyleSheetsResourcePath; }
        }
        [SerializeField]
        private string m_StyleSheetsResourcePath;

        /// <summary>
        /// Text file that contains the leading characters used for line breaking for Asian languages.
        /// </summary>
        public static TextAsset leadingCharacters
        {
            get { return instance.m_leadingCharacters; }
        }
        [SerializeField]
        private TextAsset m_leadingCharacters;

        /// <summary>
        /// Text file that contains the following characters used for line breaking for Asian languages.
        /// </summary>
        public static TextAsset followingCharacters
        {
            get { return instance.m_followingCharacters; }
        }
        [SerializeField]
        private TextAsset m_followingCharacters;

        /// <summary>
        ///
        /// </summary>
        public static LineBreakingTable linebreakingRules
        {
            get
            {
                if (instance.m_linebreakingRules == null)
                    LoadLinebreakingRules();

                return instance.m_linebreakingRules;
            }
        }
        [SerializeField]
        private LineBreakingTable m_linebreakingRules;

        // TODO : Potential new feature to explore where multiple font assets share the same atlas texture.
        //internal static TMP_DynamicAtlasTextureGroup managedAtlasTextures
        //{
        //    get
        //    {
        //        if (instance.m_DynamicAtlasTextureGroup == null)
        //        {
        //            instance.m_DynamicAtlasTextureGroup = TMP_DynamicAtlasTextureGroup.CreateDynamicAtlasTextureGroup();
        //        }

        //        return instance.m_DynamicAtlasTextureGroup;
        //    }
        //}
        //[SerializeField]
        //private TMP_DynamicAtlasTextureGroup m_DynamicAtlasTextureGroup;

        /// <summary>
        /// Determines if Modern or Traditional line breaking rules should be used for Korean text.
        /// </summary>
        public static bool useModernHangulLineBreakingRules
        {
            get { return instance.m_UseModernHangulLineBreakingRules; }
            set { instance.m_UseModernHangulLineBreakingRules = value; }
        }
        [SerializeField]
        private bool m_UseModernHangulLineBreakingRules;

        /// <summary>
        /// Get a singleton instance of the settings class.
        /// </summary>
        public static TMP_Settings instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = Resources.Load<TMP_Settings>("TMP Settings");

                    #if UNITY_EDITOR
                    // Make sure TextMesh Pro UPM packages resources have been added to the user project
                    if (s_Instance == null)
                    {
                        // Open TMP Resources Importer
                        TMP_PackageResourceImporterWindow.ShowPackageImporterWindow();
                    }
                    #endif
                }

                return s_Instance;
            }
            set
            {
                if(s_Instance != value)
                {
                    s_Instance = value;
                    ResetOSFallbackFontAssets();
                }
            }
        }


        #region Link ClickArea Vertical Spread, added by LoveEngine deal.g
        /// <summary>
        /// LoveEngine deal.g: 点击区域扩充，默认值10，扩充效果明显，相对适中
        /// </summary>
        [SerializeField]
        protected Vector2 m_linkClickAreaSpread = new Vector2(10, 10);

        public static Vector2 linkClickAreaSpread
        {
            get { return instance.m_linkClickAreaSpread; }
            set { instance.m_linkClickAreaSpread = value; }
        }
        #endregion

        #region deal.g fallback to os default font asset support
        /// <summary>
        /// LoveEngine deal.g: Solve TMP_ShaderUtilities.cs Shader.Find return null at ab mode.
        /// </summary>
        public static Shader ShaderRef_MobileSDF => instance.m_shaderMobileSDF;
        public static Shader ShaderRef_MobileBitmap => instance.m_shaderMobileBitmap;

        public static bool AllowFallbackOSFont => instance.m_allowFallbackOSFont;
        
        //PicoVideo;TextMeshProExtension;WuJunLin;Start
#if UNITY_EDITOR
        [SerializeField]
        private string[] m_LocalFontPaths;
        
        public static string[] localFontPaths => instance.m_LocalFontPaths;
#endif
        [SerializeField]
        private int m_FallbackOSFontSamplingPointSize = 60;
        public static int fallbackOSFontSamplingPointSize => instance.m_FallbackOSFontSamplingPointSize;
        
        [SerializeField]
        private int m_FallbackOSFontAtlasWidth = 512;
        public static int fallbackOSFontAtlasWidth => instance.m_FallbackOSFontAtlasWidth;
        
        [SerializeField]
        private int m_FallbackOSFontAtlasHeight = 512;
        public static int fallbackOSFontAtlasHeight => instance.m_FallbackOSFontAtlasHeight;
        //PicoVideo;TextMeshProExtension;WuJunLin;End
        
        public static bool useBestFit(uint unicode) => (unicode >= instance.m_fallbackBestFitRange.first && unicode <= instance.m_fallbackBestFitRange.last) || ((instance.m_fallbackBestFitChars != null) && instance.m_fallbackBestFitChars.Contains(unicode));

        [SerializeField]
        private Shader m_shaderMobileSDF;

        [SerializeField]
        private Shader m_shaderMobileBitmap;

        [SerializeField]
        private bool m_allowFallbackOSFont;

        [SerializeField] 
        private List<string> m_fallbackOSFontPriority;

        /// <summary>
        /// 由于一些系统字体布局和主字体格格不入，可将一些会fallback到的特定字符添加到此列表，以便保持和主字体一致。unicode字符编码搜索请访问: https://unicode-table.com/en
        /// 例如： 当如下2个字符来自android系统字体NotoSansTibetan-Regular.ttf时和WQF.ttf字体
        ///  ༺: 3898
        ///  ༻: 3899
        ///  直接过滤所有藏文字体: 0x0F00-0x0FFF
        /// </summary>
        /// 
        [SerializeField]
        private CharRange m_fallbackBestFitRange;

        [SerializeField]
        private List<uint> m_fallbackBestFitChars;
        

        [SerializeField] 
        private float m_fallbackOSFontNormalWeight = 0f;
        
        [SerializeField]
        private float m_fallbackOSFontBoldWeight = 0.75f;

        [SerializeField]
        private bool m_fallbackOSFontSetWeightOnlyPriorityFailed;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // make sure m_shaderMobileSDF is not null when this asset is creating
            // it must be used when searching fallback OS font
            if (m_shaderMobileSDF == null || m_shaderMobileBitmap == null)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }

            if (m_shaderMobileSDF == null)
            {
                m_shaderMobileSDF = ShaderUtilities.ShaderRef_MobileSDF;
            }

            if (m_shaderMobileBitmap == null)
            {
                m_shaderMobileBitmap = ShaderUtilities.ShaderRef_MobileBitmap;
            }            

            if (m_shaderMobileSDF == null)
            {
                Debug.LogError("[LoveEngine][TMP]: cannot find ShaderRef_MobileSDF shader! Please import TMP resources with shaders.");
            }

            if (m_shaderMobileBitmap == null)
            {
                Debug.LogError("[LoveEngine][TMP]: cannot find ShaderRef_MobileBitmap shader! Please import TMP resources with shaders.");
            }
        }
#endif
        /// <summary>
        /// 系统fallback字体缓存, key: 缺失字符UTF32编码
        /// </summary>
        private static Dictionary<uint, TMP_FontAsset> s_osFallbackFontAssets;
        private static (string path, bool inPriority)[] s_osFontsPath;

        public static void ResetOSFallbackFontAssets()
        {
            if(s_osFallbackFontAssets != null)
            {
                foreach(var kv in s_osFallbackFontAssets)
                {
                    if(kv.Value)
                    {
                        if(Application.isPlaying)
                        {
                            Destroy(kv.Value);    
                        }
                        else
                        {
                            DestroyImmediate(kv.Value);
                        }
                    }
                }
                s_osFallbackFontAssets.Clear();
                s_osFallbackFontAssets = null;
            }

            s_osFontsPath = null;
        }

        /// <summary>
        /// 从系统Fallback字体中获取TMP_Character，目前会遍历系统所有字体
        /// </summary>
        /// <param name="unicode">字符的utf32编码</param>
        /// <param name="fontStyle"></param>
        /// <param name="fontWeight"></param>
        /// <param name="isAlternativeTypeface"></param>
        /// <param name="cacheHit"></param>
        /// <returns></returns>
        /// <remarks>测试代码: http://gitea.c4games.com:8086/C4Engine/LoveEngine/src/branch/master/Assets/LoveEngine/Tests/TempScripts/TestTMPHugeTexture.cs</remarks>
        public static TMP_Character GetCharacterFromOSFallbackFontAsset(uint unicode, FontStyles fontStyle, FontWeight fontWeight, out bool isAlternativeTypeface, out bool cacheHit)
        {
            isAlternativeTypeface = false;

            if (s_osFallbackFontAssets == null) s_osFallbackFontAssets = new Dictionary<uint, TMP_FontAsset>();

            TMP_FontAsset fontAsset = null;
            if (cacheHit = s_osFallbackFontAssets.TryGetValue(unicode, out fontAsset))
            {
                return fontAsset != null ? TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, fontAsset, false, fontStyle, fontWeight, out isAlternativeTypeface) : null;
            }

            // 先从已有fallback asset查找，防止创建太多Mesh
            foreach(var item in s_osFallbackFontAssets)
            {
                fontAsset = item.Value;
                if (fontAsset != null) {
                    var character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, fontAsset, false, fontStyle, fontWeight, out isAlternativeTypeface);
                    if (character != null)
                    {
                        s_osFallbackFontAssets.Add(unicode, fontAsset);
                        return character;
                    }
                }
            }

            // 从系统字体查找 
            var fontPaths = FilterOSFontsPath();
            if(fontPaths == null || fontPaths.Length == 0)
            {
                return null;
            }
            
            fontAsset = null;
            FontEngine.InitializeFontEngine();
            try
            {
                foreach (var (fontPath, inPriority) in fontPaths)
                {
                    //PicoVideo;TextMeshProExtension;WuJunLin;Start
                    if (FontEngine.LoadFontFace(fontPath, fallbackOSFontSamplingPointSize) == FontEngineError.Success)
                    {
                        if (FontEngine.TryGetGlyphIndex(unicode, out _))
                        {
                            fontAsset = TMP_FontAsset.CreateFontAssetInstance(null,3, GlyphRenderMode.SDFAA, fallbackOSFontAtlasWidth, fallbackOSFontAtlasHeight, AtlasPopulationMode.DynamicOS, true);
                            if (instance != null)
                            {
                                fontAsset.sourceFontFilePath = fontPath;
                                fontAsset.name = fontAsset.faceInfo.familyName + " - " + fontAsset.faceInfo.styleName;
                                if (!instance.m_fallbackOSFontSetWeightOnlyPriorityFailed || !inPriority)
                                {
                                    fontAsset.normalStyle = instance.m_fallbackOSFontNormalWeight;
                                    fontAsset.boldStyle = instance.m_fallbackOSFontBoldWeight;

                                    fontAsset.material.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
                                    fontAsset.material.SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyle);
                                }
                            }

                            break;
                        }
                    }
                    //PicoVideo;TextMeshProExtension;WuJunLin;End
                }
            }
            finally
            {
                FontEngine.UnloadAllFontFaces();
            }
            
            s_osFallbackFontAssets.Add(unicode, fontAsset);
            return fontAsset != null ? TMP_FontAssetUtilities.GetCharacterFromFontAsset(unicode, fontAsset, false, fontStyle, fontWeight, out isAlternativeTypeface) : null;
        }

        private static (string path, bool isPrority)[] FilterOSFontsPath()
        {
            if(s_osFontsPath == null)
            {
                // 这个 Unity API 返回的结果：
                // https://bytedance.feishu.cn/sheets/shtcnAMoDeHBoCN7cHjiQogb252

                // var pathArr = Directory.EnumerateFiles(@"E:\OSFonts\android"); // For reproduce text layout issues local fonts copy from device

                //PicoVideo;TextMeshProExtension;WuJunLin;Start
#if UNITY_EDITOR
                var pathArr = (localFontPaths != null && localFontPaths.Length != 0)
                    ? localFontPaths
                    : Font.GetPathsToOSFonts();
#else
                var pathArr = Font.GetPathsToOSFonts(); 
#endif
                //PicoVideo;TextMeshProExtension;WuJunLin;End
                if (pathArr != null && pathArr.Any())
                {
                    var paths = pathArr.Distinct(); // iOS 有大量重复，所以需要拍重
                    
                    var pathsWithSize = new List<(string path, long size)>();
                    foreach(var path in paths)
                    {
                        if(!File.Exists(path))
                        {
                            continue;
                        }
                        pathsWithSize.Add((path, new FileInfo(path).Length));
                    }
        
                    // 按大小排序，大的在前面
                    // 都已经缺字了，不得在优先在大的字体里面找么？
                    pathsWithSize.Sort((a, b) =>
                    {
                        if(a.size == b.size) return 0;
                        if(a.size > b.size) return -1;
                        return 1;
                    });

                    var fontsPath = pathsWithSize.Select(p => (p.path, false)).ToList();
                    
                    // 按照指定的关键字进行优先筛选
                    if (instance != null && 
                        instance.m_fallbackOSFontPriority != null &&
                        instance.m_fallbackOSFontPriority.Count > 0)
                    {
                        var priorityList = new List<(string path, bool inPriority)>();
                        foreach(var key in instance.m_fallbackOSFontPriority)
                        {
                            if(string.IsNullOrEmpty(key)) continue;
                            
                            for(int i = 0;i < fontsPath.Count;i++)
                            {
                                var p = fontsPath[i];
                                if(p.path.ToLower().Contains(key.ToLower()))
                                {
                                    fontsPath.RemoveAt(i--);
                                    priorityList.Add((p.path, true));
                                }
                            }
                        }
                        
                        priorityList.AddRange(fontsPath);
                        fontsPath = priorityList;
                    }

                    s_osFontsPath = fontsPath.ToArray();
                }
                else
                {
                    s_osFontsPath = Array.Empty<(string, bool)>();
                }
            }

            return s_osFontsPath;
        }

        // 查找系统文字的大小、支持字符等等，可以运行一下查找系统的字体，用于 Debug，目前输出的文档：
        // https://bytedance.feishu.cn/sheets/shtcnAMoDeHBoCN7cHjiQogb252
        public static void ListDeviceOSFonts(Action<string> logOutput)
        {
            var chars = new[]
            {
                "abcd1234",
                "中文紫禁之巅",
                "にほんご", // 日语
                "조선말", // 韩语
                "Tiếng Việt", // 越南语
                "Русский язык", // 俄语
                "عربي/عربى", // 阿拉伯语
                "Latīna", // 拉丁语
                "français", // 法语
                "Español", // 西班牙
            };
        
            var list = new List<(string path, long size)>();
            var fontPaths = Font.GetPathsToOSFonts().Distinct();
            foreach(var path in fontPaths)
            {
                if(!File.Exists(path))
                {
                    continue;
                }
                list.Add((path, new FileInfo(path).Length));
            }
        
            list.Sort((a, b) =>
            {
                if(a.size == b.size) return 0;
                if(a.size > b.size) return -1;
                return 1;
            });
        
            string GetSizeString(long size)
            {
                if(size > 1000 * 1000)
                {
                    return ((double)size / (1000 * 1000)).ToString("F2") + "MiB";
                }

                return ((double)size / 1000).ToString("F2") + "KiB";
            }
        
            FontEngine.InitializeFontEngine();
            try
            {
                var supported = new string[chars.Length];
                foreach(var (path, size) in list)
                {
                    for(var i = 0; i < supported.Length; i++)
                    {
                        supported[i] = "No";
                    }

                    if(FontEngine.LoadFontFace(path, 30) == FontEngineError.Success)
                    {
                        for(var i = 0; i < chars.Length; i++)
                        {
                            supported[i] = "Yes";
                            for(var i1 = 0; i1 < chars[i].Length; i1++)
                            {
                                if(!FontEngine.TryGetGlyphIndex(chars[i][i1], out _))
                                {
                                    supported[i] = "No";
                                    break;
                                }
                            }
                        }
                    }

                    logOutput?.Invoke(path + "," + GetSizeString(size) + "," + string.Join(",",supported));
                }
            }
            finally
            {
                FontEngine.UnloadAllFontFaces();
            }
        }
#endregion


        /// <summary>
        /// Static Function to load the TMP Settings file.
        /// </summary>
        /// <returns></returns>
        public static TMP_Settings LoadDefaultSettings()
        {
            if (s_Instance == null)
            {
                // Load settings from TMP_Settings file
                TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
                if (settings != null)
                    s_Instance = settings;
            }

            return s_Instance;
        }


        /// <summary>
        /// Returns the Sprite Asset defined in the TMP Settings file.
        /// </summary>
        /// <returns></returns>
        public static TMP_Settings GetSettings()
        {
            if (TMP_Settings.instance == null) return null;

            return TMP_Settings.instance;
        }


        /// <summary>
        /// Returns the Font Asset defined in the TMP Settings file.
        /// </summary>
        /// <returns></returns>
        public static TMP_FontAsset GetFontAsset()
        {
            if (TMP_Settings.instance == null) return null;

            return TMP_Settings.instance.m_defaultFontAsset;
        }


        /// <summary>
        /// Returns the Sprite Asset defined in the TMP Settings file.
        /// </summary>
        /// <returns></returns>
        public static TMP_SpriteAsset GetSpriteAsset()
        {
            if (TMP_Settings.instance == null) return null;

            return TMP_Settings.instance.m_defaultSpriteAsset;
        }


        /// <summary>
        /// Returns the Style Sheet defined in the TMP Settings file.
        /// </summary>
        /// <returns></returns>
        public static TMP_StyleSheet GetStyleSheet()
        {
            if (TMP_Settings.instance == null) return null;

            return TMP_Settings.instance.m_defaultStyleSheet;
        }


        public static void LoadLinebreakingRules()
        {
            //Debug.Log("Loading Line Breaking Rules for Asian Languages.");

            if (TMP_Settings.instance == null) return;

            if (s_Instance.m_linebreakingRules == null)
                s_Instance.m_linebreakingRules = new LineBreakingTable();

            s_Instance.m_linebreakingRules.leadingCharacters = GetCharacters(s_Instance.m_leadingCharacters);
            s_Instance.m_linebreakingRules.followingCharacters = GetCharacters(s_Instance.m_followingCharacters);
        }


        /// <summary>
        ///  Get the characters from the line breaking files
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static HashSet<uint> GetCharacters(TextAsset file)
        {
            HashSet<uint> hashSet = new HashSet<uint>();
            string text = file.text;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                hashSet.Add((uint)c);
                // Check to make sure we don't include duplicates
                // if (hashSet.Contains((uint)c) == false)
                // {
                    // hashSet.Add((uint)c);
                    //Debug.Log("Adding [" + (int)c + "] to dictionary.");
                // }
                //else
                //    Debug.Log("Character [" + text[i] + "] is a duplicate.");
            }

            return hashSet;
        }


        public class LineBreakingTable
        {
            public HashSet<uint> leadingCharacters;
            public HashSet<uint> followingCharacters;
        }
    }
}
