Shader "DragonCave/ApprovedSprite"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white"{}
        _EdgeFade("Soft atlas boundary",Range(0,.3))=.12
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            float _EdgeFade;
            struct Input {float4 position:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct Output {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            Output vert(Input v){Output o;o.position=TransformObjectToHClip(v.position.xyz);o.uv=v.uv;o.color=v.color;return o;}
            half4 frag(Output i):SV_Target
            {
                half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;
                float edge=min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y));
                c.a*=smoothstep(0,max(.001,_EdgeFade),edge);
                return c;
            }
            ENDHLSL
        }
    }
}
