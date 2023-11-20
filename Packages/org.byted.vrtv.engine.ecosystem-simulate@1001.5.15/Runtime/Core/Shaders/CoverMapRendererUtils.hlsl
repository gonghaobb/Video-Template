#ifndef COVERMAP_RENDERER_UTILS
#define COVERMAP_RENDERER_UTILS

struct CoverMapVolumeBuffer
{
    float4 volumeParameters;
    float4 volumeFadeParameters;
    float4 ecosystemParameters0;
    float4 ecosystemParameters1;
};

RWTexture2D<float4>                             _CoverMap;
RWStructuredBuffer<CoverMapVolumeBuffer>        _CoverMapComputeBuffer;
int                                             _CoverMapComputeBufferLength;

#define BUFFER_COUNT            _CoverMapComputeBufferLength
#define BUFFER                  _CoverMapComputeBuffer

#define DECLARE_COVERMAP_PARAMETERS(name)   \
int                                             _##name##CoverMapSize;\
int                                             _##name##CoverMapMeterPrePixel;\
float3                                          _##name##CoverMapCenterPosition;\

#define MERGE_NAME_COVERMAP_SIZE(name)              _##name##CoverMapSize
#define MERGE_NAME_COVERMAP_PRE_SIZE(name)          _##name##CoverMapMeterPrePixel
#define MERGE_NAME_COVERMAP_CENTER_POSITION(name)   _##name##CoverMapCenterPosition

#define SAMPLE_POS_WS_XZ(name, id) ((id.xy - MERGE_NAME_COVERMAP_SIZE(name) * 0.5f) *\
MERGE_NAME_COVERMAP_PRE_SIZE(name) + MERGE_NAME_COVERMAP_CENTER_POSITION(name).xz)

inline float ComputeFade(float2 posXZ , CoverMapVolumeBuffer buffer)
{
    float fade;
    if (buffer.volumeParameters.w < 0.001f)
    {
        float dist = length(buffer.volumeParameters.xy - posXZ);
        float radius = buffer.volumeParameters.z;
        fade = saturate((radius - dist) / buffer.volumeFadeParameters.x);
    }
    else
    {
        float distX = abs(buffer.volumeParameters.x - posXZ.x);
        float distZ = abs(buffer.volumeParameters.y - posXZ.y);
        float2 radius = buffer.volumeParameters.zw * 0.5f;
        float fadeX = saturate((radius.x - distX) / buffer.volumeFadeParameters.x);
        float fadeZ = saturate((radius.y - distZ) / buffer.volumeFadeParameters.y);
        fade = fadeX * fadeZ;
    }
    
    return fade * buffer.volumeFadeParameters.z;
}

inline void FillCoverMapValue(uint3 id, float4 value)
{
    _CoverMap[id.xy] = value;
}

#define DECLARE_COVERMAP(name) \
uniform TEXTURE2D(_##name##CoverMap);\

#define SAMPLE_COVERMAP(name , samplePosWS , coverMapParameters) \
    {\
        coverMapParameters = 0;\
        float2 deltaPos = worldPos.xz - MERGE_NAME_COVERMAP_CENTER_POSITION(name).xz;\
        float2 uv = floor(deltaPos) / MERGE_NAME_COVERMAP_PRE_SIZE(name) / MERGE_NAME_COVERMAP_SIZE(name) + 0.5f;\
        if (uv.x <= 1 && uv.x >= 0 && uv.y <= 1 && uv.y >= 0) \
        {\
            coverMapParameters = SAMPLE_TEXTURE2D_LOD(_##name##CoverMap , s_linear_clamp_sampler , uv , 0);\
        }\
    }

#endif 