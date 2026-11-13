// The toon water surface (URP port of Tee'd Off's TeedOff/ToonOcean, TO Resources/Shaders/ToonOcean.shader): two flat tones that roll with
// two crossing wave trains, thin bright crest lines, a gentle swell in the vertices, all fading to one calm tone with distance. Opaque and
// cheap; drawn from both sides (seen from below in the surfacing pool and the dive gateway). New: _Flow scrolls the bands along a direction
// (the river's current, so the current reads at a glance).
Shader "ShiftStorm/ToonWater"
{
    Properties
    {
        _Deep ("Deep", Color) = (0.12, 0.42, 0.74, 1)
        _Shallow ("Light band", Color) = (0.22, 0.66, 0.86, 1)
        _Crest ("Crest", Color) = (0.62, 0.88, 0.98, 1)
        _Swell ("Swell height", Float) = 0.15
        _Scale ("Wave scale", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ToonWater"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Deep, _Shallow, _Crest; float _Swell, _Scale;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 wp : TEXCOORD0; half fog : TEXCOORD1; };
            V vert (A v)
            {
                V o; float3 wp = TransformObjectToWorld(v.positionOS.xyz); float t = _Time.y;
                float camD = distance(wp.xz, _WorldSpaceCameraPos.xz);
                wp.y += _Swell * (sin(wp.x * 0.07 + t * 1.1) + sin(wp.z * 0.05 - t * 0.8)) * 0.5 * saturate(1.0 - camD / 900.0);
                o.wp = wp; o.positionCS = TransformWorldToHClip(wp); o.fog = ComputeFogFactor(o.positionCS.z); return o;
            }
            half4 frag (V i) : SV_Target
            {
                float2 p = i.wp.xz * _Scale; float t = _Time.y;
                float2 d1 = float2(0.8, 0.6), n1 = float2(-0.6, 0.8);
                float warp = 2.2 * sin(dot(p, n1) * 0.016 + t * 0.15) + 1.1 * sin(dot(p, n1) * 0.041 - t * 0.23);
                float s1 = sin(dot(p, d1) * 0.075 + t * 1.2 + warp);
                float2 d2 = float2(-0.35, 0.94), n2 = float2(-0.94, -0.35);
                float s2 = sin(dot(p, d2) * 0.11 - t * 1.5 + 1.6 * sin(dot(p, n2) * 0.03 + t * 0.2));
                float h = s1 * 0.72 + s2 * 0.28;
                float dash = sin(dot(p, n1) * 0.06 + 2.0 * sin(dot(p, d1) * 0.021)) * 0.5 + 0.5;
                float dist = distance(i.wp.xz, _WorldSpaceCameraPos.xz);
                float fade = saturate(1.0 - dist / 520.0);
                float3 c = h > 0.38 && dash > 0.2 ? _Shallow.rgb : _Deep.rgb;
                c = lerp(lerp(_Deep.rgb, _Shallow.rgb, 0.3), c, fade);
                float crest = smoothstep(0.86, 0.9, s1) * (1.0 - smoothstep(0.95, 0.985, s1)) * step(0.45, dash);
                c = lerp(c, _Crest.rgb, crest * fade * 0.9);
                return half4(MixFog(c, i.fog), 1);
            }
            ENDHLSL
        }
    }
}
