// Round 2b: route beacons (gate frames, the flight's guide rings, cave and water guide lights, road chevrons). Unlit, vertex colour x _Color,
// a slow pulse (_Pulse 0..1), and no fog on purpose: the next gate must read from far away through the Jurassic haze and the cave's murk.
// Opaque or additive-looking (alpha < 1 blends); cheap: one pass.
Shader "ShiftStorm/Beacon"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.8, 0.3, 1)
        _Pulse ("Pulse", Range(0, 1)) = 0
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Color; float _Pulse;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float4 color : COLOR; };
            struct V { float4 positionCS : SV_POSITION; float4 color : COLOR; };
            V vert (A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.color = v.color; return o; }
            half4 frag (V i) : SV_Target
            {
                half4 c = i.color * _Color;
                c.rgb *= 1 + _Pulse * (0.35 + 0.35 * sin(_Time.y * 6.0));
                return c;
            }
            ENDHLSL
        }
    }
}
