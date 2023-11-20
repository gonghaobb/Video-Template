using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

#pragma warning disable 0414 // Disabled a few warnings for not yet implemented features.

namespace TMPro.EditorUtilities
{
    [CustomEditor(typeof(TMP_Settings))]
    public class TMP_SettingsEditor : Editor
    {
        internal class Styles
        {
            public static readonly GUIContent defaultFontAssetLabel = new GUIContent("Default Font Asset", "The Font Asset that will be assigned by default to newly created text objects when no Font Asset is specified.");
            public static readonly GUIContent defaultFontAssetPathLabel = new GUIContent("Path:        Resources/", "The relative path to a Resources folder where the Font Assets and Material Presets are located.\nExample \"Fonts & Materials/\"");

            public static readonly GUIContent fallbackFontAssetsLabel = new GUIContent("Fallback Font Assets", "The Font Assets that will be searched to locate and replace missing characters from a given Font Asset.");
            public static readonly GUIContent fallbackFontAssetsListLabel = new GUIContent("Fallback Font Assets List", "The Font Assets that will be searched to locate and replace missing characters from a given Font Asset.");

            public static readonly GUIContent fallbackMaterialSettingsLabel = new GUIContent("Fallback Material Settings");
            public static readonly GUIContent matchMaterialPresetLabel = new GUIContent("Match Material Presets");

            public static readonly GUIContent containerDefaultSettingsLabel = new GUIContent("Text Container Default Settings");

            public static readonly GUIContent textMeshProLabel = new GUIContent("TextMeshPro");
            public static readonly GUIContent textMeshProUiLabel = new GUIContent("TextMeshPro UI");
            public static readonly GUIContent enableRaycastTarget = new GUIContent("Enable Raycast Target");
            public static readonly GUIContent autoSizeContainerLabel = new GUIContent("Auto Size Text Container", "Set the size of the text container to match the text.");
            public static readonly GUIContent isTextObjectScaleStaticLabel = new GUIContent("Is Object Scale Static", "Disables calling InternalUpdate() when enabled. This can improve performance when text object scale is static.");

            public static readonly GUIContent textComponentDefaultSettingsLabel = new GUIContent("Text Component Default Settings");
            public static readonly GUIContent defaultFontSize = new GUIContent("Default Font Size");
            public static readonly GUIContent autoSizeRatioLabel = new GUIContent("Text Auto Size Ratios");
            public static readonly GUIContent minLabel = new GUIContent("Min");
            public static readonly GUIContent maxLabel = new GUIContent("Max");

            public static readonly GUIContent wordWrappingLabel = new GUIContent("Word Wrapping");
            public static readonly GUIContent kerningLabel = new GUIContent("Kerning");
            public static readonly GUIContent extraPaddingLabel = new GUIContent("Extra Padding");
            public static readonly GUIContent tintAllSpritesLabel = new GUIContent("Tint All Sprites");
            public static readonly GUIContent parseEscapeCharactersLabel = new GUIContent("Parse Escape Sequence");

            public static readonly GUIContent dynamicFontSystemSettingsLabel = new GUIContent("Dynamic Font System Settings");
            public static readonly GUIContent getFontFeaturesAtRuntime = new GUIContent("Get Font Features at Runtime", "Determines if Glyph Adjustment Data will be retrieved from font files at runtime when new characters and glyphs are added to font assets.");
            public static readonly GUIContent dynamicAtlasTextureGroup = new GUIContent("Dynamic Atlas Texture Group");

            public static readonly GUIContent missingGlyphLabel = new GUIContent("Missing Character Unicode", "The character to be displayed when the requested character is not found in any font asset or fallbacks.");
            public static readonly GUIContent disableWarningsLabel = new GUIContent("Disable warnings", "Disable warning messages in the Console.");

