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

#ifndef __PARTICLE_LIGHT_COMPUTE__
#define __PARTICLE_LIGHT_COMPUTE__
#define SHADOW_ULTRA_LOW

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

#define HAS_LIGHTLOOP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"

uniform float _ParticlePointLightMaxDistance;

// Returns unassociated (non-premultiplied) color with alpha (attenuation).
// The calling code must perform alpha-compositing.
// distances = {d, d^2, 1/d, d_proj}, where d_proj = dot(lightToSample, light.forward).
float4 EvaluateLight_Punctual(float3 position,
    LightData light, float3 L, float4 distances)
{
    float4 color = float4(light.color, 1.0);

    color.a *= PunctualLightAttenuation(distances, light.rangeAttenuationScale, light.rangeAttenuationBias,
                                        light.angleScale, light.angleOffset);

    // #ifndef LIGHT_EVALUATION_NO_HEIGHT_FOG
    // // Height fog attenuation.
    // // TODO: add an if()?
    // {
    //     float cosZenithAngle = L.y;
    //     float distToLight = (light.lightType == GPULIGHTTYPE_PROJECTOR_BOX) ? distances.w : distances.x;
    //     float fragmentHeight = position.y;
    //     color.a *= TransmittanceHeightFog(_HeightFogBaseExtinction, _HeightFogBaseHeight,
    //                                       _HeightFogExponents, cosZenithAngle,
    //                                       fragmentHeight, distToLight);
    // }
    // #endif

    return color;
}

float4 GetLightColor(const float3 position)
{
    float4 color = 0;
    #ifndef _ENABLE_PARTICLE_POINT_LIGHT
    return 0;
    #endif

    // This struct is define in the material. the Lightloop must not access it
    // PostEvaluateBSDF call at the end will convert Lighting to diffuse and specular lighting
    // AggregateLighting aggregateLighting;
    // ZERO_INITIALIZE(AggregateLighting, aggregateLighting); // LightLoop is in charge of initializing the struct

        uint lightCount, lightStart;

 // #ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
 //        GetCountAndStart(posInput, LIGHTCATEGORY_PUNCTUAL, lightStart, lightCount);
 // #else  LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        lightCount = _PunctualLightCount;
        lightStart = 0;
 // #endif

        bool fastPath = false;
    #if SCALARIZE_LIGHT_LOOP
        uint lightStartLane0;
        fastPath = IsFastPath(lightStart, lightStartLane0);

        if (fastPath)
        {
            lightStart = lightStartLane0;
        }
    #endif

        // Scalarized loop. All lights that are in a tile/cluster touched by any pixel in the wave are loaded (scalar load), only the one relevant to current thread/pixel are processed.
        // For clarity, the following code will follow the convention: variables starting with s_ are meant to be wave uniform (meant for scalar register),
        // v_ are variables that might have different value for each thread in the wave (meant for vector registers).
        // This will perform more loads than it is supposed to, however, the benefits should offset the downside, especially given that light data accessed should be largely coherent.
        // Note that the above is valid only if wave intriniscs are supported.
        uint v_lightListOffset = 0;
        uint v_lightIdx = lightStart;

        while (v_lightListOffset < lightCount)
        {
            // v_lightIdx = FetchIndex(lightStart, v_lightListOffset);
            v_lightIdx = lightStart + v_lightListOffset;
#if SCALARIZE_LIGHT_LOOP
            uint s_lightIdx = ScalarizeElementIndex(v_lightIdx, fastPath);
#else
            uint s_lightIdx = v_lightIdx;
#endif
            if (s_lightIdx == -1)
                break;

            LightData s_lightData = FetchLight(s_lightIdx);
            
            // If current scalar and vector light index match, we process the light. The v_lightListOffset for current thread is increased.
            // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
            // end up with a unique v_lightIdx value that is smaller than s_lightIdx hence being stuck in a loop. All the active lanes will not have this problem.
            if (s_lightIdx >= v_lightIdx)
            {
                v_lightListOffset++;

                float distance = length(s_lightData.positionRWS + _WorldSpaceCameraPos - position);

                [branch]
                if(distance < _ParticlePointLightMaxDistance)
                {
                    float3 L;
                    float4 distances; // {d, d^2, 1/d, d_proj}
                    GetPunctualLightVectors(position - _WorldSpaceCameraPos, s_lightData, L, distances);

                    // Is it worth evaluating the light?
                    if ((s_lightData.lightDimmer > 0))
                    {
                        float4 lightColor = EvaluateLight_Punctual(position, s_lightData, L, distances);
                        lightColor.rgb *= lightColor.a; // Composite
                        color += lightColor;
                    }
                }
            }
        }
    
    return color;
}


#endif