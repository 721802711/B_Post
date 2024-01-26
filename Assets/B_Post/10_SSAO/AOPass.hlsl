#ifndef AO_PASS_INCLUDED
#define AO_PASS_INCLUDED


#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "SSAOFunLib.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _MainTex_ST;
float4 _aoColor;
float _SampleCount;
float _Radius;
float _RangeCheck;
float _AOInt;
float4x4 _VMatrix, _PMatrix;
CBUFFER_END


struct appdata
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
};
struct v2f
{
    float2 uv : TEXCOORD0;
    float4 positionCS : SV_POSITION;
};


TEXTURE2D(_MainTex);                          SAMPLER(sampler_MainTex);
TEXTURE2D(_AOTex);                          SAMPLER(sampler_AOTex);

// ao pass

v2f vert(appdata v)
{
    v2f o;
    VertexPositionInputs PositionInputs = GetVertexPositionInputs(v.positionOS.xyz);
    o.positionCS = PositionInputs.positionCS;
    o.uv = v.texcoord;
    return o;
}

half4 frag_ao (v2f i) : SV_Target
{
    // 获取深度
    float depth = GetEyeDepth(i.uv);
    float3 worldPos = GetWorldPos(i.uv);
    float3 wNor = GetWorldNormal(i.uv);
    float3 wTan = GetRandomVec(i.uv);
    float3 wBin = cross(wNor, wTan); 
    wTan = cross(wBin, wNor);                    // 求出在顶点法线空间下 切线的显示
    float3x3 TBN_line = float3x3(wTan, wBin, wNor);
    float ao = 0;
    int sampleCount = (int)_SampleCount;
    [unroll(128)]
    for (int j = 0; j < sampleCount; j++)
    {
        // 随机向量  
        float3 offDir = GetRandomVecHalf(j * i.uv);
        float scale = j / _SampleCount;
        scale = lerp(0.01, 1, scale * scale);
        // 扩展半径
        offDir *= scale * _Radius;
        float weight = smoothstep(0,0.2, length(offDir));
        offDir = mul(offDir, TBN_line);    
        // 世界坐标转换成裁剪空间
        float4 offPosW = float4(offDir, 0) + float4(worldPos, 1);
        float4 offPosV = mul(_VMatrix, offPosW);                
        float4 offPosC = mul(_PMatrix, offPosV);                
        float2 offPosScr = offPosC.xy / offPosC.w;
        offPosScr = offPosScr * 0.5 + 0.5;          // offPosScr = offPosScr * 0.5 + 0.5将其映射到[0,1]范围内的屏幕坐标
        // 采样深度
        float sampleDepth = SampleSceneDepth(offPosScr);
        sampleDepth = LinearEyeDepth(sampleDepth,_ZBufferParams);   
        // 采样AO
        float sampleZ = offPosC.w;  
        float rangeCheck = smoothstep(0, 1.0, _Radius / abs(sampleZ - sampleDepth) * _RangeCheck * 0.1);
        float selfCheck = (sampleDepth < depth - 0.08) ?  1 : 0;       
        ao += (sampleDepth < sampleZ) ?  1 * rangeCheck * selfCheck * _AOInt * weight : 0;
    } 
    ao = 1 - saturate((ao / sampleCount));
    return ao;
}


// final pass
half4 frag_final (v2f i) : SV_Target
{
    half4 scrTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    half4 aoTex = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex, i.uv);
    //aoTex = (1 - aoTex) * _aoColor;

    half4 finalCol = lerp(aoTex * _aoColor, aoTex, scrTex.r);
    return finalCol;
}


#endif