            public static readonly GUIContent defaultSpriteAssetLabel = new GUIContent("Default Sprite Asset", "The Sprite Asset that will be assigned by default when using the <sprite> tag when no Sprite Asset is specified.");
            public static readonly GUIContent missingSpriteCharacterUnicodeLabel = new GUIContent("Missing Sprite Unicode", "The unicode value for the sprite character to be displayed when the requested sprite character is not found in any sprite assets or fallbacks.");
            public static readonly GUIContent enableEmojiSupportLabel = new GUIContent("iOS Emoji Support", "Enables Emoji support for Touch Screen Keyboards on target devices.");
            //public static readonly GUIContent spriteRelativeScale = new GUIContent("Relative Scaling", "Determines if the sprites will be scaled relative to the primary font asset assigned to the text object or relative to the current font asset.");

            public static readonly GUIContent spriteAssetsPathLabel = new GUIContent("Path:        Resources/", "The relative path to a Resources folder where the Sprite Assets are located.\nExample \"Sprite Assets/\"");

            public static readonly GUIContent defaultStyleSheetLabel = new GUIContent("Default Style Sheet", "The Style Sheet that will be used for all text objects in this project.");
            public static readonly GUIContent styleSheetResourcePathLabel = new GUIContent("Path:        Resources/", "The relative path to a Resources folder where the Style Sheets are located.\nExample \"Style Sheets/\"");

            public static readonly GUIContent colorGradientPresetsLabel = new GUIContent("Color Gradient Presets", "The relative path to a Resources folder where the Color Gradient Presets are located.\nExample \"Color Gradient Presets/\"");
            public static readonly GUIContent colorGradientsPathLabel = new GUIContent("Path:        Resources/", "The relative path to a Resources folder where the Color Gradient Presets are located.\nExample \"Color Gradient Presets/\"");

            public static readonly GUIContent lineBreakingLabel = new GUIContent("Line Breaking for Asian languages", "The text assets that contain the Leading and Following characters which define the rules for line breaking with Asian languages.");
            public static readonly GUIContent koreanSpecificRules = new GUIContent("Korean Language Options");

            public static readonly GUIContent fallbackOSFontSettings = new GUIContent("[LoveEngine] Fallback OS Font Settings");
            public static readonly GUIContent miscSettings = new GUIContent("[LoveEngine] MISC Settings");
            //PicoVideo;TextMeshProExtension;WuJunLin;Start
            public static readonly GUIContent fallbackOSFontBestBitSettings = new GUIContent("Best Fit Settings");
            public static readonly GUIContent fallbackBestFitRangeLabel = new GUIContent("Fallback Best Fit Range");
            public static readonly GUIContent localFontPathsLabel = new GUIContent("Local Font Paths (Editor Only)");
            public static readonly GUIContent fallbackOSFontAtlasSizeLabel = new GUIContent("Fallback OS Font Atlas Size");
            public static readonly GUIContent fallbackOSFontAtlasWidthLabel = new GUIContent("Width");
            public static readonly GUIContent fallbackOSFontAtlasHeightLabel = new GUIContent("Height");
            public static readonly GUIContent firstLabel = new GUIContent("First");
            public static readonly GUIContent lastLabel = new GUIContent("Last");
            //PicoVideo;TextMeshProExtension;WuJunLin;End
        }

        SerializedProperty m_PropFontAsset;
        SerializedProperty m_PropDefaultFontAssetPath;
        SerializedProperty m_PropDefaultFontSize;
        SerializedProperty m_PropDefaultAutoSizeMinRatio;
        SerializedProperty m_PropDefaultAutoSizeMaxRatio;
        SerializedProperty m_PropDefaultTextMeshProTextContainerSize;
        SerializedProperty m_PropDefaultTextMeshProUITextContainerSize;
        SerializedProperty m_PropAutoSizeTextContainer;
        SerializedProperty m_PropEnableRaycastTarget;
        SerializedProperty m_PropIsTextObjectScaleStatic;

        SerializedProperty m_PropSpriteAsset;
        SerializedProperty m_PropMissingSpriteCharacterUnicode;
        //SerializedProperty m_PropSpriteRelativeScaling;
        SerializedProperty m_PropEnableEmojiSupport;
        SerializedProperty m_PropSpriteAssetPath;


