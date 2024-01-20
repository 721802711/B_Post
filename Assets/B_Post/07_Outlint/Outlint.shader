Shader "B_Post/Outlint"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        LOD 100

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _MainTex_TexelSize;
        float4x4 _ClipToView;
        float4 _Color;

        float _Scale;
        float _DepthThreshold;
        float _NormalThreshold;

        float _DepthNormalThreshold;
        float _DepthNormalThresholdScale;

        CBUFFER_END

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewSpaceDir : TEXCOORD1;
	            float2 texcoordStereo : TEXCOORD2;

            };

            TEXTURE2D(_MainTex);                         SAMPLER(sampler_MainTex);
			TEXTURE2D(_CameraDepthTexture);              SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_CameraNormalsTexture);            SAMPLER(sampler_CameraNormalsTexture);

        ENDHLSL


        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature _ADDDEPTH
            #pragma shader_feature _ADDNORMAL


            float _RenderViewportScaleFactor;

            float2 TransformStereoScreenSpaceTex(float2 uv, float w)
            {

                float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
                scaleOffset.xy *= _RenderViewportScaleFactor;
                return uv.xy * scaleOffset.xy + scaleOffset.zw * w;
            }

            float2 TransformTriangleVertexToUV(float2 vertex)
            {
                float2 uv = (vertex + 1.0) * 0.5;
                return uv;
            }

			float4 alphaBlend(float4 top, float4 bottom)
			{
				float3 color = (top.rgb * top.a) + (bottom.rgb * (1 - top.a));
				float alpha = top.a + bottom.a * (1 - top.a);

				return float4(color, alpha);
			}

            v2f vert (appdata v)
            {
                v2f o;
                VertexPositionInputs  PositionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = PositionInputs.positionCS;   
                
                // uv
                o.uv  = TransformTriangleVertexToUV(o.positionCS.xy);
                #if UNITY_UV_STARTS_AT_TOP
				    o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
			    #endif
	            o.texcoordStereo = TransformStereoScreenSpaceTex(o.uv, 1.0);      
                
                float4 vertex = float4(o.positionCS.xy, 0.0, -1.0);
                o.viewSpaceDir = mul(_ClipToView, vertex).xyz;

                return o;
            }


            half4 frag (v2f i) : SV_Target
            {



                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);      
                // 初始化边缘强度
                float edgeDepth = 0.0;
                float edgeNormal = 0.0;


                float halfScaleFloor = floor(_Scale * 0.5);
                float halfScaleCeil = ceil(_Scale * 0.5);

                float2 bottomLeftUV  = i.uv - float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * halfScaleFloor;
                float2 topRightUV    = i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * halfScaleCeil;  
                float2 bottomRightUV = i.uv + float2(_MainTex_TexelSize.x * halfScaleCeil, -_MainTex_TexelSize.y * halfScaleFloor);
                float2 topLeftUV     = i.uv + float2(-_MainTex_TexelSize.x * halfScaleFloor, _MainTex_TexelSize.y * halfScaleCeil);

                // 深度
                float depth0 = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, bottomLeftUV).r;
                float depth1 = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, topRightUV).r;
                float depth2 = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, bottomRightUV).r;
                float depth3 = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, topLeftUV).r;
                
                float depthFiniteDifference0 = depth1 - depth0;
                float depthFiniteDifference1 = depth3 - depth2;

                edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100;
                float DepthThreshold = _DepthThreshold * depth0;
                edgeDepth = edgeDepth > DepthThreshold ? 1 : 0;




                // 深度法线
                float3 normal0 = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, bottomLeftUV).rgb;
                float3 normal1 = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, topRightUV).rgb;
                float3 normal2 = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, bottomRightUV).rgb;
                float3 normal3 = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, topLeftUV).rgb;

                float3 normalFiniteDifference0 = normal1 - normal0;
                float3 normalFiniteDifference1 = normal3 - normal2;

                edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));



                float3 viewNormal = normal0 * 2 - 1;
                float NdotV = 1 - dot(viewNormal, -i.viewSpaceDir);

                float normalThreshold01 = saturate((NdotV - _DepthNormalThreshold) / (1 - _DepthNormalThreshold));
                float normalThreshold = (normalThreshold01 * _DepthNormalThresholdScale + 1) * _NormalThreshold;

                edgeNormal = edgeNormal > normalThreshold ? 1 : 0;


                #ifdef _ADDDEPTH

                float4 edge = float4(_Color.rgb, _Color.a * edgeDepth);
                return alphaBlend(edge, color);
                    
                #endif

                #ifdef _ADDNORMAL


                float4 edge = float4(_Color.rgb, _Color.a * edgeNormal);
                return alphaBlend(edge, color);

                #endif


                // 计算最终边缘强度

                float edgeStrength = max(edgeDepth, edgeNormal);

                float4 edgeColor = float4(_Color.rgb, _Color.a * edgeStrength);
                return alphaBlend(edgeColor, color);
            }
            ENDHLSL
        }
    }
}