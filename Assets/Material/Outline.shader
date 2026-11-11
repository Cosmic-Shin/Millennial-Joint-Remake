// Inverted-hull outline: draws back faces pushed out along the normals, so a dark rim shows around the model
Shader "Custom/Outline"
{
    Properties
    {
        _Color ("Color", Color) = (0.05, 0.05, 0.07, 1)
        _Width ("Width (world units)", Float) = 0.02
        _ScreenWidth ("Minimum width (pixels)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Width;
                float _ScreenWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 vert(Attributes i) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(i.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(i.normalOS);
                float4 pos = TransformWorldToHClip(positionWS + normalWS * _Width);

                // Extra push in screen space so the rim stays visible from far away.
                float2 dir = mul((float3x3)UNITY_MATRIX_VP, normalWS).xy;
                float len = length(dir);
                if (len > 1e-4)
                    pos.xy += dir / len * _ScreenWidth * 2.0 / _ScreenParams.xy * pos.w;
                return pos;
            }

            half4 frag() : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}
