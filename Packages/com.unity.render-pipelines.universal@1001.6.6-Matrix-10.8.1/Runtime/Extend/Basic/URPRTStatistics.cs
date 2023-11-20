using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.Universal
{
    [ExecuteInEditMode]
    public class URPRTStatistics : MonoBehaviour
    {
        private static string[] STATISTIC_NAME = new string[] 
        {
            "RequireDepthTexture",
            "DepthPrepass",
            "OpaqueTexture",
            "CreateColorTexture",
            "TransparentDepthTexture",
            "TransparentColorTexture",
            "TransparentColorTextureBeforePost"
        };
        private const string SWITCH_OPEN_STATISTICS = "Switch_Open_Statistics";

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return;
            }
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (symbols.Contains("URP_DEBUG_MODE"))
            {
                return;
            }
            if (!symbols.EndsWith(";"))
            {
                symbols += ";";
            }
            symbols += "URP_DEBUG_MODE";
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            float startX = 0;
            float startY = 200;
           
            bool isOpen = PlayerPrefs.GetInt(SWITCH_OPEN_STATISTICS, 0) > 0;
            if (GUI.Button(new Rect(startX, startY, 100, 50), (isOpen ? "关闭" : "打开")))
            {
                isOpen = !isOpen;
                PlayerPrefs.SetInt(SWITCH_OPEN_STATISTICS, isOpen ? 1 : 0);
            }
            #if URP_DEBUG_MODE
            if (isOpen)
            {
                List<string> flagList = null;
                int index = 0;
                for (int i = 0; i < ForwardRendererExtend.REQUIRE_FLAG_COUNT; ++i)
                {
                    //if (i == 3)
                    //{
                    //    continue;
                    //}
                    flagList = ForwardRendererExtend.GetURPRequireFlag(0x1 << i);
                    if (flagList != null && flagList.Count > 0)
                    {
                        if (GUI.Button(new Rect(startX + 100, startY + index * 50, 300, 50), STATISTIC_NAME[i] + "(" + flagList.Count.ToString() + ")"))
                        {
                            foreach(string content in flagList)
                            {
                                Debug.Log("---->" + content);
                            }
                        }
                        index++;
                    }
                }
            }
            #endif
        }
#endif
    }
}
