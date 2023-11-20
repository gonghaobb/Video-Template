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

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;


namespace Matrix.EcosystemSimulate
{
    [ExecuteAlways]
    public class PostEffectScript : MonoBehaviour
    {
        public Material postEffectMat;

        private void OnEnable()
        {
            if (postEffectMat == null)
            {
                Renderer renderer = GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    postEffectMat = renderer.sharedMaterial;
                }
            }

            if (IsPosetEffectOpened())
            {
                PostEffectPass.AddPostEffectScript(this);
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (IsPosetEffectOpened())
            {
                PostEffectPass.AddPostEffectScript(this);
            }
            else
            {
                PostEffectPass.RemovePostEffectScript(this);
            }
        }
#endif

        private void OnDisable()
        {
            PostEffectPass.RemovePostEffectScript(this);
        }

        private bool IsPosetEffectOpened()
        {
            if (postEffectMat == null)
            {
                return false;
            }

            if (postEffectMat.shader.name != "PicoVideo/Effects/UberPostEffect")
            {
                return false;
            }

            return postEffectMat.IsKeywordEnabled("_SCREEN_DISTORTION") || postEffectMat.IsKeywordEnabled("_GRAINY_BLUR") || postEffectMat.IsKeywordEnabled("_KAWASE_BLUR")
                || postEffectMat.IsKeywordEnabled("_GLITCH_IMAGE_BLOCK") || postEffectMat.IsKeywordEnabled("_GLITCH_SCREEN_SHAKE") || postEffectMat.IsKeywordEnabled("_SCREEN_DISSOLVE");
        }
    }
}