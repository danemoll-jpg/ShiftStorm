// Bright cartoon sky (skybox): a smooth gradient from the horizon to the top, and below the horizon (URP port of Tee'd Off's TeedOff/Sky,
// colours from the course's art style).
Shader "ShiftStorm/Sky"
{
    Properties
    {
        _Top ("Top", Color) = (0.22, 0.52, 0.95, 1)
        _Horizon ("Horizon", Color) = (0.68, 0.86, 1, 1)
        _Below ("Below horizon", Color) = (0.55, 0.75, 0.9, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Top, _Horizon, _Below;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert (A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.dir = v.positionOS.xyz; return o; }
            half4 frag (V i) : SV_Target
            {
                float y = normalize(i.dir).y;
                float3 c = y > 0 ? lerp(_Horizon.rgb, _Top.rgb, pow(saturate(y * 1.4), 0.7)) : lerp(_Horizon.rgb, _Below.rgb, saturate(-y * 5));
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
