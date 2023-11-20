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

namespace Matrix.UniversalUIExtension
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("UniversalUIEffect/SoftMaskNotifier", 12)]
    [RequireComponent(typeof(RectTransform))]
    public class SoftMaskNotifier : MonoBehaviour
    {
        [NonSerialized]
        private SoftMask m_ParentSoftMask = null;

        public SoftMask parentSoftMask
        {
            get { return m_ParentSoftMask; }
        }

        private void OnTransformChildrenChanged()
        {
            if (m_ParentSoftMask != null)
            {
                SoftMask.DeployModifierInChildren(m_ParentSoftMask, transform);
            }
        }

        private void Start()
        {
            RebindParentSoftMask();
        }

        private void OnCanvasHierarchyChanged()
        {
            RebindParentSoftMask();
        }

        private void OnTransformParentChanged()
        {
            RebindParentSoftMask();
        }

        private void RebindParentSoftMask()
        {
            if (transform.parent != null && UIUtilities.TryFindParentSoftMask(transform.parent, out SoftMask mask))
            {
                if (mask != m_ParentSoftMask)
                {
                    m_ParentSoftMask = mask;
                }
            }
            else
            {
                m_ParentSoftMask = null;
            }
        }
    }
}