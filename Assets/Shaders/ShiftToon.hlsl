// ShiftStorm's URP port of Tee'd Off's toon look (TO Resources/Shaders/ToonCommon.cginc, Toon.shader, ToonOutline.shader: Built-in pipeline,
// read-only reference). Same model: flat 3-band light from the sun (thresholds and brightness are shader globals set from the course's art
// style, JurassicLook), colour = albedo x (band x sun x 0.85 + 0.25), the sun's shadow drops a pixel to the shadow band, fog on top, and an
// inverted-hull outline whose pixel width thins with distance. Rewritten in HLSL for URP (main light + its shadow map, URP fog).
#ifndef SHIFTSTORM_TOON
#define SHIFTSTORM_TOON

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
float4 _BaseMap_ST;
float _VertexColor;
float4 _Emission;
float _OutlineScale;
CBUFFER_END
TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

half _ToonLit, _ToonMidT, _ToonMid, _ToonShadow;
half4 _ToonOutlineColor;
half _ToonOutlineWidth;

struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; float2 uv : TEXCOORD0; };
struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; half4 color : COLOR; float2 uv : TEXCOORD2; half fog : TEXCOORD3; };

half ToonBand(half ndl, half atten)
{
    half lit = saturate(ndl) * atten;
    half litT = _ToonLit > 0 ? _ToonLit : 0.55;
    half midT = _ToonMidT > 0 ? _ToonMidT : 0.12;
    half mid = _ToonMid > 0 ? _ToonMid : 0.84;
    half shade = _ToonShadow > 0 ? _ToonShadow : 0.64;
    return lit > litT ? 1.0 : (lit > midT ? mid : shade);
}

Varyings ToonVert(Attributes v)
{
    Varyings o;
    VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
    o.positionCS = p.positionCS; o.positionWS = p.positionWS;
    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
    o.color = v.color; o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
    o.fog = ComputeFogFactor(p.positionCS.z);
    return o;
}

half4 ToonFrag(Varyings i) : SV_Target
{
    half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
    half3 albedo = tex.rgb * _BaseColor.rgb * lerp(half3(1, 1, 1), i.color.rgb, _VertexColor);
    Light L = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    half band = ToonBand(dot(normalize(i.normalWS), L.direction), L.shadowAttenuation);
    half3 c = albedo * (band * L.color * 0.85 + 0.25) + _Emission.rgb;
    return half4(MixFog(c, i.fog), 1);
}

// inverted hull: push the vertex out along its normal in screen space by a pixel width that thins with distance (TO OutlinePos)
Varyings OutlineVert(Attributes v)
{
    Varyings o = (Varyings)0;
    float4 pos = TransformObjectToHClip(v.positionOS.xyz);
    float3 nW = TransformObjectToWorldNormal(v.normalOS);
    float2 nC = mul((float3x3)UNITY_MATRIX_VP, nW).xy;
    float len = length(nC);
    float2 dir = len > 1e-5 ? nC / len : float2(0, 0);
    float px = _ToonOutlineWidth * _OutlineScale * (_ScreenParams.y / 720.0) * saturate(16.0 / max(pos.w, 0.01) + 0.3);
    pos.xy += dir * px * 2.0 / _ScreenParams.xy * pos.w;
    o.positionCS = _ToonOutlineWidth * _OutlineScale > 0 ? pos : float4(0, 0, -10, 1);
    o.fog = ComputeFogFactor(pos.z);
    return o;
}
half4 OutlineFrag(Varyings i) : SV_Target { return half4(MixFog(_ToonOutlineColor.rgb, i.fog), 1); }

// shadow caster and depth (URP needs both for shadows and any depth prepass)
float3 _LightDirection;
Varyings ShadowVert(Attributes v)
{
    Varyings o = (Varyings)0;
    float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
    o.positionCS = positionCS;
    return o;
}
Varyings DepthVert(Attributes v) { Varyings o = (Varyings)0; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.normalWS = TransformObjectToWorldNormal(v.normalOS); return o; }
half4 NullFrag(Varyings i) : SV_Target { return 0; }
half4 DepthNormalsFrag(Varyings i) : SV_Target { return half4(NormalizeNormalPerPixel(i.normalWS), 0); }

#endif
