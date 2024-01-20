#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _MainTex_ST;
float _BlurRange;                          // 模糊 


float _FocusPower;                         // 整体强度
float _DOFDistance;                        // 控制焦点
float _farBlurScale;                       // 焦点大小
float _farBlurScalePower;                  // 对比度

float _End;
float _Start;
float _Density;
float _Iteration;
float _DownSample;
CBUFFER_END     

TEXTURE2D(_MainTex);             SAMPLER(sampler_MainTex);

struct appdata
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
};



struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;

};


float DOFOffset(float2 uv)
{
    float depth = SampleSceneDepth(uv);
    float final_result_depth = saturate(_farBlurScale*(1 - depth - _DOFDistance)*(1 - depth - _DOFDistance));
    float  Offset = _FocusPower * pow(final_result_depth, _farBlurScalePower);
    
    return Offset;         
}

// 获取高斯模糊，模糊范围
float4 GetBlurRange(float2 uv, float blurrange)
{

    float4 col = float4(0, 0, 0, 0);

    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv+ float2(0.0, 0.0)) * 0.147716f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(blurrange, 0.0)) * 0.118318f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0, -blurrange)) * 0.118318f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0, blurrange)) * 0.118318f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-blurrange, 0.0)) * 0.118318f;

    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(blurrange, blurrange)) * 0.0947416f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-blurrange, -blurrange)) * 0.0947416f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(blurrange, -blurrange)) * 0.0947416f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-blurrange, blurrange)) * 0.0947416f;

    return col;

}

half4 BokehBlur(float2 uv)
{
    // 预结算 旋转
    float c = cos(2.39996323f);
    float s = sin(2.39996323f);

    half4 GoldenRot = half4(c, s, -s, c);
    half2x2 rot = half2x2(GoldenRot);
    half4 accumvlaor = 0.0;
    half4 divisor = 0.0;
    half r = 1.0;
    half2 angle = half2(0.0, _BlurRange);
    for (int j = 0; j < _Iteration; j++)
    {
        r += 1.0/r;
        angle = mul(rot, angle);
        half4 bokeh = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv + _DownSample * (r - 1.0) * angle));
        accumvlaor += bokeh * bokeh;
        divisor += bokeh;
    }
    return accumvlaor/divisor;
}


v2f vert(appdata v)
{
    v2f o;
    o.vertex = TransformObjectToHClip(v.positionOS.xyz);
    o.uv = v.texcoord;
    return o;
}


half4 DOFfrag(v2f i) : SV_Target                       // 第二个片元着色器
{
    float4 col = float4(0, 0, 0, 0);

    float depth = SampleSceneDepth(i.uv);
    float final_result_depth = saturate(_farBlurScale*(1 - depth - _DOFDistance)*(1 - depth - _DOFDistance));
    float  Offset = _FocusPower * pow(final_result_depth, _farBlurScalePower);
    float blurrange = _BlurRange / 300 * Offset;                    // 景深处理
    // 显示深度
    #ifdef _ADDDEPTH
        return Offset;

    #endif
        col = GetBlurRange(i.uv, blurrange);
        return col;

}

half4 Bokehfrag(v2f i): SV_Target
{
   
    float Depth = SAMPLE_TEXTURE2D(_CameraDepthTexture,sampler_CameraDepthTexture,i.uv).r;
    float depthValue = Linear01Depth(Depth, _ZBufferParams);
    float4 col =  SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv); 
   
    float DOFRange = saturate(-_End *  depthValue + _Density * _End) * (step(-_Density, -depthValue)) +   // 前部分
                     saturate(_Start * depthValue - _Density * _Start) * (step(_Density, depthValue));    // 后部分
    col.rgb = lerp(col.rgb, BokehBlur(i.uv).rgb, DOFRange);
    return col;
}