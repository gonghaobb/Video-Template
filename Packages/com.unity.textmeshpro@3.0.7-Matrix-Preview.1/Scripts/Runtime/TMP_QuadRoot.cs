using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

#pragma warning disable 0414 // Disabled a few warnings related to serialized variables not used in this script but used in the editor.

namespace TMPro
{
    [ExecuteAlways]
    public class TMP_QuadRoot : MonoBehaviour
    {
        TMP_Text m_owner;

        public TMP_Text Owner { get => m_owner; set => m_owner = value; }

        // OnEnable
        void OnEnable()
        {
        }

        void OnDestroy()
        {
            if (m_owner != null)
            { // TODO: destroy quad objects?
                m_owner.OnQuadRootDestroy(this);
            }
        }

        public void OnQuadItemDestroy(TMP_QuadItem item)
        {
            if (m_owner != null)
            {
                m_owner.DestroyQuadEntity(item);
            }
        }

        public static TMP_QuadRoot CreateQuadRoot(TMP_Text textComponent)
        {
            GameObject go = new GameObject("TMP QuadRoot", new System.Type[] { typeof(TMP_QuadRoot), typeof(RectTransform) });

            TMP_QuadRoot quadRoot = go.GetComponent<TMP_QuadRoot>();
            quadRoot.m_owner = textComponent; // Store owner

            go.transform.SetParent(textComponent.transform, false);
            go.layer = textComponent.gameObject.layer;

            LayoutElement layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            var transform = go.GetComponent<RectTransform>();
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            var rcTrans = transform as RectTransform;
            if (rcTrans)
            {
                // var parentTrans = transform.parent as RectTransform;
                // if (parentTrans != null)
                // {
                //     rcTrans.anchorMin = parentTrans.anchorMin;
                //     rcTrans.anchorMax = parentTrans.anchorMax;
                // }
                // ??? Does ok when same with TMP_SubMeshUI
                rcTrans.anchorMin = Vector2.zero;
                rcTrans.anchorMax = Vector2.one;
                rcTrans.sizeDelta = Vector2.zero;
                rcTrans.pivot = textComponent.rectTransform.pivot;
                rcTrans.anchoredPosition = Vector3.zero;
            }
            return quadRoot;
        }
    }
}
