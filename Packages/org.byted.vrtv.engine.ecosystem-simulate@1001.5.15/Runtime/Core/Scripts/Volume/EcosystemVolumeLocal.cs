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
//      Procedural LOGO:https://www.shadertoy.com/view/WdyfDm 
//                  @us:https://kdocs.cn/l/sawqlPuqKX7f
//
//      The team was set up on September 4, 2019.
//

using UnityEngine;

namespace Matrix.EcosystemSimulate
{
    public partial class EcosystemVolume
    {
        public enum ColliderType
        {
            Sphere,
            Box
        }

        [SerializeField] private ColliderType m_ColliderType = ColliderType.Sphere;
        [SerializeField] private SphereCollider m_SphereCollider = null;
        [SerializeField] private float m_SphereInvFadeRadius = 0;
        [SerializeField] private BoxCollider m_BoxCollider = null;
        [SerializeField] private Vector3 m_BoxInvFadeSize = Vector3.zero;

        private float m_CurrentDistanceFade = 0;
        private float m_LastSphereRadius = 0;
        private Vector3 m_LastBoxSize = Vector3.zero;
        private Vector3 m_LastPosition = Vector3.zero;

        private void EcosystemVolumeLocalOnEnable()
        {
            if (colliderType == ColliderType.Sphere)
            {
                if (!gameObject.TryGetComponent(out m_SphereCollider))
                {
                    m_SphereCollider = gameObject.AddComponent<SphereCollider>();
                }

                m_SphereCollider.isTrigger = true;
                m_SphereCollider.enabled = true;
            }
            else
            {
                if (!gameObject.TryGetComponent(out m_BoxCollider))
                {
                    m_BoxCollider = gameObject.AddComponent<BoxCollider>();
                }

                m_BoxCollider.isTrigger = true;
                m_BoxCollider.enabled = true;
            }

            SetChanged();
        }

        private void EcosystemVolumeLocalOnDisable()
        {
            if (m_SphereCollider != null)
            {
                m_SphereCollider.enabled = false;
            }

            if (m_BoxCollider != null)
            {
                m_BoxCollider.enabled = false;
            }

            m_CurrentDistanceFade = 0;
            SetChanged();
        }

        private void EcosystemVolumeLocalUpdate()
        {
            if (m_ColliderType == ColliderType.Sphere && m_SphereCollider == null)
            {
                if (!gameObject.TryGetComponent(out m_SphereCollider))
                {
                    m_SphereCollider = gameObject.AddComponent<SphereCollider>();
                }

                m_SphereCollider.isTrigger = true;
            }

            if (m_ColliderType == ColliderType.Box && m_BoxCollider == null)
            {
                if (!gameObject.TryGetComponent(out m_BoxCollider))
                {
                    m_BoxCollider = gameObject.AddComponent<BoxCollider>();
                }

                m_BoxCollider.isTrigger = true;
            }

            if (!m_LastPosition.Equals(transform.position) ||
                m_BoxCollider != null && !m_LastBoxSize.Equals(m_BoxCollider.size) ||
                m_SphereCollider != null && !m_LastSphereRadius.Equals(m_SphereCollider.radius))
            {
                m_LastPosition = transform.position;
                m_LastBoxSize = m_BoxCollider != null ? m_BoxCollider.size : Vector3.zero;
                m_LastSphereRadius = m_SphereCollider != null ? m_SphereCollider.radius : 0;
                SetChanged();
            }
        }

