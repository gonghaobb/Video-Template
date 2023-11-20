//
//      __  __       _______ _____  _______   __
//      |  \/  |   /\|__   __|  __ \|_   _\ \ / /
//      | \  / |  /  \  | |  | |__) | | |  \ V / 
//      | |\/| | / /\ \ | |  |  _  /  | |   > <  
//      | |  | |/ ____ \| |  | | \ \ _| |_ / . \ 
//      |_|  |_/_/    \_\_|  |_|  \_\_____/_/ \_\                        
//									   (ByteDance)
//
//      Created by Matrix team.
//      Procedural LOGO:https://www.shadertoy.com/view/ftKBRW
//
//      The team was set up on September 4, 2019.
//

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

#if UNITY_EDITOR
namespace Matrix.OverlayUI
{
    public class LayerUtility
    {
        public const int MAX_OVERLAY_CANVAS_COUNT = 8;
        
        [MenuItem("Window/PicoVideo/OverlayUI/AddSortingLayer")]
        public static void AddSortingLayer()
        {
            SerializedObject tagsAndLayersManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            tagsAndLayersManager.Update();
            SerializedProperty sortingLayersProp = tagsAndLayersManager.FindProperty("m_SortingLayers");
            for (int i = 0; i < MAX_OVERLAY_CANVAS_COUNT; i++)
            {
                string name = "OverlayCanvas" + i;
                bool existed = false;
                for (int j = 0; j < sortingLayersProp.arraySize; j++)
                {
                    SerializedProperty layer = sortingLayersProp.GetArrayElementAtIndex(j);
                    if (layer.FindPropertyRelative("name").stringValue == name)
                    {
                        existed = true;
                        break;
                    }
                }
    
                if (!existed)
                {
                    sortingLayersProp.InsertArrayElementAtIndex(sortingLayersProp.arraySize);
                    SerializedProperty newlayer = sortingLayersProp.GetArrayElementAtIndex(sortingLayersProp.arraySize - 1);
                    newlayer.FindPropertyRelative("uniqueID").intValue = 10000 + i;
                    newlayer.FindPropertyRelative("name").stringValue = name;
                    // newlayer.FindPropertyRelative("locked").boolValue = true;
                }
            }
            tagsAndLayersManager.ApplyModifiedProperties();
            OverlayCanvasManager.instance.Reset();
        }
        
        [MenuItem("Window/PicoVideo/OverlayUI/RemoveSortingLayer")]
        public static void RemoveSortingLayer()
        {
            SerializedObject tagsAndLayersManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            tagsAndLayersManager.Update();
            SerializedProperty sortingLayersProp = tagsAndLayersManager.FindProperty("m_SortingLayers");
            for (int i = 0; i < 8; i++)
            {
                string name = "OverlayCanvas" + i;
                for (int j = 0; j < sortingLayersProp.arraySize; j++)
                {
                    SerializedProperty layer = sortingLayersProp.GetArrayElementAtIndex(j);
                    if (layer.FindPropertyRelative("name").stringValue == name)
                    {
                        sortingLayersProp.DeleteArrayElementAtIndex(j);
                        break;
                    }
                }
            }
            tagsAndLayersManager.ApplyModifiedProperties();
            OverlayCanvasManager.instance.Reset();
        }
    }
}
#endif