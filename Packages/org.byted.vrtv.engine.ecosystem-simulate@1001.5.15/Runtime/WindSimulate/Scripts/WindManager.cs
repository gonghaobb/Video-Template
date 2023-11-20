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

namespace Matrix.EcosystemSimulate
{
    [CreateAssetMenu(fileName = "WindSimulate", menuName = "EcosystemSimulate/WindSimulate")]
    public partial class WindManager : SubEcosystem
    {
        [Header("全局静态风参数")]
        [SerializeField]
        [Range(0f, 10f)]
        private float m_SingleWindGlobalStrength = 1f;
        [Range(0f, 360f)]
        [SerializeField]
        private float m_SingleWindGlobalDirection = 1f;
        
        private Vector3 m_LastPosition;
        private Transform m_Target = null;
        private static readonly int s_WindSimulateGlobalWindShaderID = Shader.PropertyToID("_WindSimulateGlobalWind");

        private Action m_OnGlobalWindParamsChanged;
        private float m_OldSingleWindGlobalStrength = 0;
        private float m_OldSingleWindGlobalDirection = 0;

        public override void SetTarget(Transform target)
        {
            m_Target = target;
            if (m_Target != null)
                m_LastPosition = m_Target.position;
        }

        public override void Enable()
        {
            m_OldSingleWindGlobalStrength = 0;
            m_OldSingleWindGlobalDirection = 0;
        }

        public override void OnDrawGizmos()
        {
            if (enable)
            {
                DrawGlobalWind();
            }
        }

        public override void Update()
        {
            CheckWindChanged();
        }
        
        public override bool SupportRunInEditor()
        {
            return true;
        }

        public override bool SupportInCurrentPlatform()
        {
            return true;
        }
        private void CheckWindChanged()
        {
            if (Math.Abs(m_OldSingleWindGlobalStrength - m_SingleWindGlobalStrength) > 0.001f ||
                Math.Abs(m_OldSingleWindGlobalDirection - m_SingleWindGlobalDirection) > 0.001f)
            {
                if (onGlobalWindParamsChanged != null)
                {
                    onGlobalWindParamsChanged.Invoke();
                }

                m_OldSingleWindGlobalStrength = m_SingleWindGlobalStrength;
                m_OldSingleWindGlobalDirection = m_SingleWindGlobalDirection;
            }
        }

        public void SetupGPUData()
        {
            Shader.SetGlobalVector(s_WindSimulateGlobalWindShaderID, GetGlobalWind());
        }

        public void ClearGPUData()
        {
            Shader.SetGlobalVector("_WindSimulateGlobalWind", Vector3.zero);
        }

        public Vector3 GetGlobalWind()
        {
            Vector3 dir = Quaternion.Euler(0f, singleWindGlobalDirection, 0f) * Vector3.forward.normalized;
            dir *= singleWindGlobalStrength;
            return dir;
        }

        public void DrawGlobalWind()
        {
            var size = 2f;
            var forward = Quaternion.Euler(0f, singleWindGlobalDirection, 0f) * Vector3.forward * size;
            var right = Quaternion.Euler(0f, singleWindGlobalDirection, 0f) * Vector3.right * size * 0.5f;
            
            Gizmos.color = Color.cyan;
            var position = EcosystemManager.instance.GetTarget().position;
            Gizmos.DrawLine(position - forward, position + forward);
            Gizmos.DrawLine(position + forward, position + forward * 0.5f + right);
            Gizmos.DrawLine(position + forward, position + forward * 0.5f - right);
        }
        
        public float singleWindGlobalStrength
        {
            get => m_SingleWindGlobalStrength;
            set => m_SingleWindGlobalStrength = value;
        }

        public float singleWindGlobalDirection
        {
            get => m_SingleWindGlobalDirection;
            set => m_SingleWindGlobalDirection = value;
        }

        public Action onGlobalWindParamsChanged
        {
            get => m_OnGlobalWindParamsChanged;
            set => m_OnGlobalWindParamsChanged = value;
        }
    }
}

