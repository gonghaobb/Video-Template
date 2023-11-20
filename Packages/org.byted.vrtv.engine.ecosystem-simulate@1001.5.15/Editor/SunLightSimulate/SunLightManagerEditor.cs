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

using UnityEditor;

namespace Matrix.EcosystemSimulate
{
    [CustomEditor(typeof(SunLightManager))]
    public class SunLightManagerEditor : Editor
    {
        private static string[] s_VolumetricPros = new string[10]
        {
            "Script",
            "m_VolumetricShader",
            "m_VolumetricDownSample",
            "m_SamplerScale",
            "m_StepNum",
            "m_MaxRayLength",
            "m_VolumetricColor",
            "m_LightRange",
            "m_LightIntensity",
            "m_LightScatteringFactor",
        };
        
        public override void OnInspectorGUI()
        {
            SunLightManager skyLightManager = target as SunLightManager;
            if (skyLightManager == null)
            {
                return;
            }
            
            DrawPropertiesExcluding(serializedObject, s_VolumetricPros);
            serializedObject.ApplyModifiedProperties();
        }
    }

}
