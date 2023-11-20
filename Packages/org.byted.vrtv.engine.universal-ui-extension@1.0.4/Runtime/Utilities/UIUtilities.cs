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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Matrix.UniversalUIExtension
{
    public static class UIUtilities
    {
        /// <summary>
        /// Find a root Canvas.
        /// </summary>
        /// <returns>Finds either the most root canvas, or the first canvas that overrides sorting.</returns>
        public static Canvas FindRootSortOverrideCanvas(Transform start, bool includeInactive)
        {
            List<Canvas> canvasList = ListPool<Canvas>.Get();
            start.GetComponentsInParent(includeInactive, canvasList);
            Canvas canvas = null;

            for (int i = 0; i < canvasList.Count; ++i)
            {
                canvas = canvasList[i];

                if (canvas.overrideSorting)
                {
                    break;
                }
            }

            ListPool<Canvas>.Release(canvasList);

            return canvas != null ? canvas : null;
        }
        
        /// <summary>
        /// Find the closest softMask in parent.
        /// </summary>
        /// <returns>There is a parent softmask</returns>
        public static bool TryFindParentSoftMask(Transform start, out SoftMask mask)
        {
            mask = null;
            List<SoftMask> results = ListPool<SoftMask>.Get();
            start.GetComponentsInParent(true, results);

            for (int i = 0; i < results.Count; ++i)
            {
                mask = results[i];
                if (!mask.isDestroyed)
                {
                    break;
                }

                mask = null;
            }

            ListPool<SoftMask>.Release(results);
            
            return mask != null;
        }
        
        /// <summary>
        /// Shortcut for enable Keyword
        /// </summary>
        public static void SetKeywordEnable(this Material mat, string keyword, bool enabled)
        {
            if (enabled)
            {
                mat.EnableKeyword(keyword);
            }
            else
            {
                mat.DisableKeyword(keyword);
            }
        }
    }
}