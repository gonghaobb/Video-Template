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

#ifndef __RAIN_PARTICLE_POSITION_COMPUTE__
#define __RAIN_PARTICLE_POSITION_COMPUTE__

#include "Packages/com.seasun.ecosystem-simulate/Runtime/VoxelizationSimulate/Resource/Shaders/VoxelSearch.hlsl"

// surface
#define AIR _VoxelSurfaceAir
#define TERRAIN _VoxelSurfaceTerrain
#define MODEL _VoxelSurfaceModel
#define WATER _VoxelSurfaceWater

// environment
#define ENVIRONMENT_OUTDOOR _VoxelEnvironmentOutdoor
#define ENVIRONMENT_WATER _VoxelEnvironmentWater     
#define ENVIRONMENT_DEEPWATER _VoxelEnvironmentDeepWater    

#define SAMPLE_ENVIRONMENT(voxel) (voxel.x)
#define SAMPLE_SURFACE(voxel) (voxel.y)
#define SAMPLE_CAVE(voxel) (voxel.z)

#define IS_COLLISION(voxel) (SAMPLE_SURFACE(voxel) == TERRAIN || SAMPLE_SURFACE(voxel) == MODEL || SAMPLE_SURFACE(voxel) == WATER)
#define IS_IN_PARTICLE_ENVIRONMENT(voxel) (SAMPLE_ENVIRONMENT(voxel) == ENVIRONMENT_OUTDOOR && SAMPLE_CAVE(voxel) == 0)

#include "Packages/com.seasun.ecosystem-simulate/Runtime/HeightMapSimulate/Resource/Shaders/HeightMapUtils.hlsl"
#define LAYER_TERRAIN 0.0f
#define LAYER_MODEL 1.0f
#define LAYER_WATER 2.0f

#include "Packages/com.seasun.ecosystem-simulate/Runtime/Core/Shaders/EnvironmentMaskUtils.hlsl"

float4 GetParticleAliveOnInit(const float3 position)
{
#ifdef USE_VOXEL
    uint4 temp = GetVoxelForGraphic(position);
    uint4 belowVoxel = GetVoxelForGraphic(position - float3(0 , UNIT_SIZE , 0));
    float isAlive = (temp.x < 1000 && IS_IN_PARTICLE_ENVIRONMENT(temp) && SAMPLE_SURFACE(temp) == AIR) || temp.x == 1000;
    isAlive *= (belowVoxel.x < 1000 && IS_IN_PARTICLE_ENVIRONMENT(belowVoxel) && SAMPLE_SURFACE(belowVoxel) == AIR) || belowVoxel.x == 1000;
    return float4(isAlive * (1 - GetEnvironmentMask(position)), 0 , 0 , 0);
#else
    float3 heightMapData = SampleStaticHeightMapDataByWorldPos(position);
    return float4(step(heightMapData.x , position.y), 0 , 0 , 0);
#endif
}
 
float4 GetParticleAliveOnUpdate(const float3 position , const float3 velocity)
{
#ifdef USE_VOXEL    
    float3 currentPosition = VOXEL_NORMALIZE(position);
    float3 normalizeVelocity = normalize(velocity) * UNIT_SIZE;
    for (int i = 0; i <= 2; ++i)
    {
        currentPosition = currentPosition + normalizeVelocity;
        uint4 tempVoxel = GetVoxelForGraphic(currentPosition);
        if(tempVoxel.x == 1000)
        {
            return 1;
        }
        if (IS_COLLISION(tempVoxel))
        {
            return 0;
        }
    }
    return 1;
#else
    float3 heightMapData = SampleStaticHeightMapDataByWorldPos(position);
    return float4(step(heightMapData.x, position.y + velocity.y * 0.05f), 0 , 0 , 0);
#endif
}

float4 GetParticleCorrectionPosition(float3 currentPos)
{
#ifdef USE_VOXEL    
    float3 pos = currentPos;
    for (int i = 0; i <= 3; ++i)
    {
        float3 tempPos = pos;
        tempPos.y -= UNIT_SIZE * i;
        uint4 tempVoxel = GetVoxelForGraphic(tempPos);
        if (tempVoxel.x != 1000 && IS_COLLISION(tempVoxel))
        {
            bool isNotEdge = true;
            uint4 besideVoxel = 0;
            
            besideVoxel = GetVoxelForGraphic(tempPos + float3(-UNIT_SIZE , 0 , 0));
            isNotEdge = isNotEdge && besideVoxel.x != 1000u && IS_COLLISION(besideVoxel);
            besideVoxel = GetVoxelForGraphic(tempPos + float3( UNIT_SIZE , 0 , 0));
            isNotEdge = isNotEdge && besideVoxel.x != 1000u && IS_COLLISION(besideVoxel);
            besideVoxel = GetVoxelForGraphic(tempPos + float3(0 , 0 , -UNIT_SIZE));
            isNotEdge = isNotEdge && besideVoxel.x != 1000u && IS_COLLISION(besideVoxel);
            besideVoxel = GetVoxelForGraphic(tempPos + float3(0 , 0 ,  UNIT_SIZE));
            isNotEdge = isNotEdge && besideVoxel.x != 1000u && IS_COLLISION(besideVoxel);

            if (!isNotEdge)
            {
                return 0;
            }
            
            pos.y = GetFixedPosition(tempPos , tempVoxel.w);
            return float4(1 , pos.y , SAMPLE_SURFACE(tempVoxel) == WATER , 0);
        }
    }
    
    return 0;
#else
    float3 heightMapData = SampleStaticHeightMapDataByWorldPos(currentPos);
    return float4(heightMapData.y <= 0.01f , heightMapData.x + 0.2f, heightMapData.z * 2 == LAYER_WATER, 0);
#endif
}

float4 GetEdgeParticleAliveOnInit(float3 pos)
{
    return 1 - GetEnvironmentMask(pos);
}

#endif