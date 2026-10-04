Shader "Game/Interaction Outline"
{
    Properties
    {
        [HDR] _OutlineColor ("Outline Color", Color) = (1, 0.75, 0.15, 1)
        _OutlineWidth ("Width in Pixels", Range(0, 12)) = 3
        _OutlineEmissionIntensity ("Emission Intensity", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+10" "RenderType" = "Transparent" }
        Pass
        {
            Name "InteractionOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineEmissionIntensity;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                float2 direction = mul((float3x3)UNITY_MATRIX_VP, normalWS).xy;
                direction /= max(length(direction), 0.0001);
                output.positionCS.xy += direction * (2.0 * _OutlineWidth / _ScaledScreenParams.xy) * output.positionCS.w;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 emission = _OutlineColor.rgb * max(0.0, _OutlineEmissionIntensity);
                return half4(_OutlineColor.rgb + emission, _OutlineColor.a);
            }
            ENDHLSL
        }
    }
}