        SerializedProperty m_PropStyleSheet;
        SerializedProperty m_PropStyleSheetsResourcePath;
        ReorderableList m_List;

        SerializedProperty m_PropColorGradientPresetsPath;

        SerializedProperty m_PropMatchMaterialPreset;
        SerializedProperty m_PropWordWrapping;
        SerializedProperty m_PropKerning;
        SerializedProperty m_PropExtraPadding;
        SerializedProperty m_PropTintAllSprites;
        SerializedProperty m_PropParseEscapeCharacters;
        SerializedProperty m_PropMissingGlyphCharacter;

        //SerializedProperty m_DynamicAtlasTextureManager;
        SerializedProperty m_GetFontFeaturesAtRuntime;

        SerializedProperty m_PropWarningsDisabled;

        SerializedProperty m_PropLeadingCharacters;
        SerializedProperty m_PropFollowingCharacters;
        SerializedProperty m_PropUseModernHangulLineBreakingRules;

        #region LoveEngine os fallback font support
        SerializedProperty m_shaderMobileSDF;
        SerializedProperty m_shaderMobileBitmap;
        SerializedProperty m_allowFallbackOSFont;
        //PicoVideo;TextMeshProExtension;WuJunLin;Start
        SerializedProperty m_LocalFontPaths;
        SerializedProperty m_FallbackOSFontSamplingPointSize;
        SerializedProperty m_FallbackOSFontAtlasWidth;
        SerializedProperty m_FallbackOSFontAtlasHeight;
        //PicoVideo;TextMeshProExtension;WuJunLin;End
        SerializedProperty m_fallbackOSFontPriority;
        SerializedProperty m_fallbackOSFontNormalWeight;
        SerializedProperty m_fallbackOSFontBoldWeight;
        SerializedProperty m_fallbackOSFontSetWeightOnlyPriorityFailed;
        SerializedProperty m_fallbackBestFitRange;
        SerializedProperty m_fallbackBestFitChars;
        #endregion

        #region misc settings
        SerializedProperty m_linkClickAreaSpread;
        #endregion

        private const string k_UndoRedo = "UndoRedoPerformed";

        public void OnEnable()
        {
            if (target == null)
                return;

            m_PropFontAsset = serializedObject.FindProperty("m_defaultFontAsset");
            m_PropDefaultFontAssetPath = serializedObject.FindProperty("m_defaultFontAssetPath");
            m_PropDefaultFontSize = serializedObject.FindProperty("m_defaultFontSize");
            m_PropDefaultAutoSizeMinRatio = serializedObject.FindProperty("m_defaultAutoSizeMinRatio");
            m_PropDefaultAutoSizeMaxRatio = serializedObject.FindProperty("m_defaultAutoSizeMaxRatio");
            m_PropDefaultTextMeshProTextContainerSize = serializedObject.FindProperty("m_defaultTextMeshProTextContainerSize");
            m_PropDefaultTextMeshProUITextContainerSize = serializedObject.FindProperty("m_defaultTextMeshProUITextContainerSize");
            m_PropAutoSizeTextContainer = serializedObject.FindProperty("m_autoSizeTextContainer");
            m_PropEnableRaycastTarget = serializedObject.FindProperty("m_EnableRaycastTarget");
            m_PropIsTextObjectScaleStatic = serializedObject.FindProperty("m_IsTextObjectScaleStatic");

            m_PropSpriteAsset = serializedObject.FindProperty("m_defaultSpriteAsset");
            m_PropMissingSpriteCharacterUnicode = serializedObject.FindProperty("m_MissingCharacterSpriteUnicode");
            //m_PropSpriteRelativeScaling = serializedObject.FindProperty("m_SpriteRelativeScaling");
            m_PropEnableEmojiSupport = serializedObject.FindProperty("m_enableEmojiSupport");
            m_PropSpriteAssetPath = serializedObject.FindProperty("m_defaultSpriteAssetPath");

            m_PropStyleSheet = serializedObject.FindProperty("m_defaultStyleSheet");
            m_PropStyleSheetsResourcePath = serializedObject.FindProperty("m_StyleSheetsResourcePath");


            m_PropColorGradientPresetsPath = serializedObject.FindProperty("m_defaultColorGradientPresetsPath");

            m_List = new ReorderableList(serializedObject, serializedObject.FindProperty("m_fallbackFontAssets"), true, true, true, true);

            m_List.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = m_List.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            };

