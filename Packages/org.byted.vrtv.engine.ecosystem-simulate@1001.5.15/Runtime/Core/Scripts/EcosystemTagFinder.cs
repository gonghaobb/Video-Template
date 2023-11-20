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
using System.Collections.Generic;

namespace Matrix.EcosystemSimulate
{
    [ExecuteAlways]
    public class EcosystemTagFinder : MonoBehaviour
    {
        private static Dictionary<string, Transform> s_TagFinderDict = new Dictionary<string, Transform>();
        private string m_TagName;

        public static GameObject Find(string tagName) 
        {
            if (s_TagFinderDict.TryGetValue(tagName, out Transform result)) 
            {
                if (result != null) 
                {
                    return result.gameObject;
                }
            }
            return null;
        }

        private void OnEnable() 
        {
            if (!string.IsNullOrEmpty(m_TagName))
            {
                Init(m_TagName);
            }
        }

        public void Init(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                return;
            }

            m_TagName = tagName;
            if (!s_TagFinderDict.ContainsKey(tagName))
            {
                s_TagFinderDict.Add(m_TagName, transform);
            }
            else
            {
                s_TagFinderDict[m_TagName] = transform;
            }
        }

        private void OnDisable() 
        {
            if (string.IsNullOrEmpty(m_TagName))
            {
                return;
            }

            if (!s_TagFinderDict.ContainsKey(m_TagName))
            {
                return;
            }

            if (s_TagFinderDict[m_TagName] != transform)
            {
                return;
            }

            s_TagFinderDict.Remove(m_TagName);
            m_TagName = "";
        }
    }
}

