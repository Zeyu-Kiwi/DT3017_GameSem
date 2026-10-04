Shader "Game/Item Sparkles"
{
 Properties
 {
  [HDR] _EffectColor ("Sparkle Color", Color) = (1,1,1,1)
  _Intensity ("Intensity", Float) = 2
  _PixelSize ("Pixel Size", Float) = 2
  _ConstantScreenSize ("Constant Screen Size", Float) = 0
  _WorldSize ("World Size", Float) = 0.06
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+6" }
  Pass
  {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Cull Off ZWrite Off ZTest LEqual
   Blend SrcAlpha One
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _EffectColor;
   float _Intensity,_PixelSize,_ConstantScreenSize,_WorldSize;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;float2 data:TEXCOORD1;};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float2 data:TEXCOORD1;};
   Varyings Vert(Attributes input)
   {
    Varyings output;
    output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
    output.uv=input.uv*2-1;
    output.data=input.data;
    if (_ConstantScreenSize > .5)
    {
     output.positionCS.xy+=output.uv*input.data.y/_ScaledScreenParams.xy*output.positionCS.w;
    }
    else
    {
     // Camera-facing world-space quads naturally shrink with distance.
     float diameter=_WorldSize*input.data.x;
     output.positionCS.xy+=output.uv*diameter*.5*float2(UNITY_MATRIX_P._m00,UNITY_MATRIX_P._m11);
     output.data.y=diameter*abs(UNITY_MATRIX_P._m11)*_ScaledScreenParams.y/(2*max(.0001,output.positionCS.w));
    }
    return output;
   }
   half4 Frag(Varyings input):SV_Target
   {
    float steps=max(2,input.data.y/max(1,_PixelSize));
    float2 p=abs((floor(input.uv*steps)+.5)/steps);
    float star=1-step(.22,p.x+p.y);
    star=max(star,(1-step(.14,p.x))*(1-step(.9,p.y)));
    star=max(star,(1-step(.14,p.y))*(1-step(.9,p.x)));
    clip(star-.5);
    return half4(_EffectColor.rgb*max(0,_Intensity),input.data.x*_EffectColor.a);
   }
   ENDHLSL
  }
 }
}