            m_List.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, Styles.fallbackFontAssetsListLabel);
            };

            m_PropMatchMaterialPreset = serializedObject.FindProperty("m_matchMaterialPreset");

            m_PropWordWrapping = serializedObject.FindProperty("m_enableWordWrapping");
            m_PropKerning = serializedObject.FindProperty("m_enableKerning");
            m_PropExtraPadding = serializedObject.FindProperty("m_enableExtraPadding");
            m_PropTintAllSprites = serializedObject.FindProperty("m_enableTintAllSprites");
            m_PropParseEscapeCharacters = serializedObject.FindProperty("m_enableParseEscapeCharacters");
            m_PropMissingGlyphCharacter = serializedObject.FindProperty("m_missingGlyphCharacter");

            m_PropWarningsDisabled = serializedObject.FindProperty("m_warningsDisabled");

            //m_DynamicAtlasTextureManager = serializedObject.FindProperty("m_DynamicAtlasTextureGroup");
            m_GetFontFeaturesAtRuntime = serializedObject.FindProperty("m_GetFontFeaturesAtRuntime");

            m_PropLeadingCharacters = serializedObject.FindProperty("m_leadingCharacters");
            m_PropFollowingCharacters = serializedObject.FindProperty("m_followingCharacters");
            m_PropUseModernHangulLineBreakingRules = serializedObject.FindProperty("m_UseModernHangulLineBreakingRules");

            // added by LoveEngine deal.g
            m_shaderMobileSDF = serializedObject.FindProperty("m_shaderMobileSDF");
            m_shaderMobileBitmap = serializedObject.FindProperty("m_shaderMobileBitmap");
            m_allowFallbackOSFont = serializedObject.FindProperty("m_allowFallbackOSFont");
            //PicoVideo;TextMeshProExtension;WuJunLin;Start
            m_LocalFontPaths = serializedObject.FindProperty("m_LocalFontPaths");
            m_FallbackOSFontSamplingPointSize = serializedObject.FindProperty("m_FallbackOSFontSamplingPointSize");
            m_FallbackOSFontAtlasWidth = serializedObject.FindProperty("m_FallbackOSFontAtlasWidth");
            m_FallbackOSFontAtlasHeight = serializedObject.FindProperty("m_FallbackOSFontAtlasHeight");
            //PicoVideo;TextMeshProExtension;WuJunLin;End
            m_fallbackBestFitRange = serializedObject.FindProperty("m_fallbackBestFitRange");
            m_fallbackBestFitChars = serializedObject.FindProperty("m_fallbackBestFitChars");
            m_fallbackOSFontPriority = serializedObject.FindProperty("m_fallbackOSFontPriority");
            m_fallbackOSFontNormalWeight = serializedObject.FindProperty("m_fallbackOSFontNormalWeight");
            m_fallbackOSFontBoldWeight = serializedObject.FindProperty("m_fallbackOSFontBoldWeight");
            m_fallbackOSFontSetWeightOnlyPriorityFailed = serializedObject.FindProperty("m_fallbackOSFontSetWeightOnlyPriorityFailed");

            m_linkClickAreaSpread = serializedObject.FindProperty("m_linkClickAreaSpread");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            string evt_cmd = Event.current.commandName;

            float labelWidth = EditorGUIUtility.labelWidth;
            float fieldWidth = EditorGUIUtility.fieldWidth;

            // TextMeshPro Font Info Panel
            EditorGUI.indentLevel = 0;

            // FONT ASSET
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.defaultFontAssetLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropFontAsset, Styles.defaultFontAssetLabel);
            EditorGUILayout.PropertyField(m_PropDefaultFontAssetPath, Styles.defaultFontAssetPathLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            // FALLBACK FONT ASSETs
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.fallbackFontAssetsLabel, EditorStyles.boldLabel);
            m_List.DoLayoutList();

            GUILayout.Label(Styles.fallbackMaterialSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropMatchMaterialPreset, Styles.matchMaterialPresetLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            // MISSING GLYPHS
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.dynamicFontSystemSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_GetFontFeaturesAtRuntime, Styles.getFontFeaturesAtRuntime);
            EditorGUILayout.PropertyField(m_PropMissingGlyphCharacter, Styles.missingGlyphLabel);
            EditorGUILayout.PropertyField(m_PropWarningsDisabled, Styles.disableWarningsLabel);
            //EditorGUILayout.PropertyField(m_DynamicAtlasTextureManager, Styles.dynamicAtlasTextureManager);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            // TEXT OBJECT DEFAULT PROPERTIES
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.containerDefaultSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;

            EditorGUILayout.PropertyField(m_PropDefaultTextMeshProTextContainerSize, Styles.textMeshProLabel);
            EditorGUILayout.PropertyField(m_PropDefaultTextMeshProUITextContainerSize, Styles.textMeshProUiLabel);
            EditorGUILayout.PropertyField(m_PropEnableRaycastTarget, Styles.enableRaycastTarget);
            EditorGUILayout.PropertyField(m_PropAutoSizeTextContainer, Styles.autoSizeContainerLabel);
            EditorGUILayout.PropertyField(m_PropIsTextObjectScaleStatic, Styles.isTextObjectScaleStaticLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();

            GUILayout.Label(Styles.textComponentDefaultSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropDefaultFontSize, Styles.defaultFontSize);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel(Styles.autoSizeRatioLabel);
                EditorGUIUtility.labelWidth = 32;
                EditorGUIUtility.fieldWidth = 10;

                EditorGUI.indentLevel = 0;
                EditorGUILayout.PropertyField(m_PropDefaultAutoSizeMinRatio, Styles.minLabel);
                EditorGUILayout.PropertyField(m_PropDefaultAutoSizeMaxRatio, Styles.maxLabel);
                EditorGUI.indentLevel = 1;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;

            EditorGUILayout.PropertyField(m_PropWordWrapping, Styles.wordWrappingLabel);
            EditorGUILayout.PropertyField(m_PropKerning, Styles.kerningLabel);

            EditorGUILayout.PropertyField(m_PropExtraPadding, Styles.extraPaddingLabel);
            EditorGUILayout.PropertyField(m_PropTintAllSprites, Styles.tintAllSpritesLabel);

            EditorGUILayout.PropertyField(m_PropParseEscapeCharacters, Styles.parseEscapeCharactersLabel);

            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            // SPRITE ASSET
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.defaultSpriteAssetLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropSpriteAsset, Styles.defaultSpriteAssetLabel);
            EditorGUILayout.PropertyField(m_PropMissingSpriteCharacterUnicode, Styles.missingSpriteCharacterUnicodeLabel);
            EditorGUILayout.PropertyField(m_PropEnableEmojiSupport, Styles.enableEmojiSupportLabel);
            //EditorGUILayout.PropertyField(m_PropSpriteRelativeScaling, Styles.spriteRelativeScale);
            EditorGUILayout.PropertyField(m_PropSpriteAssetPath, Styles.spriteAssetsPathLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            // STYLE SHEET
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.defaultStyleSheetLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_PropStyleSheet, Styles.defaultStyleSheetLabel);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                TMP_StyleSheet styleSheet = m_PropStyleSheet.objectReferenceValue as TMP_StyleSheet;
                if (styleSheet != null)
                    styleSheet.RefreshStyles();
            }
            EditorGUILayout.PropertyField(m_PropStyleSheetsResourcePath, Styles.styleSheetResourcePathLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            // COLOR GRADIENT PRESETS
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.colorGradientPresetsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropColorGradientPresetsPath, Styles.colorGradientsPathLabel);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            // LINE BREAKING RULE
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.lineBreakingLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_PropLeadingCharacters);
            EditorGUILayout.PropertyField(m_PropFollowingCharacters);

            EditorGUILayout.Space();
            GUILayout.Label(Styles.koreanSpecificRules, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PropUseModernHangulLineBreakingRules, new GUIContent("Use Modern Line Breaking", "Determines if traditional or modern line breaking rules will be used to control line breaking. Traditional line breaking rules use the Leading and Following Character rules whereas Modern uses spaces for line breaking."));

            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            #region fallback os font settings
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.fallbackOSFontSettings, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_shaderMobileSDF);
            EditorGUILayout.PropertyField(m_shaderMobileBitmap);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_allowFallbackOSFont);
            //PicoVideo;TextMeshProExtension;WuJunLin;Start
            var rect = GUILayoutUtility.GetLastRect();
            rect.xMin = rect.xMax - 120;
            bool needReloadText = false;

            if(EditorGUI.Button(rect,new GUIContent("Reapply Settings")) || EditorGUI.EndChangeCheck())
            {
                TMP_Settings.ResetOSFallbackFontAssets();
                needReloadText = true;
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(m_FallbackOSFontSamplingPointSize);
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel(Styles.fallbackOSFontAtlasSizeLabel);
                EditorGUIUtility.labelWidth = 40;
                EditorGUIUtility.fieldWidth = 10;
                EditorGUI.indentLevel = 0;
                EditorGUILayout.PropertyField(m_FallbackOSFontAtlasWidth, Styles.fallbackOSFontAtlasWidthLabel);
                EditorGUILayout.PropertyField(m_FallbackOSFontAtlasHeight, Styles.fallbackOSFontAtlasHeightLabel);
                EditorGUI.indentLevel = 1;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
            
            EditorGUILayout.Space();
            
            m_allowFallbackOSFont.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(
                m_allowFallbackOSFont.isExpanded,
                Styles.fallbackOSFontBestBitSettings);
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUI.indentLevel = 1;
            if (m_allowFallbackOSFont.isExpanded)
            {
                EditorGUILayout.PropertyField(m_LocalFontPaths, Styles.localFontPathsLabel);
                EditorGUILayout.HelpBox(
                    "指定字体路径，便于在编辑器下还原预览真机上字体fallback的效果，建议配置上目标设备上所有的系统字体，不指定时会直接Fallback本机系统字体",
                    MessageType.Info);
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Load Fonts From Directory", GUILayout.Width(210)))
                    {
                        LoadFontPathsFromDirectory();
                    }

                    if (GUILayout.Button("Clear", GUILayout.Width(60)))
                    {
                        m_LocalFontPaths.ClearArray();
                    }
                }
                EditorGUILayout.EndHorizontal();

                DrawSplitter();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PrefixLabel(Styles.fallbackBestFitRangeLabel);
                    EditorGUIUtility.labelWidth = 35;
                    EditorGUIUtility.fieldWidth = 10;
                    EditorGUI.indentLevel = 0;
                    EditorGUILayout.PropertyField(m_fallbackBestFitRange.FindPropertyRelative("first"),
                        Styles.firstLabel);
                    EditorGUILayout.PropertyField(m_fallbackBestFitRange.FindPropertyRelative("last"),
                        Styles.lastLabel);
                    EditorGUI.indentLevel = 1;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = labelWidth;
                EditorGUIUtility.fieldWidth = fieldWidth;

                EditorGUILayout.HelpBox(
                    "由于一些系统字体布局和主字体格格不入，可将一些会fallback到的特定字符范围，以便保持和主字体一致。unicode字符编码搜索请访问: https://unicode-table.com/en。建议添所有加藏文字符范围: (3840,4095)，详见: https://unicode.org/charts/PDF/U0F00.pdf",
                    MessageType.Info);

                DrawSplitter();

                EditorGUILayout.PropertyField(m_fallbackBestFitChars);
                EditorGUILayout.HelpBox(
                    "由于一些系统字体布局和主字体格格不入，可将一些会fallback到的特定字符添加到此列表，以便保持和主字体一致。unicode字符编码搜索请访问: https://unicode-table.com/en",
                    MessageType.Info);

                DrawSplitter();

                EditorGUILayout.PropertyField(m_fallbackOSFontPriority);
                EditorGUILayout.HelpBox("优先筛选的字体关键字列表（忽略大小写），查找系统字体的时候，会按文件大小从高到低排序，然后从优先级列表中匹配出优先fallback的字体。" +
                                        "字体文件列表请访问： https://bytedance.feishu.cn/sheets/shtcnAMoDeHBoCN7cHjiQogb252",
                    MessageType.Info);

                DrawSplitter();

                EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.labelWidth, 300);
                EditorGUILayout.PropertyField(m_fallbackOSFontNormalWeight);
                EditorGUILayout.PropertyField(m_fallbackOSFontBoldWeight);
                EditorGUILayout.PropertyField(m_fallbackOSFontSetWeightOnlyPriorityFailed);
            }

            EditorGUI.indentLevel = 0;
            //PicoVideo;TextMeshProExtension;WuJunLin;End

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            #endregion
            
            #region love engine misc settings
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(Styles.miscSettings, EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(m_linkClickAreaSpread);
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            #endregion

            if (serializedObject.ApplyModifiedProperties() || evt_cmd == k_UndoRedo || needReloadText)
            {
                EditorUtility.SetDirty(target);
                TMPro_EventManager.ON_TMP_SETTINGS_CHANGED();
            }
        }
        
        //PicoVideo;TextMeshProExtension;WuJunLin;Start
        private void OnDisable()
        {
            TMP_Settings.ResetOSFallbackFontAssets();
        }
        
        private static void DrawSplitter()
        {
            GUILayout.Space(4);
            var rect = GUILayoutUtility.GetRect(1f, 1f);
            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
            GUILayout.Space(4);
        }

        private void LoadFontPathsFromDirectory()
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
            if (folderPath.Length == 0) {
                Debug.LogError("No folder selected");
                return;
            }
            string[] paths = Directory
                .GetFiles(folderPath, "*.*")
                .Where(s => s.EndsWith(".ttf") || s.EndsWith(".otf") || s.EndsWith(".ttc"))
                .ToArray();
            m_LocalFontPaths.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++)
            {
                m_LocalFontPaths.GetArrayElementAtIndex(i).stringValue = paths[i];
            }
        }
        //PicoVideo;TextMeshProExtension;WuJunLin;End
    }

