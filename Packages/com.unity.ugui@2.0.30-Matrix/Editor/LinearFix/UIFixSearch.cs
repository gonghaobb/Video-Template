using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor
{
    public class UIFixSearch
    {
        static private string[] s_uiPrefabPaths = {
            "Assets/HotUpdateResources/UIRes/uiprefab",
            "Assets/HotUpdateResources/Prefab",
            "Assets/RawResources/RawUI",
            "Assets/HotUpdateResources",
            "Assets/Model/PicoClient",
            "Assets/Resources",
            "Assets/RawResources/RawArt/Standard/Pugc",
        };

        private static string s_uiMaterialSpriteDefaultPath = "Packages/com.unity.ugui/Runtime/Resources/Materials/LinearSpritesDefault.mat";

        [MenuItem("Window/PicoVideo/UI/LinearFix/FixAllSpriteRender", false)]
        public static void ReImportAllUIPrefab()
        {
            FixUIPrefabsSpriteRender(s_uiPrefabPaths);
        }

        [MenuItem("Assets/PicoVideo/UI/LinearFix/FixFolderSpriteRender", false)]
        public static void ReImportFolderUIPrefab()
        {
            string folderPath = AssetDatabase.GetAssetPath(Selection.objects[0]);
            FixUIPrefabsSpriteRender(new string[] { folderPath });
        }

        [MenuItem("Window/PicoVideo/UI/LinearFix/SearchAllUserDefineMaterial", false)]
        public static void SearchAllUserDefineMaterial()
        {
            string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", s_uiPrefabPaths);
            HashSet<Shader> userDefineShaders = new HashSet<Shader>();

            for (int i = 0; i < prefabGUIDs.Length; ++i)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUIDs[i]);

                EditorUtility.DisplayProgressBar($"Search TransparentColor UI ({i}/{prefabGUIDs.Length})", prefabPath, (float)i / prefabGUIDs.Length);

                GameObject gameObject = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject;
                if (gameObject != null)
                {
                    Image[] images = gameObject.GetComponentsInChildren<Image>(true);
                    if (images != null)
                    {
                        for (int j = 0; j < images.Length; ++j)
                        {
                            if (images[j].material != null && images[j].material != images[j].defaultMaterial)
                            {
                                userDefineShaders.Add(images[j].material.shader);
                            }
                        }
                    }

                    RawImage[] rawImages = gameObject.GetComponentsInChildren<RawImage>(true);
                    if (rawImages != null)
                    {
                        for (int j = 0; j < rawImages.Length; ++j)
                        {
                            if (rawImages[j].material != null && rawImages[j].material != rawImages[j].defaultMaterial)
                            {
                                userDefineShaders.Add(rawImages[j].material.shader);
                            }
                        }
                    }
                }
            }

            foreach (var shader in userDefineShaders)
            {
                Debug.LogErrorFormat("User Define Shader:{0} Path:{1}", shader.name, AssetDatabase.GetAssetPath(shader));
            }

            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Window/PicoVideo/UI/LinearFix/SearchAllUIPrefabsSpriteRender", false)]
        public static void SearchAllUIPrefabsSpriteRender()
        {
            string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", s_uiPrefabPaths);

            for (int i = 0; i < prefabGUIDs.Length; ++i)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUIDs[i]);

                EditorUtility.DisplayProgressBar($"Search UI Prefab With SpriteRender ({i}/{prefabGUIDs.Length})", prefabPath, (float)i / prefabGUIDs.Length);

                GameObject gameObject = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject;
                if (gameObject != null)
                {
                    SpriteRenderer[] spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);
                    if (spriteRenderers != null && spriteRenderers.Length > 0)
                    {
                        Debug.LogErrorFormat("UIPrefab With SpriteRender Path:{0}", prefabPath);
                    }
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private static void FixUIPrefabsSpriteRender(string[] paths)
        {
            string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", paths);

            Material matSpriteDefault = AssetDatabase.LoadAssetAtPath(s_uiMaterialSpriteDefaultPath, typeof(Material)) as Material;
            Material matSpriteBuildInDefault = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            for (int i = 0; i < prefabGUIDs.Length; ++i)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUIDs[i]);

                EditorUtility.DisplayProgressBar($"ReImport Prefab ({i}/{prefabGUIDs.Length})", prefabPath, (float)i / prefabGUIDs.Length);

                GameObject prefab = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject;
                GameObject gameObject = GameObject.Instantiate(prefab);
                if (gameObject != null)
                {
                    SpriteRenderer[] spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);
                    if (spriteRenderers != null && spriteRenderers.Length > 0)
                    {
                        for (int j = 0; j < spriteRenderers.Length; j++)
                        {
                            if (spriteRenderers[j].sharedMaterial == null || spriteRenderers[j].sharedMaterial == matSpriteBuildInDefault)
                            {
                                spriteRenderers[j].sharedMaterial = matSpriteDefault;
                            }
                        }

                        PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);
                        //EditorUtility.SetDirty(gameObject);
                        //AssetDatabase.SaveAssetIfDirty(gameObject);
                    }
                }
            }

            EditorUtility.ClearProgressBar();
            //AssetDatabase.SaveAssets();
        }
    }
}