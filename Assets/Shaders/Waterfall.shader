// A waterfall curtain or spray sheet: see-through white water with streaks falling down it (uv.y), a brighter rim at the bottom, fog.
// New for ShiftStorm (Tee'd Off's waterfall is built from moving meshes); cheap: one unlit transparent pass.
Shader "ShiftStorm/Waterfall"
{
    Properties
    {
        _Color ("Color", Color) = (0.85, 0.95, 1, 0.55)
        _Streak ("Streak colour", Color) = (1, 1, 1, 0.9)
        _Speed ("Fall speed", Float) = 1.6
        _Density ("Streaks across", Float) = 9
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Color, _Streak; float _Speed, _Density;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half fog : TEXCOORD1; };
            V vert (A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = v.uv; o.fog = ComputeFogFactor(o.positionCS.z); return o; }
            half4 frag (V i) : SV_Target
            {
                float x = i.uv.x * _Density, t = _Time.y * _Speed;
                float lane = floor(x); float r = frac(sin(lane * 12.9898) * 43758.5453);
                float s = frac(i.uv.y * (1.5 + r) + t * (0.8 + r * 0.6) + r);
                float streak = smoothstep(0.55, 0.9, s) * smoothstep(0.1, 0.35, frac(x));
                half4 c = lerp(_Color, _Streak, streak);
                c.a = saturate(c.a + (1 - i.uv.y) * 0.25);
                return half4(MixFog(c.rgb, i.fog), c.a);
            }
            ENDHLSL
        }
    }
}
