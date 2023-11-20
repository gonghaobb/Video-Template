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
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MaskableGraphic))]
    public class UIShineEffect : UIBehaviour, IMaterialModifier
    {
        [SerializeField]
        private Texture2D m_ShineTexture = null;

        [SerializeField]
        private Vector4 m_ShineST = new Vector4(1, 1, 0, 0);

        [SerializeField]
        [ColorUsage(true, true)]
        private Color m_ShineColor = Color.white;

        [SerializeField]
        private float m_ShineRotation = 0f;

        [SerializeField]
        private Vector2 m_ShineSpeedXY = Vector2.zero;

        private Graphic m_Graphic = null;
        private Material m_LastMaterial = null;
        private Material m_EffectMaterial = null;

        private static readonly int s_ShineTex = Shader.PropertyToID("_ShineTex");
        private static readonly int s_ShineTexST = Shader.PropertyToID("_ShineTex_ST");
        private static readonly int s_ShineColor = Shader.PropertyToID("_ShineColor");
        private static readonly int s_ShineParams = Shader.PropertyToID("_ShineParams");
        private const string SHINE_KEYWORD = "UNITY_UI_SHINE";

        public Texture2D shineTexture
        {
            get { return m_ShineTexture; }
            set
            {
                if (m_ShineTexture != value && (m_ShineTexture == null || value == null))
                {
                    graphic.SetMaterialDirty();
                }
                
                m_ShineTexture = value;
                SetShaderParamsDirty();
            }
        }

        public Vector4 shineST
        {
            get { return m_ShineST; }
            set
            {
                m_ShineST = value;
                SetShaderParamsDirty();
            }
        }

        public Color shineColor
        {
            get { return m_ShineColor; }
            set
            {
                m_ShineColor = value;
                SetShaderParamsDirty();
            }
        }

        public float shineRotation
        {
            get { return m_ShineRotation; }
            set
            {
                m_ShineRotation = value;
                SetShaderParamsDirty();
            }
        }

        public Vector2 shineSpeedXY
        {
            get { return m_ShineSpeedXY; }
            set
            {
                m_ShineSpeedXY = value;
                SetShaderParamsDirty();
            }
        }

        public Graphic graphic
        {
            get
            {
                if (m_Graphic == null)
                {
                    m_Graphic = GetComponent<Graphic>();
                }
                return m_Graphic;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (graphic != null)
            {
                graphic.SetMaterialDirty();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (graphic != null)
            {
                graphic.SetMaterialDirty();
            }

            m_LastMaterial = null;
        }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetShaderParamsDirty();
        }
#endif

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            SetShaderParamsDirty();
        }

        private void SetShaderParamsDirty()
        {
            if (graphic != null)
            {
                MaterialManager.instance.RegisterForApplyParameters(graphic, ApplyShaderParams);
            }
        }
        
        private void ApplyShaderParams(Material material)
        {
            if (material != null && DefaultUIEffect.IsSupportEffect(material.shader))
            {
                material.SetTexture(s_ShineTex, m_ShineTexture);
                material.SetVector(s_ShineTexST, m_ShineST);
                material.SetColor(s_ShineColor, m_ShineColor);
                material.SetVector(s_ShineParams,
                    new Vector3(m_ShineRotation, m_ShineSpeedXY.x, m_ShineSpeedXY.y));
            }
        }

        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled || shineTexture == null)
            {
                return baseMaterial;
            }
            
            if (!UIMaterials.IsDefaultShader(baseMaterial.shader))
            {
                return baseMaterial;
            }

            if (m_LastMaterial != baseMaterial)
            {
                m_LastMaterial = baseMaterial;
                baseMaterial = DefaultUIEffect.Replace(baseMaterial);
                baseMaterial.name += " - Shine";
                baseMaterial.EnableKeyword(SHINE_KEYWORD);
                m_EffectMaterial = baseMaterial;
            }

            SetShaderParamsDirty();
            return m_EffectMaterial;
        }
    }
}