        public bool TryGetParametersWithDistanceFade(EcosystemType type, out EcosystemParameters parameters,
            bool needDefaultParameters = false)
        {
            if (isGlobal)
            {
                parameters = null;
                return false;
            }
            
            if (m_CurrentDistanceFade < 0.001f)
            {
                parameters = null;
                return false;
            }

            parameters = GetOrCreateParameters(type);
            parameters.Clear();

            int idx = IndexOfParameters(type);
            if (idx >= 0)
            {
                parameters.Override(m_EcosystemParametersValueList[idx]);
                parameters.Scale(m_CurrentDistanceFade * weight);
                parameters.priority = m_Priority;
                return true;
            }
            
            if (needDefaultParameters && parameters.IsSupportDefaultParameters() && 
                EcosystemVolumeManager.current != null &&
                m_Priority > EcosystemVolumeManager.current.priority)
            {
                EcosystemParameters param = EcosystemVolumeManager.GetDefaultParameters(type);
                parameters.Override(param);
                parameters.Scale(m_CurrentDistanceFade * weight);
                parameters.priority = m_Priority;
                return true;
            }
            
            return false;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (m_VolumeType != VolumeType.Local)
            {
                return;
            }

            m_CurrentDistanceFade = 1;
        }

        private void OnTriggerExit(Collider other)
        {
            if (m_VolumeType != VolumeType.Local)
            {
                return;
            }

            m_CurrentDistanceFade = 0;
        }

        private void OnTriggerStay(Collider other)
        {
            if (m_VolumeType != VolumeType.Local)
            {
                return;
            }
            
            if (colliderType == ColliderType.Sphere)
            {
                m_CurrentDistanceFade = 0;
                if (sphereCollider != null && sphereCollider.radius > 0)
                {
                    Vector3 otherPos = transform.InverseTransformPoint(other.transform.position);
                    float dist = Vector3.Distance(sphereCollider.center, otherPos);
                    m_CurrentDistanceFade = Mathf.Clamp01((sphereCollider.radius - dist) / m_SphereInvFadeRadius);
                }
            }

            if (colliderType == ColliderType.Box)
            {
                m_CurrentDistanceFade = 0;
                if (boxCollider != null && boxCollider.size.x > 0 && boxCollider.size.y > 0 && boxCollider.size.z > 0)
                {
                    Vector3 otherPos = transform.InverseTransformPoint(other.transform.position);
                    Vector3 distXYZ = boxCollider.center - otherPos;
                    distXYZ.x = Mathf.Abs(distXYZ.x);
                    distXYZ.y = Mathf.Abs(distXYZ.y);
                    distXYZ.z = Mathf.Abs(distXYZ.z);
                    Vector3 radius = boxCollider.size * 0.5f;
                    float fadeX = Mathf.Clamp01((radius.x - distXYZ.x) / boxInvFadeSize.x);
                    float fadeY = Mathf.Clamp01((radius.y - distXYZ.y) / boxInvFadeSize.y);
                    float fadeZ = Mathf.Clamp01((radius.z - distXYZ.z) / boxInvFadeSize.z);
                    m_CurrentDistanceFade = fadeX * fadeY * fadeZ;
                }
            }
        }

        public void SetChanged(EcosystemType type)
        {
            EcosystemVolumeManager.SetLocalVolumeParametersChanged(type);
        }

        public void SetChanged()
        {
            foreach (EcosystemParameters t in m_EcosystemParametersValueList)
            {
                if (t != null)
                {
                    EcosystemVolumeManager.SetLocalVolumeParametersChanged(t.GetEcosystemType());
                }
            }
        }

        public ColliderType colliderType
        {
            get { return m_ColliderType; }
            set { m_ColliderType = value; }
        }

        public SphereCollider sphereCollider
        {
            get { return m_SphereCollider; }
            set { m_SphereCollider = value; }
        }

        public BoxCollider boxCollider
        {
            get { return m_BoxCollider; }
            set { m_BoxCollider = value; }
        }

        public float sphereInvFadeRadius
        {
            get { return m_SphereInvFadeRadius; }
            set { m_SphereInvFadeRadius = value; }
        }

        public Vector3 boxInvFadeSize
        {
            get { return m_BoxInvFadeSize; }
            set { m_BoxInvFadeSize = value; }
        }

        public float distanceFade
        {
            get
            {
                if (m_VolumeType == VolumeType.Local)
                {
                    return m_CurrentDistanceFade;
                }

                return 1;
            }
        }
    }
}