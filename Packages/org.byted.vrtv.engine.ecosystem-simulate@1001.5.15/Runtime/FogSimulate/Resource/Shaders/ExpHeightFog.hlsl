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

#ifndef EXP_GEIGHT_FOG_INCLUDE
#define EXP_GEIGHT_FOG_INCLUDE

#include "UnityCG.cginc"

uniform float4 _DistanceFogParam; // x:fFogStart,y:fFogEnd,z:fFogPow,w:fWTF
uniform float4 HeightFogParam; // x:fFogDensity,y:fFogHeightFalloff,z:fFogHeight,w:fFogOpacity

float _fFogStartDistance; //0;    

uniform float _fDirectionalInscatteringExponent; // 2.0f;
uniform float _fDirectionalInscatteringStartDistance; // 0;
uniform float4 _DirectionalInscatteringColor;

uniform float4 _FogSurfaceParam; // x->_fFogStartDistance, y->fFogHeightFalloff, z->fFogDensity

float4 GetHeightFogColor(float3 i_WorldPos, float3 i_CamPos, float3 i_LightDir, float start_distance,
                         float height_falloff, float density)
{
    float fFogDensity = density / 1000.0f; ///100000.0f;// / 1000.0f;//0.02 / 1000.0f;     
    float fFogHeightFalloff = height_falloff / 1000.0f; // / 1000.0f;//0.01 / 1000.0f;
    float4 v4InscatteringLightDirection = float4(i_LightDir, 1);
    
    float fFogHeight = HeightFogParam.z;
    const float MinFogOpacity = HeightFogParam.w;
    const float3 ExponentialFogColor = unity_FogColor.rgb; //HeightFogParam.v3FogColor;
    float3 v3DirectionalInscatteringColor = _DirectionalInscatteringColor.rgb;
    float FLT_EPSILON = 0.001f;
    float FLT_EPSILON2 = 0.01f;

    float realFogDensity = fFogDensity * exp2(-fFogHeightFalloff * (i_CamPos.y - fFogHeight));
    
    float3 WorldPositionRelativeToCamera = i_WorldPos.xyz - i_CamPos.xyz;

    float3 CameraToReceiver = WorldPositionRelativeToCamera;
    float CameraToReceiverLengthSqr = dot(CameraToReceiver, CameraToReceiver);
    float CameraToReceiverLengthInv = rsqrt(CameraToReceiverLengthSqr);
    float CameraToReceiverLength = CameraToReceiverLengthSqr * CameraToReceiverLengthInv;
    float3 CameraToReceiverNormalized = CameraToReceiver * CameraToReceiverLengthInv;

    //float RayOriginTerms = fExponentialFogParametersX;
    float RayLength = CameraToReceiverLength;
    float RayDirectionZ = CameraToReceiver.y;

    // Calculate the line integral of the ray from the camera to the receiver position through the fog density function
    // The exponential fog density function is d = GlobalDensity * exp(-HeightFalloff * z)
    float EffectiveZ = (abs(RayDirectionZ) > FLT_EPSILON2) ? RayDirectionZ : FLT_EPSILON2;
    float Falloff = max(-127.0f, fFogHeightFalloff * EffectiveZ);
    // if it's lower than -127.0, then exp2() goes crazy in OpenGL's GLSL.
    float ExponentialHeightLineIntegralShared = realFogDensity * (1.0f - exp2(-Falloff)) / Falloff;
    float ExponentialHeightLineIntegral = ExponentialHeightLineIntegralShared * max(
        RayLength - start_distance, 0.0f);


    float3 Inscattering = ExponentialFogColor;
    float3 DirectionalInscattering = 0;

    // Setup a cosine lobe around the light direction to approximate inscattering from the directional light off of the ambient haze;
    float3 DirectionalLightInscattering = v3DirectionalInscatteringColor * pow(
        saturate(dot(CameraToReceiverNormalized, v4InscatteringLightDirection.xyz)),
        _fDirectionalInscatteringExponent);

    // Calculate the line integral of the eye ray through the haze, using a special starting distance to limit the inscattering to the distance
    float DirExponentialHeightLineIntegral = ExponentialHeightLineIntegralShared * max(
        RayLength - _fDirectionalInscatteringStartDistance, 0.0f);
    // Calculate the amount of light that made it through the fog using the transmission equation
    float DirectionalInscatteringFogFactor = saturate(exp2(-DirExponentialHeightLineIntegral));
    // Final inscattering from the light
    DirectionalInscattering = DirectionalLightInscattering * (1 - DirectionalInscatteringFogFactor);

    // Calculate the amount of light that made it through the fog using the transmission equation
    float ExpFogFactor = max(saturate(exp2(-ExponentialHeightLineIntegral)), MinFogOpacity);
    
    float4 v4FogColor = float4((Inscattering) * (1 - ExpFogFactor) + DirectionalInscattering, ExpFogFactor);
    return v4FogColor;
}


float4 GetDistanceFogColor(float3 i_WorldPos, float3 i_CamPos)
{
    float3 v3FogColor = unity_FogColor.rgb; //DistanceFogParam.v3FogColor;
    float fFogStart = _DistanceFogParam.x; //DistanceFogParam.fFogStart;
    float fFogEnd = _DistanceFogParam.y; //DistanceFogParam.fFogEnd;
    float fFogPow = _DistanceFogParam.z; //DistanceFogParam.fFogPow;

    float fDist = length(i_WorldPos.xyz - i_CamPos.xyz);
    float ratio = 0.0f;
    if (fDist < fFogStart)
    {
        ratio = 0.0f;
    }
    else if (fDist > fFogEnd)
    {
        ratio = 1.0f;
    }
    else
    {
        ratio = (fDist - fFogStart) / (fFogEnd - fFogStart);
    }
    ratio = pow(ratio, fFogPow);
    ratio = saturate(1 - ratio);

    float4 v4FogColor = float4(v3FogColor, ratio);
    return v4FogColor;
}

float3 ApplyFog_Forward(float3 SrcColor, float3 WorldPosition)
{
    float3 FinalColor = SrcColor;
    float4 HeightFogColor = GetHeightFogColor(WorldPosition, _WorldSpaceCameraPos, GetMainLight().direction,
                                                  _fFogStartDistance, HeightFogParam.y, HeightFogParam.x);
    FinalColor = lerp(HeightFogColor.rgb, FinalColor.rgb, HeightFogColor.a);
    
    return FinalColor;
}

float3 ApplyFog_Forward(float3 SrcColor, float3 WorldPosition, float emissionTerm)
{
    float3 FinalColor = SrcColor;
    float4 HeightFogColor = GetHeightFogColor(WorldPosition, _WorldSpaceCameraPos, GetMainLight().direction,
                                              _fFogStartDistance, HeightFogParam.y, HeightFogParam.x);
    FinalColor = lerp(HeightFogColor.rgb, FinalColor.rgb, HeightFogColor.a + emissionTerm);
    
    return FinalColor;
}

float3 ApplyFog_Forward_Surface(float3 src_color, float3 world_position)
{
    float3 final_color = src_color;
    float4 height_fog_color = GetHeightFogColor(world_position, _WorldSpaceCameraPos, GetMainLight().direction,
                                                _FogSurfaceParam.x, _FogSurfaceParam.y, _FogSurfaceParam.z);
    final_color = lerp(height_fog_color.rgb, final_color.rgb, height_fog_color.a);
    return final_color;
}

#endif // EXP_GEIGHT_FOG_INCLUDE
