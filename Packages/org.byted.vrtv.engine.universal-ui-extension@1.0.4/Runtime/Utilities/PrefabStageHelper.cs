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

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;

namespace Matrix.UniversalUIExtension
{
    [InitializeOnLoad]
    public static class PrefabStageHelper
    {
        static PrefabStageHelper()
        {
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabSaving += OnPrefabSaving;
            PrefabStage.prefabSaved += OnPrefabSaved;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
        }

        public enum PrefabStatus
        {
            Opened,
            Saving,
            Saved,
            Closing,
            Dirtied
        }

        private static PrefabStatus s_PrefabStatus = PrefabStatus.Saved;

        public static PrefabStatus prefabStatus
        {
            get { return s_PrefabStatus; }
        }

        public static bool isPrefabEditing
        {
            get
            {
                return s_PrefabStatus == PrefabStatus.Opened || s_PrefabStatus == PrefabStatus.Saving || s_PrefabStatus == PrefabStatus.Saved;
            }
        }

        private static void OnPrefabStageClosing(PrefabStage obj)
        {
            s_PrefabStatus = PrefabStatus.Closing;
        }

        private static void OnPrefabSaved(GameObject obj)
        {
            s_PrefabStatus = PrefabStatus.Saved;
        }

        private static void OnPrefabSaving(GameObject obj)
        {
            s_PrefabStatus = PrefabStatus.Saving;
        }

        private static void OnPrefabStageOpened(PrefabStage obj)
        {
            s_PrefabStatus = PrefabStatus.Opened;
        }
    }
}
#endif