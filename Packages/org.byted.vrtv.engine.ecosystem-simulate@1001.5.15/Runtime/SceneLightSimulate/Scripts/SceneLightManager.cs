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

namespace Matrix.EcosystemSimulate
{
    [Serializable]
    public class DirectionalLightData
    {
        [Min(0)]
        public float intensity = 1;
        public Vector3 direction = new Vector3(50, -30, 0);
        public Color color = Color.white;
        
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            unchecked {
                hash = hash * 23 + intensity.GetHashCode();
                hash = hash * 23 + direction.GetHashCode();
                hash = hash * 23 + color.GetHashCode();
            }
            return hash;
        }
    }

    [Serializable]
    public class PointLightData
    {
        public bool enable = false;
        [Min(0)]
        public float intensity = 1;
        [Min(0)]
        public float range = 5;
        public Color color = Color.white;
        public Vector3 position = Vector3.zero;
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            unchecked {
                hash = hash * 23 + enable.GetHashCode();
                hash = hash * 23 + intensity.GetHashCode();
                hash = hash * 23 + range.GetHashCode();
                hash = hash * 23 + color.GetHashCode();
                hash = hash * 23 + position.GetHashCode();
            }
            return hash;
        }
    }
    
    [CreateAssetMenu(fileName = "SceneLightSimulate", menuName = "EcosystemSimulate/SceneLightSimulate")]
    public class SceneLightManager : SubEcosystem
    {
        private static readonly int s_AdditionLightMaxCount = 4;

        [SerializeField] private DirectionalLightData m_DirectionalLightData = new DirectionalLightData();
        [SerializeField] private List<PointLightData> m_PointLightDataList = new List<PointLightData>();
        
        private Vector4 m_DirectionCustomLightData0 = Vector4.zero;
        private Vector4 m_DirectionCustomLightData1 = Vector4.zero;
        private Vector4[] m_PointCustomLightData0 = new Vector4[4];
        private Vector4[] m_PointCustomLightData1 = new Vector4[4];

        private static readonly int s_CustomDirectionLightData0 = Shader.PropertyToID("_CustomDirectionLightData0");
        private static readonly int s_CustomDirectionLightData1 = Shader.PropertyToID("_CustomDirectionLightData1");
        private static readonly int s_PointCustomLightData0 = Shader.PropertyToID("_PointCustomLightData0");
        private static readonly int s_PointCustomLightData1 = Shader.PropertyToID("_PointCustomLightData1");
        private static readonly int s_CustomAdditionLightCounts = Shader.PropertyToID("_CustomAdditionLightCounts");

        public DirectionalLightData directionalLightData
        {
            get => m_DirectionalLightData;
            set
            {
                if (CheckIfLightDataChanged(value))
                {
                    m_DirectionalLightData = value;
                    SetCustomLightParams();
                }
            }
        }

        public List<PointLightData> pointLightDataList
        {
            get => m_PointLightDataList;
            set
            {
                if (CheckIfPointLightDataChanged(value))
                {
                    m_PointLightDataList = value;
                    SetCustomLightParams();
                }
            }
        }

        private bool CheckIfPointLightDataChanged(List<PointLightData> value)
        {
            if (m_PointLightDataList.Count != value.Count)
            {
                return true;
            }

            bool changed = false;
            for (int i = 0; i < value.Count; i++)
            {
                changed = changed || m_PointLightDataList[i].GetHashCode() != (value[i].GetHashCode());
            }

            return changed;
        }

        private bool CheckIfLightDataChanged(DirectionalLightData value)
        {
            return !m_DirectionalLightData.GetHashCode().Equals(value.GetHashCode());
        }

        public override bool SupportRunInEditor()
        {
            return true;
        }

        public override bool SupportInCurrentPlatform()
        {
            return true;
        }
        
        public override void Enable()
        {
            SetCustomLightParams();
        }

        public override void Disable()
        {
            ClearCustomLightData();
            Shader.SetGlobalVector(s_CustomDirectionLightData0, Vector4.zero);
            Shader.SetGlobalVector(s_CustomDirectionLightData1, Vector4.zero);
            Shader.SetGlobalVectorArray(s_PointCustomLightData0, m_PointCustomLightData0);
            Shader.SetGlobalVectorArray(s_PointCustomLightData1, m_PointCustomLightData1);
            Shader.SetGlobalFloat(s_CustomAdditionLightCounts, pointLightDataList.Count > 4 ? 4 : pointLightDataList.Count);
        }

        public override void Update()
        {
#if UNITY_EDITOR
            SetCustomLightParams();
#endif
        }

        public void SetCustomLightParams()
        {
            if (!enable)
            {
                return;
            }
            ClearCustomLightData();
            
            Vector3 dir = Quaternion.Euler(m_DirectionalLightData.direction) * Vector3.back;
            m_DirectionCustomLightData0 = new Vector4(m_DirectionalLightData.intensity,
                dir.x, dir.y, dir.z);
            m_DirectionCustomLightData1 = m_DirectionalLightData.color;
            
            for (int i = 0; i < pointLightDataList.Count; ++i)
            {
                if (i > 4)
                {
                    break;
                }
                var pointLightData = pointLightDataList[i];
                {
                    m_PointCustomLightData0[i] = new Vector4(pointLightData.intensity,
                        pointLightData.position.x, pointLightData.position.y,
                        pointLightData.position.z);
                    m_PointCustomLightData1[i] = new Vector4(
                        1 / Mathf.Max(pointLightData.range * pointLightData.range, 0.00001f),
                        pointLightData.color.r, pointLightData.color.g,
                        pointLightData.color.b);
                }
            }
            
            Shader.SetGlobalVector(s_CustomDirectionLightData0, m_DirectionCustomLightData0);
            Shader.SetGlobalVector(s_CustomDirectionLightData1, m_DirectionCustomLightData1);
            Shader.SetGlobalVectorArray(s_PointCustomLightData0, m_PointCustomLightData0);
            Shader.SetGlobalVectorArray(s_PointCustomLightData1, m_PointCustomLightData1);
            Shader.SetGlobalFloat(s_CustomAdditionLightCounts, pointLightDataList.Count > 4 ? 4 : pointLightDataList.Count);
        }

        private void ClearCustomLightData()
        {
            m_DirectionCustomLightData0 = Vector4.zero;
            m_DirectionCustomLightData1 = Vector4.zero;
            Array.Clear(m_PointCustomLightData0, 0, s_AdditionLightMaxCount);
            Array.Clear(m_PointCustomLightData1, 0, s_AdditionLightMaxCount);
        }

        public override void OnValidate()
        {
            SetCustomLightParams();
        }

        public override void OnDrawGizmos()
        {
            if (EcosystemManager.instance == null || EcosystemManager.instance.GetTarget() == null)
            {
                return;
            }
            Vector3 origin = EcosystemManager.instance.GetTarget().transform.position;
            Gizmos.color = m_DirectionalLightData.color;
            Vector3 dirAxis = Quaternion.Euler(m_DirectionalLightData.direction) * Vector3.forward;
            Vector3 dirNor = Quaternion.Euler(m_DirectionalLightData.direction) * Vector3.left;
            Vector3 currentP = origin + dirNor * 0.2f;
            for (int i = 0; i <= 360 / 10; i++)
            {
                Vector3 dir = Quaternion.AngleAxis(10 * i, dirAxis) * dirNor;
                Vector3 oldP = currentP;
                currentP = origin + dir * 0.2f;
                Gizmos.DrawLine(oldP, currentP);
                if (i % 6 == 0)
                {
                    Gizmos.DrawLine(oldP, oldP + dirAxis * 0.5f);
                }
            }

            for (int i = 0; i < pointLightDataList.Count; i++)
            {
                if (pointLightDataList[i].enable)
                {
                    Gizmos.DrawWireSphere(pointLightDataList[i].position, pointLightDataList[i].intensity);
                }
            }
        }
    }
}