#ifndef SSAO_FunLib_INCLUDED
#define SSAO_FunLib_INCLUDED


#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

float4x4 _VPMatrix_invers;

TEXTURE2D(_CameraNormalsTexture);  SAMPLER(sampler_CameraNormalsTexture);

// 输入UV，使用矩阵 _VPMatrix_invers  转换成世界坐标
float4 GetWorldPos(float2 uv)
{
    float rawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv)).r;
#if defined(UNITY_REVERSED_Z)
    rawDepth = 1 - rawDepth;
#endif
    float4 ndc = float4(uv.xy * 2 - 1, rawDepth * 2 - 1, 1);
    float4 wPos = mul(_VPMatrix_invers, ndc);
    wPos /= wPos.w;
    return wPos;
}


// 深度
float GetEyeDepth(float2 uv)
{
    float rawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv)).r;
    return LinearEyeDepth(rawDepth, _ZBufferParams);
}

// 深度法线
float3 GetWorldNormal(float2 uv)
{
    float3 wNor = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv).xyz; //world normal
    return wNor;

}
// 
float3 GetRandomVecHalf(float2 p)
{
    float3 vec = float3(0, 0, 0);
    vec.x = Hash(p) * 2 - 1;
    vec.y = Hash(p * p) * 2 - 1;
    vec.z = saturate(Hash(p * p * p) + 0.2);
    return normalize(vec);
}


float Hash(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

// 随机向量
float3 GetRandomVec(float2 p)
{
    float3 vec = float3(0, 0, 0);
    vec.x = Hash(p) * 2 - 1;
    vec.y = Hash(p * p) * 2 - 1;
    vec.z = Hash(p * p * p) * 2 - 1;
    return normalize(vec);
}


#endif