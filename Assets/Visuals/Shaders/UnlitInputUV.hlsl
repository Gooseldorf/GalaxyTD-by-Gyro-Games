#ifndef UNIVERSAL_UNLIT_INPUT_INCLUDED
#define UNIVERSAL_UNLIT_INPUT_INCLUDED

//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _MainTex_UV;
    half4 _BaseColor;
    half _Cutoff;
    half _Surface;
    half _IsBlink;
    half _IsOutline;
    half _Treshold;
    int _OutlineSize;
    float4 _OutlineColor;
    float4 _BaseMap_TexelSize;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _MainTex_UV)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _Surface)
    UNITY_DOTS_INSTANCED_PROP(float , _IsBlink)
    UNITY_DOTS_INSTANCED_PROP(float , _IsOutline)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _BaseColor)
#define _Cutoff             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Cutoff)
#define _Surface            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Surface)
#define _MainTex_UV         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _MainTex_UV)
#define _IsBlink            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _IsBlink)
#define _IsOutline          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _IsOutline)
#endif

#endif
