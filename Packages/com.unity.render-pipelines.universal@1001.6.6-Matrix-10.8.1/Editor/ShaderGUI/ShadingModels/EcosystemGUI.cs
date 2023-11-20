using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    public class EcosystemGUI
    {
        public static class Styles
        {
            public static string[] weatherModeOptions = { "None", "Rain", "Snow" };

            public static readonly GUIContent ecosystemInputs = new GUIContent("Ecosystem Inputs",
                "These settings affect the Ecosystem effect.");
            public static readonly GUIContent weatherModeName = new GUIContent("WeatherType");

            public static readonly GUIContent globalCloudShadow = new GUIContent("Global Cloud Shadow",
                "Enable Global Cloud Shadow");

            public static readonly GUIContent cloudShadowIntensity = new GUIContent("Cloud Shadow Intensity",
                "Global Cloud Shadow Intensity");
        }

        public struct EcosystemProperties
        {
            public MaterialProperty weatherMode;
            public MaterialProperty disableHeightmapRenderer;
            public MaterialProperty customFogFragment;
            public MaterialProperty globalCloudShadow;
            public MaterialProperty cloudShadowIntensity;

            public EcosystemProperties(MaterialProperty[] properties)
            {
                weatherMode = BaseShaderGUI.FindProperty("_WeatherMode", properties, false);
                disableHeightmapRenderer = BaseShaderGUI.FindProperty("_DisableHeightmapRenderer", properties, false);
                customFogFragment = BaseShaderGUI.FindProperty("_CustomFogFragment", properties, false);
                globalCloudShadow = BaseShaderGUI.FindProperty("_GlobalCloudShadow", properties);
                cloudShadowIntensity = BaseShaderGUI.FindProperty("_CloudShadowIntensity", properties);
            }
        }

        public static void DoEcosystemArea(EcosystemProperties properties, MaterialEditor materialEditor,
            Material material)
        {
            if (properties.weatherMode != null)
            {
                BaseShaderGUI.DoPopup(Styles.weatherModeName, properties.weatherMode,
                    Styles.weatherModeOptions, materialEditor);
            }

            if (properties.disableHeightmapRenderer != null)
            {
                bool enableHeightMapRenderer = properties.disableHeightmapRenderer.floatValue > 0.5f;
                enableHeightMapRenderer = EditorGUILayout.Toggle("Disable Heightmap Renderer", enableHeightMapRenderer);
                properties.disableHeightmapRenderer.floatValue = enableHeightMapRenderer ? 1 : 0;
            }
            
            if (properties.customFogFragment != null)
            {
                bool customFogFragment = properties.customFogFragment.floatValue > 0.5f;
                customFogFragment = EditorGUILayout.Toggle("Custom Fog Fragment", customFogFragment);
                properties.customFogFragment.floatValue = customFogFragment ? 1 : 0;
            }

            if (properties.globalCloudShadow != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = properties.globalCloudShadow.hasMixedValue;
                var enableCloudShadow = EditorGUILayout.Toggle(Styles.globalCloudShadow, properties.globalCloudShadow.floatValue == 1.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    properties.globalCloudShadow.floatValue = enableCloudShadow ? 1.0f : 0.0f;
                }
                EditorGUI.showMixedValue = false;

                if (enableCloudShadow && properties.cloudShadowIntensity != null)
                {
                    EditorGUI.indentLevel++;
                    materialEditor.ShaderProperty(properties.cloudShadowIntensity, Styles.cloudShadowIntensity);
                    EditorGUI.indentLevel--;
                }
            }
        }

        public static void SetMateiralKeywords(EcosystemProperties litVegetationProperties, Material material)
        {
            if (litVegetationProperties.weatherMode != null)
            {
                if (litVegetationProperties.weatherMode.floatValue == 0.0f)
                {
                    CoreUtils.SetKeyword(material, "_GLOBAL_RAIN_SURFACE", false);
                    CoreUtils.SetKeyword(material, "_GLOBAL_SNOW_SURFACE", false);
                }
                else if (Math.Abs(litVegetationProperties.weatherMode.floatValue - 1.0f) < 0.01f)
                {
                    CoreUtils.SetKeyword(material, "_GLOBAL_SNOW_SURFACE", false);
                    CoreUtils.SetKeyword(material, "_GLOBAL_RAIN_SURFACE", true);
                }
                else if (Math.Abs(litVegetationProperties.weatherMode.floatValue - 2.0f) < 0.01f)
                {
                    CoreUtils.SetKeyword(material, "_GLOBAL_RAIN_SURFACE", false);
                    CoreUtils.SetKeyword(material, "_GLOBAL_SNOW_SURFACE", true);
                }
                
                if (litVegetationProperties.customFogFragment != null)
                {
                    CoreUtils.SetKeyword(material, "CUSTOM_FOG_FRAGMENT", litVegetationProperties.customFogFragment.floatValue > 0.5f);
                }
            }

            if (litVegetationProperties.disableHeightmapRenderer != null)
            {
                CoreUtils.SetKeyword(material, "_DISABLE_RENDER_HEIGHT", litVegetationProperties.disableHeightmapRenderer.floatValue > 0.5f);
            }

            if (litVegetationProperties.globalCloudShadow != null)
            {
                CoreUtils.SetKeyword(material, "_GLOBAL_CLOUD_SHADOW", litVegetationProperties.globalCloudShadow.floatValue > 0.5f);
            }
        }
    }
}