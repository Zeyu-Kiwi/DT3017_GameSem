Shader "Game/Item Sheen"
{
 Properties
 {
  [HDR] _EffectColor ("Sheen Color", Color) = (1,1,1,1)
  _Intensity ("Intensity", Float) = 1.5
  _Width ("Band Width", Float) = 0.18
  _Progress ("Progress", Float) = -1
  _ScreenBounds ("Screen Bounds", Vector) = (0,0,1,1)
  _BaseMap ("Alpha Texture", 2D) = "white" {}
  _BaseMap_STCopy ("Texture Tiling", Vector) = (1,1,0,0)
  _Cutoff ("Alpha Cutoff", Float) = 0
  _PixelSize ("Pixel Size", Float) = 2
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+5" }
  Pass
  {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Cull Back ZWrite Off ZTest LEqual Offset -1,-1
   Blend SrcAlpha One
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   half4 _EffectColor;
   float4 _ScreenBounds, _BaseMap_STCopy;
   float _Intensity, _Width, _Progress, _Cutoff, _PixelSize;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct Varyings { float4 positionCS:SV_POSITION; float4 screen:TEXCOORD0; float2 uv:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
   Varyings Vert(Attributes input)
   {
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
    output.screen=ComputeScreenPos(output.positionCS);
    output.uv=input.uv*_BaseMap_STCopy.xy+_BaseMap_STCopy.zw;
    return output;
   }
   half4 Frag(Varyings input):SV_Target
   {
    float2 pixel=input.screen.xy/input.screen.w*_ScaledScreenParams.xy;
    pixel=(floor(pixel/max(1,_PixelSize))+.5)*max(1,_PixelSize);
        // Viewport coordinates remain valid for Game view, Scene view, and scaled render targets.
    float2 viewport=pixel/_ScaledScreenParams.xy;
    float2 local=(viewport-_ScreenBounds.xy)/max(_ScreenBounds.zw,float2(.0001,.0001));
    float band=1-smoothstep(_Width*.35,max(.001,_Width),abs(local.x+local.y-_Progress));
    half alpha=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).a;
    clip(alpha-max(.001,_Cutoff));
    return half4(_EffectColor.rgb*max(0,_Intensity),band*alpha*_EffectColor.a);
   }
   ENDHLSL
  }
 }
}