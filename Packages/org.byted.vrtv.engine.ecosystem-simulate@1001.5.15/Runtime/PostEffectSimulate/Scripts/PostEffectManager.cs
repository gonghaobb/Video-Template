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

namespace Matrix.EcosystemSimulate
{
    [CreateAssetMenu(fileName = "PostEffect", menuName = "EcosystemSimulate/PostEffect")]
    public class PostEffectManager : SubEcosystem
    {
        [Header("通用后处理特效Shader")]
        public Shader postEffectShader;

        #region _ScreenDistortion
        [Header("是否开启全屏屏幕扭曲(可以与其它效果结合,其余效果互斥)")]
        public bool enableScreenDistortion;

        [Header("屏幕扭曲贴图")]
        public Texture2D screenDistortionTexture;

        [Header("屏幕扭曲贴图Scale")]
        public Vector2 screenDistortionTextureScale = new Vector2(1, 1);

        [Header("屏幕扭曲贴图Offset")]
        public Vector2 screenDistortionTextureOffset = new Vector2(0, 0);

        [Header("屏幕扭曲水平速度")]
        public float screenDistortionU;

        [Header("屏幕扭曲垂直速度")]
        public float screenDistortionV;

        [Header("屏幕扭曲强度")]
        [Range(0.0f, 1.0f)]
        public float screenDistortStrength;
        #endregion

        #region _KawaseBlur
        [Header("是否开启全屏Kawase模糊")]
        public bool enableKawaseBlur;

        [Header("Kawase模糊半径")]
        [Range(0.0f, 4.0f)]
        public float kawaseBlurRadius = 5.0f;

        [Header("Kawase模糊迭代次数")]
        [Range(1, 4)]
        public int kawaseBlurIteration = 1;
        #endregion
        
        [Header("Kawase模糊边缘减弱距离")]
        [Range(0, 3f)]
        public float kawaseBlurEdgeWeakDistance = 0;

        #region _GrainyBlur
        [Header("是否开启全屏粒状模糊")]
        public bool enableGrainyBlur;

        [Header("粒状模糊半径")]
        [Range(0.0f, 50.0f)]
        public float grainyRadius = 5.0f;

        [Header("粒状模糊迭代次数")]
        [Range(1, 8)]
        public int grainyBlurIteration = 1;

        [Header("粒状模糊边缘减弱距离")]
        [Range(0, 3f)]
        public float grainyBlurEdgeWeakDistance = 0;
        #endregion

        #region _GlitchImageBlock
        [Header("是否开启全屏错位图块")]
        public bool enableGlitchImageBlock;

        [Header("错位图块故障速度")]
        [Range(0, 50)]
        public float glitchImageBlockSpeed = 10;

        [Header("错位图块故障尺寸")]
        [Range(0, 50)]
        public float glitchImageBlockSize = 8;

        [Header("错位图块故障RGB分离水平尺寸")]
        [Range(0, 25)]
        public float glitchImageBlockMaxRGBSplitX = 1;

        [Header("错位图块故障RGB分离垂直尺寸")]
        [Range(0, 25)]
        public float glitchImageBlockMaxRGBSplitY = 1;
        #endregion

        #region _GlitchScreenShake
        [Header("是否开启全屏屏幕抖动")]
        public bool enableGlitchScreenShake;

        [Header("屏幕抖动故障水平强度")]
        [Range(0, 1)]
        public float glitchScreenShakeIndensityX = 0.5f;

        [Header("屏幕抖动故障垂直强度")]
        [Range(0, 1)]
        public float glitchScreenShakeIndensityY = 0f;
        #endregion
        
        #region _Dissolve
        [Header("是否开启全屏溶解")]
        public bool enableScreenDissolve = false;
        
        [Header("溶解贴图")]
        [Reload("Runtime/PostEffectSimulate/Resource/Textures/DissolveSample.tga")]
        public Texture2D dissolveTexture = null;

        [Header("溶解图Scale")]
        public Vector2 dissolveTextureScale = new Vector2(1, 1);
        
        [Header("溶解图Offset")]
        public Vector2 dissolveTextureOffset = new Vector2(0, 0);
        
        [Header("溶解范围")] [Range(0, 1)]
        public float dissolveWidth = 0;
        
        [Header("溶解进度")] [Range(0, 1)]
        public float dissolveProcess = 0;
        
        [Header("溶解边缘颜色")][ColorUsage(true, true)]
        public Color dissolveEdgeColor = Color.white;
        
        [Header("背景颜色(Editor模拟)")]
        public Color dissolveBackgroundColor = Color.black;
        
        [Header("硬边溶解")]
        public bool dissolveHardEdge = true;
        
        [Header("溶解反向")]
        public bool invertDissolve = false;

        [Header("畸变方向")][Range(-1,1)]
        public float lensDistortionStrength = -0.5f;
        
        [Header("畸变影响强度")] [Range(0,1)] 
        public float lensDistortionIntensity = 1;
        
        [Header("畸变范围")] [Range(0, 2)] 
        public float lensDistortionRange = 1;
        #endregion

        private PostEffectPass m_PostEffectPass;

#if UNITY_EDITOR
        private PostEffectPass m_ScenePostEffectPass;
#endif

        public override bool SupportInCurrentPlatform()
        {
            return true;
        }

        public override bool SupportRunInEditor()
        {
            return true;
        }

        public override void Enable()
        {
            CreatePostEffectPass();
        }

        public override void Disable()
        {
            ReleasePostEffectPass();
        }
        
        public override void Update()
        {
            base.Update();

#if UNITY_EDITOR
            CreatePostEffectPass();
#endif
        }

        private void CreatePostEffectPass()
        {
            if (postEffectShader == null)
            {
                postEffectShader = Shader.Find("PicoVideo/Effects/UberPostEffect");
            }

            if (m_PostEffectPass == null)
            {
                Camera cam = EcosystemManager.instance.mainCamera;
                if (cam == null || cam.isActiveAndEnabled == false)
                {
                    cam = Camera.main;
                }

                if (cam != null)
                {
                    m_PostEffectPass = new PostEffectPass(this, cam, true);
                }
            }

#if UNITY_EDITOR
            if (m_ScenePostEffectPass == null)
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    m_ScenePostEffectPass = new PostEffectPass(this, sceneView.camera, true);
                }
            }
#endif
        }

        private void ReleasePostEffectPass()
        {
            if (m_PostEffectPass != null && m_PostEffectPass.renderCamera != null)
            {
                m_PostEffectPass.Release();
            }
            m_PostEffectPass = null;

#if UNITY_EDITOR
            if (m_ScenePostEffectPass != null && m_ScenePostEffectPass.renderCamera != null)
            {
                m_ScenePostEffectPass.Release();
            }
             m_ScenePostEffectPass = null;
#endif
        }
    }
}