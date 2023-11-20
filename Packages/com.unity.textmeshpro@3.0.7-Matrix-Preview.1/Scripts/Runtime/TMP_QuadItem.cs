using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

#pragma warning disable 0414 // Disabled a few warnings related to serialized variables not used in this script but used in the editor.

namespace TMPro
{
    [ExecuteAlways]
    public class TMP_QuadItem : MonoBehaviour
    {
        TMP_QuadRoot m_owner;
        ITMP_QuadEntity m_entity; // the display entity

        public ITMP_QuadEntity Entity { get => m_entity; set => m_entity = value; }
        public TMP_QuadRoot Owner { get => m_owner; set => m_owner = value; }

        // OnEnable
        void OnEnable()
        {
        }

        void OnDestroy()
        {
            if(m_owner != null)
            { //
                m_owner.OnQuadItemDestroy(this);
            }
        }

        public static TMP_QuadItem CreateQuadItem(string name, TMP_QuadRoot quadRoot)
        {
            GameObject go = new GameObject(name, new System.Type[] { typeof(TMP_QuadItem), typeof(RectTransform) });

            TMP_QuadItem quadItem = go.GetComponent<TMP_QuadItem>();
            quadItem.m_owner = quadRoot; // Store owner

            go.transform.SetParent(quadRoot.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.layer = quadRoot.gameObject.layer;

            return quadItem;
        }
    }
}
