#ifndef BLUR_PASS_INCLUDED
#define BLUR_PASS_INCLUDED



#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "SSAOFunLib.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize, _AOTex_TexelSize;
float _BlurRadius;
float _BilaterFilterFactor;
CBUFFER_END


TEXTURE2D(_MainTex);     SAMPLER(sampler_MainTex);
TEXTURE2D(_AOTex);     SAMPLER(sampler_AOTex);

struct appdata
{
    float4 positionOS : POSITION;                     
    float2 texcoord : TEXCOORD0;                     
};
struct v2f
{
    float2 uv : TEXCOORD0;                            
    float4 positionCS : SV_POSITION;                  
    float2 delta : TEXCOORD1;
};


v2f vert_h (appdata v)
{
    v2f o;
    VertexPositionInputs  PositionInputs = GetVertexPositionInputs(v.positionOS);
    o.positionCS = PositionInputs.positionCS;   
    o.uv = v.texcoord;
    o.delta = _AOTex_TexelSize.xy * float2(_BlurRadius,0);   // 
    return o;
}
v2f vert_v (appdata v)
{
    v2f o;
    VertexPositionInputs  PositionInputs = GetVertexPositionInputs(v.positionOS);
    o.positionCS = PositionInputs.positionCS;   
    o.uv = v.texcoord;
    o.delta = _AOTex_TexelSize.xy * float2(0,_BlurRadius);
    return o;
}

half CompareNormal(float3 nor1,float3 nor2)
{
	return smoothstep(_BilaterFilterFactor,1.0,dot(nor1,nor2));
}

half4 frag_Blur(v2f i) : SV_Target
{
    float2 uv = i.uv;
    float2 delta = i.delta;
    float2 uv0a = i.uv - delta;
    float2 uv0b = i.uv + delta;	
    float2 uv1a = i.uv - 2.0 * delta;
    float2 uv1b = i.uv + 2.0 * delta;
    float2 uv2a = i.uv - 3.0 * delta;
    float2 uv2b = i.uv + 3.0 * delta;
    
    float3 normal = GetWorldNormal(uv);
    float3 normal0a = GetWorldNormal(uv0a);
    float3 normal0b = GetWorldNormal(uv0b);
    float3 normal1a = GetWorldNormal(uv1a);
    float3 normal1b = GetWorldNormal(uv1b);
    float3 normal2a = GetWorldNormal(uv2a);
    float3 normal2b = GetWorldNormal(uv2b);
    
    float4 col = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex,  uv);
    float4 col0a = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex, uv0a);
    float4 col0b = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex,  uv0b);
    float4 col1a = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex,  uv1a);
    float4 col1b = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex,  uv1b);
    float4 col2a = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex,  uv2a);
    float4 col2b = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex,  uv2b);
    
    float w = 0.37004405286;
    float w0a = CompareNormal(normal, normal0a) * 0.31718061674;
    float w0b = CompareNormal(normal, normal0b) * 0.31718061674;
    float w1a = CompareNormal(normal, normal1a) * 0.19823788546;
    float w1b = CompareNormal(normal, normal1b) * 0.19823788546;
    float w2a = CompareNormal(normal, normal2a) * 0.11453744493;
    float w2b = CompareNormal(normal, normal2b) * 0.11453744493;
    
    float3 result = w * col.rgb;
    result += w0a * col0a.rgb;
    result += w0b * col0b.rgb;
    result += w1a * col1a.rgb;
    result += w1b * col1b.rgb;
    result += w2a * col2a.rgb;
    result += w2b * col2b.rgb;
    
    result /= w + w0a + w0b + w1a + w1b + w2a + w2b;
    return float4(result, 1.0);
}


#endif