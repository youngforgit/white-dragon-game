Shader "DragonCave/SourceWithMatte"
{
 Properties {
  [PerRendererData] _MainTex("Approved original frame",2D)="white"{}
  [PerRendererData] _AlphaMask("Separate cutout mask",2D)="white"{}
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off
  Pass {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   TEXTURE2D(_AlphaMask);SAMPLER(sampler_AlphaMask);
   struct Input {float4 position:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct Output {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   Output vert(Input v){Output o;o.position=TransformObjectToHClip(v.position.xyz);o.uv=v.uv;o.color=v.color;return o;}
   half4 frag(Output i):SV_Target {
    half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;
    c.a*=smoothstep(.12,.62,SAMPLE_TEXTURE2D(_AlphaMask,sampler_AlphaMask,i.uv).a);
    return c;
   }
   ENDHLSL
  }
 }
}
