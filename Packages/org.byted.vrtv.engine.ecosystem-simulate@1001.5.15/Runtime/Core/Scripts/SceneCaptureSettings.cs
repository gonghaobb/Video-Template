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

[CreateAssetMenu(fileName = "SceneCaptureSettings", menuName = "EcosystemSimulate/SceneCaptureSettings")]
[Serializable]
public class SceneCaptureSettings : ScriptableObject
{
    [Header("贴图大小")]
    public int texSize = 256;
    [Header("捕获中心点")]
    public Vector3 centerPos = new Vector3(0, 20, 0);
    [Header("捕获相机半径")]
    public float orthographicSize = 50;
    [Header("捕获相机近裁剪面")]
    public float near = 0.3f;
    [Header("捕获相机远裁剪面")]
    public float far = 1000f;
    [Header("捕获颜色贴图")]
    public Texture2D sceneColorTexture;
    [Header("捕获相机层级")]
    public int layerMask = -1;

    public void CopyValue(SceneCaptureSettings settings)
    {
        if (settings != null)
        {
            texSize = settings.texSize;
            centerPos = settings.centerPos;
            orthographicSize = settings.orthographicSize;
            near = settings.near;
            far = settings.far;
            layerMask = settings.layerMask;
        }
    }
}