#if UNITY_2018_3_OR_NEWER
    class TMP_ResourceImporterProvider : SettingsProvider
    {
        TMP_PackageResourceImporter m_ResourceImporter;

        public TMP_ResourceImporterProvider()
            : base("Project/TextMesh Pro", SettingsScope.Project)
        {
        }

        public override void OnGUI(string searchContext)
        {
            // Lazy creation that supports domain reload
            if (m_ResourceImporter == null)
                m_ResourceImporter = new TMP_PackageResourceImporter();

            m_ResourceImporter.OnGUI();
        }

        public override void OnDeactivate()
        {
            if (m_ResourceImporter != null)
                m_ResourceImporter.OnDestroy();
        }

        static UnityEngine.Object GetTMPSettings()
        {
            return Resources.Load<TMP_Settings>("TMP Settings");
        }

        [SettingsProviderGroup]
        static SettingsProvider[] CreateTMPSettingsProvider()
        {
            var providers = new List<SettingsProvider> { new TMP_ResourceImporterProvider() };

            if (GetTMPSettings() != null)
            {
                var provider = new AssetSettingsProvider("Project/TextMesh Pro/Settings", GetTMPSettings);
                provider.PopulateSearchKeywordsFromGUIContentProperties<TMP_SettingsEditor.Styles>();
                providers.Add(provider);
            }

            return providers.ToArray();
        }
    }
#endif
}
