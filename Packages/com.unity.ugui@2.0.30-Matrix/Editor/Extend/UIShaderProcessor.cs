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
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;

class UIShaderProcessor : IPreprocessShaders
{
    public int callbackOrder
    {
        get { return 0; }
    }

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
#if UNIVERSAL_UI_EXTENSION
        if ((shader.name == UIMaterials.DEFAULT_ADJUSTMENT_UI_SHADER ||
             shader.name == UIMaterials.DEFAULT_ADJUSTMENT_UI_SHADER_NOFETCH)
            && snippet.passName == "Default")
        {
            data.Clear();
        }
#else
        if (snippet.passName == "UniversalUIExtension")
        {
            data.Clear();
        }
#endif

#if PLATFORM_ANDROID
        BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
        GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(buildTarget);

        if ((apis.Contains(GraphicsDeviceType.Vulkan)
             || apis.Contains(GraphicsDeviceType.Metal)
             || apis.Contains(GraphicsDeviceType.Direct3D11)
             || apis.Contains(GraphicsDeviceType.Direct3D12)) 
            && !apis.Contains(GraphicsDeviceType.OpenGLES3))
        {
            if (shader.name == UIMaterials.DEFAULT_ADJUSTMENT_UI_SHADER)
            {
                data.Clear();
            }
        }
#else
        if (shader.name == UIMaterials.DEFAULT_ADJUSTMENT_UI_SHADER)
        {
            data.Clear();
        }
#endif
    }
}