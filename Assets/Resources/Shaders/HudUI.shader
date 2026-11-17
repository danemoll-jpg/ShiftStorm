// Round 15b: the HUD / menu canvas's shader (Unity's UI/Default, rewritten in URP HLSL, with one change: where a vertex goes).
// The HUD is a Screen Space - Camera canvas a few cm in front of the race camera. Unity's own UI shader places each vertex through the
// world (canvas -> world -> camera): on Coaster Coast (8 km east) and Big Top Breakdown (9 km west) a world position is only held to ~1 mm,
// which is ~4 pixels on the Deck at that distance, so the whole UI shook as the camera moved. Here the vertex goes from the canvas's own
// coordinates straight into the camera's view (_HudLocalToView: the canvas's scale and its distance ahead, set by Hud every frame), then
// through the camera's projection: no large number anywhere.
Shader "ShiftStorm/HudUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" "RenderPipeline"="UniversalPipeline" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            Name "Default"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; half4 color : COLOR; float2 texcoord : TEXCOORD0; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST; half4 _Color; half4 _TextureSampleAdd; float4x4 _HudLocalToView;
            CBUFFER_END
            v2f vert(appdata v)
            {
                v2f o;
                float4 view = mul(_HudLocalToView, float4(v.vertex.xyz, 1));
                o.vertex = mul(UNITY_MATRIX_P, view);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }
            half4 frag(v2f i) : SV_Target
            {
                half4 c = (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;
                return c;
            }
            ENDHLSL
        }
    }
}
