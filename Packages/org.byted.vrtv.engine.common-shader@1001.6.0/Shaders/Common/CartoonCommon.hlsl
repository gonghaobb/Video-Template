#ifndef MATRIX_CARTOON_COMMON_SHADER_COMMON_INCLUDED
#define MATRIX_CARTOON_COMMON_SHADER_COMMON_INCLUDED

struct StylizedData
{
    float4 shadowColorFirst;
    float4 shadowColorSecond;
    float4 shadowColorThird;
    float shadowBoundaryFirst;
    float shadowBoundarySecond;
    float shadowBoundaryThird;
    float shadowSmoothFirst;
    float shadowSmoothSecond;
    float shadowSmoothThird;
    float shadowAreaFirst;
    float shadowAreaSecond;
    float shadowAreaThird;
};

half3 ShadowSmooth(half shadowBoundary,half shadowSmooth, half shadowArea, half halfLambert, half3 originColor, half3 shadowColor)
{
    half x = halfLambert - shadowArea;
    half factor = lerp(1 - shadowBoundary, 1, smoothstep(-shadowSmooth, shadowSmooth, x));
    return lerp(shadowColor, originColor, factor);
}

half3 CartoonLighting(half3 lightColor, half3 lightDir, half3 normal, StylizedData stylizedData)
{
    half halfLambert = dot(normal, lightDir) * 0.5 + 0.5;
    half3 firstShadowLayer = ShadowSmooth(
        stylizedData.shadowBoundaryFirst, stylizedData.shadowSmoothFirst, stylizedData.shadowAreaFirst,
        halfLambert, lightColor, stylizedData.shadowColorFirst.rgb);
    half3 secondShadowLayer = ShadowSmooth(
        stylizedData.shadowBoundarySecond, stylizedData.shadowSmoothSecond, stylizedData.shadowAreaSecond,
        halfLambert, firstShadowLayer, stylizedData.shadowColorSecond.rgb);
    half3 thirdShadowLayer = ShadowSmooth(
        stylizedData.shadowBoundaryThird, stylizedData.shadowSmoothThird, stylizedData.shadowAreaThird,
        halfLambert, secondShadowLayer, stylizedData.shadowColorThird.rgb);
    return thirdShadowLayer;
}

#endif