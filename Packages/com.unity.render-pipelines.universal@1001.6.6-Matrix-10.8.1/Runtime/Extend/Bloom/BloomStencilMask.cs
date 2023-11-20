using UnityEngine;
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

namespace Matrix.Render
{
    // PicoVideo;Bloom;ZhouShaoyang;Begin
    [ExecuteAlways]
    public class BloomStencilMask : MonoBehaviour
    {
        public int StencilRef = 2;

        private Renderer[] m_Rendereres = null;

        private void OnEnable()
        {
            UpdateBloomMask();
        }

#if UNITY_EDITOR
        // Update is called once per frame
        void Update()
        {
            ForceUpdateBloomMask();
        }
#endif

        public void ForceUpdateBloomMask()
        {
            m_Rendereres = gameObject.GetComponentsInChildren<Renderer>(true);
            UpdateBloomMask();
        }

        private void UpdateBloomMask()
        {
            if (m_Rendereres == null)
            {
                m_Rendereres = gameObject.GetComponentsInChildren<Renderer>(true);
            }

            if (m_Rendereres != null)
            {
                for (int i = 0; i < m_Rendereres.Length; i++)
                {
                    if (m_Rendereres[i] == null)
                    {
                        continue;
                    }

                    Material[] mats = m_Rendereres[i].sharedMaterials;
                    for (int j = 0; j < mats.Length; j++)
                    {
                        if (mats[j] == null)
                        {
                            continue;
                        }

                        if (mats[j].HasProperty("_StencilRef"))
                        {
                            mats[j].SetFloat("_StencilRef", StencilRef);
                        }
                    }
                }
            }
        }
    }
    // PicoVideo;Bloom;ZhouShaoyang;End
}