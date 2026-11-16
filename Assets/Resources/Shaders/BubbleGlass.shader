// ShiftStorm Round 14 item 3: the Scrambler sub's glass sphere (a whole ball round the bike and its rider, Dan: "a bubble that enveloped
// the whole bike"). Written for ShiftStorm (URP, CG includes like CrowdInstanced.shader). Two passes:
//   back faces (SRPDefaultUnlit, drawn first): the far side of the ball, faint, so from inside (first person) the glass is still there
//   front faces (UniversalForward): a little refraction (the scene behind, from the camera's opaque copy, shifted by the surface's slope;
//     only while BubbleSphere turns the copy on for a camera near a bubble: _BubbleRefractOn), a fresnel rim, the sky's reflection, the
//     sun's highlight and a soft window-shaped highlight up and to the left, so it reads as glass from the chase camera.
Shader "ShiftStorm/BubbleGlass"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.78, 0.92, 1, 1)
        _Base ("Face alpha", Float) = 0.09
        _Rim ("Rim alpha", Float) = 0.7
        _Refract ("Refraction (screen share)", Float) = 0.05
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    float4 _Tint; float _Base, _Rim, _Refract, _BubbleRefractOn;
    float4 _MainLightPosition; float4 _MainLightColor;
    sampler2D _CameraOpaqueTexture;
    struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
    struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 wpos : TEXCOORD1; float4 screen : TEXCOORD2; float3 vn : TEXCOORD3; };
    v2f vert(appdata v)
    {
        v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.n = UnityObjectToWorldNormal(v.normal); o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
        o.screen = ComputeScreenPos(o.pos); o.vn = mul((float3x3)UNITY_MATRIX_V, o.n); return o;
    }
    float3 Sky(float3 dir)
    {
        half4 s = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, dir, 2);
        return DecodeHDR(s, unity_SpecCube0_HDR);
    }
    ENDCG
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+20" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        // the far side (and the whole ball from inside it): faint
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front ZWrite Off Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            float4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(-i.n), view = normalize(_WorldSpaceCameraPos - i.wpos);
                float rim = pow(1 - saturate(abs(dot(n, view))), 3);
                float a = saturate(_Base * .6 + rim * _Rim * .45);
                float3 c = _Tint.rgb * (.55 + .45 * rim);
                return float4(c * a, a);
            }
            ENDCG
        }
        // the near side: refraction, rim, reflection, highlights
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Cull Back ZWrite Off Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            float4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.n), view = normalize(_WorldSpaceCameraPos - i.wpos);
                float ndv = saturate(dot(n, view)), edge = 1 - ndv;
                float rim = pow(edge, 2.2);
                float3 refl = Sky(reflect(-view, n));
                float3 l = normalize(_MainLightPosition.xyz + float3(0, .0001, 0)); float3 h = normalize(l + view);
                float sun = pow(saturate(dot(n, h)), 180) * 1.6;
                float3 vn = normalize(i.vn); float win = smoothstep(.82, .96, dot(vn, normalize(float3(-.45, .55, .7)))) * .7 + smoothstep(.9, .99, dot(vn, normalize(float3(.5, -.35, .79)))) * .2;   // the window highlight
                float3 sunCol = _MainLightColor.rgb; if (dot(sunCol, sunCol) < .001) sunCol = float3(1, 1, 1);
                float3 glow = refl * (.12 + .5 * rim) + sun * sunCol + win;
                float a = saturate(_Base + rim * _Rim);
                if (_BubbleRefractOn > .5)
                {
                    // the scene behind, shifted by the surface's slope (more at the edge): the bike and rider seen through the glass
                    float2 uv = i.screen.xy / i.screen.w + vn.xy * _Refract * (.25 + edge);
                    float3 behind = tex2D(_CameraOpaqueTexture, uv).rgb * lerp(float3(1, 1, 1), _Tint.rgb, .35);
                    // the glass over it: its tint and rim (as without refraction), the reflection and highlights on top
                    float k = .9;   // the opaque scene comes through the glass (90 %); what was drawn after it (water, particles) through the rest
                    float3 c = lerp(behind, _Tint.rgb * .9 + refl * .25, a) + glow;
                    return float4(c * k, k);
                }
                return float4(_Tint.rgb * a + glow * (.6 + a), saturate(a + sun * .5 + win * .4));
            }
            ENDCG
        }
    }
}
