PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET 
{    
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    #if _AlphaClip
        half alpha = surfaceDescription.Alpha;
        clip(alpha - surfaceDescription.AlphaClipThreshold);
    #elif _SURFACE_TYPE_TRANSPARENT
        half alpha = surfaceDescription.Alpha;
    #else
        half alpha = 1;
    #endif

#ifdef _ALPHAPREMULTIPLY_ON
    surfaceDescription.BaseColor *= surfaceDescription.Alpha;
#endif

    //PicoVideo;Ecosystem;ZhengLingFeng;Begin
    #if defined(CUSTOM_FOG) && defined(VARYINGS_NEED_FOG)
    half3 color = surfaceDescription.BaseColor;
    color.rgb = FogFrag(color.rgb, normalize(unpacked.viewDirectionWS), unpacked.positionWS, unpacked.fogFactor.x);
    return half4(color, alpha);
    #else
    return half4(surfaceDescription.BaseColor, alpha);
    #endif
    //PicoVideo;Ecosystem;ZhengLingFeng;End
}
