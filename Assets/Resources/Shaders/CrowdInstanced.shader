// Copied from Gridiron Glory (C:\Users\danmo\footballGame, Resources/Shaders/CrowdInstanced.shader, its Phase 10.7 crowd: read-only, copied
// and changed here; Docs/REUSE-AUDIT.md). Every seat is one camera-facing quad that shows a cell of the fan atlas (Resources/Crowd/FanAtlas.png,
// Gridiron Glory's, baked from the Y Bot and Dan's Mixamo clips): 14 poses x 6 frames. The atlas channels are how lit the shirt (r), skin (g)
// and pants / hair (b) are, so each fan gets his own colours. Per instance: _Shirt, _Params (phase, -, -, skin tone), _Params2 (personality,
// hair / cap, pants, mirror).
// ShiftStorm Round 9 item 3b changes: URP (the pass is "UniversalForward"; the light and ambient come from _CrowdLight / _CrowdAmbient, set by
// World/Crowd.cs from the sun, since URP sets no _LightColor0); one stand per material: its cheer (_CrowdBurst) ripples along the stand from
// where the racer passed at the racer's speed (_CrowdSweep), so the fans stand up and cheer as you go by; no wave round a bowl, no boos,
// towels, ponchos or empty seats (a football match's); sun hats / caps in Jurassic Par colours.
Shader "ShiftStorm/CrowdInstanced"
{
    Properties
    {
        _Atlas ("Fan atlas", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _Atlas;
            float _CrowdEnergy, _CrowdTime;
            float4 _CrowdLight, _CrowdAmbient;   // globals: the sun's colour x intensity, the ambient
            float4 _CrowdBurst;   // this stand: start time, amount, hold, -
            float4 _CrowdSweep;   // this stand: along axis (x, z), the along position where the racer was, the racer's speed (m/s)
            float _CrowdNeck[84];
            float4 _CrowdRect[14]; // per pose: u0, u1, v1 = the figure's bounds in its cell (only that part is drawn)
            float4 _CrowdGrid;    // frames, poses, box width, box height

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Shirt)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Params)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Params2)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 local : TEXCOORD1;      // x = height in the cell (0-1), y = neck height
                float3 shirt : TEXCOORD2;
                float3 skin : TEXCOORD3;
                float3 pants : TEXCOORD4;
                float3 hair : TEXCOORD5;
                float3 light : TEXCOORD6;
                UNITY_FOG_COORDS(7)
            };

            float H1(float x) { return frac(sin(x * 91.3458) * 47453.5453); }

            // A reaction that starts at `start` (+ this fan's delay), comes up in 0.2 s, holds, then fades over 2.5 s.
            float Envelope(float start, float amount, float delay, float hold)
            {
                float t = _CrowdTime - start - delay;
                if (t < 0 || amount <= 0) return 0;
                return amount * smoothstep(0, 0.2, t) * saturate(1 - (t - hold) / 2.5);
            }

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                float4 prm = UNITY_ACCESS_INSTANCED_PROP(Props, _Params);
                float4 prm2 = UNITY_ACCESS_INSTANCED_PROP(Props, _Params2);
                float4 shirt = UNITY_ACCESS_INSTANCED_PROP(Props, _Shirt);
                float3 center = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float scale = length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));
                float phase = prm.x;
                float pers = prm2.x;

                // The cheer ripples along the stand from where the racer passed, at its speed (+ up to 0.3 s each fan's own reaction time).
                float along = dot(center.xz, _CrowdSweep.xy);
                float delay = abs(along - _CrowdSweep.z) / max(4.0, _CrowdSweep.w) + 0.3 * H1(phase * 13.1);
                float burst = Envelope(_CrowdBurst.x, _CrowdBurst.y, delay, _CrowdBurst.z);
                float excite = saturate(burst + _CrowdEnergy * 0.35);
                float standAt = 0.3 + 0.45 * pers;   // rowdy fans stand up first

                // Pose: 0 sit, 1 sit clap, 2 sit cheer, 3 sit disbelief, 4 sit angry, 5 sit victory, 6 stand,
                // 7 stand clap, 8 stand cheer, 9 arms up, 10 seated fist pump, 11 jump, 12 towel, 13 foam finger.
                float pose = 0;
                float rate = 1.6;
                float pick = H1(prm2.y * 7.7 + floor(_CrowdBurst.x));
                if (pers > 0.9) { pose = 6; rate = 1.2; }                          // the ones who never sit down
                if (excite > 0.12 && pers < 0.4) { pose = 1; rate = 6; }           // clapping along
                if (excite > 0.2 && pers >= 0.4 && pers < 0.6) { pose = 2; rate = 5; }
                if (excite > standAt) { pose = pers < 0.5 ? 7 : 6; rate = 6; }
                if (excite > standAt + 0.2)
                {
                    pose = pick < 0.3 ? 9 : pick < 0.5 ? 11 : pick < 0.68 ? 8 : pick < 0.8 ? 10 : pick < 0.9 ? 12 : 13;
                    rate = 7;
                }
                float frames = _CrowdGrid.x;
                float frame = floor(frac(_CrowdTime * rate / frames + phase) * frames);

                // Idle life: everyone sways and shifts a little, on his own beat.
                float t = _CrowdTime * (0.7 + phase * 0.6) + phase * 6.283;
                float sway = sin(t) * 0.025 + sin(t * 2.3 + 1.7) * 0.012;

                // Camera-facing quad (around the vertical axis).
                float3 toCam = _WorldSpaceCameraPos - center;
                toCam.y = 0;
                toCam = length(toCam) > 0.001 ? normalize(toCam) : float3(0, 0, 1);
                float3 right = cross(toCam, float3(0, 1, 0));
                // Only the pose's figure bounds are drawn (most of a cell is empty).
                float4 rc = _CrowdRect[(int)pose];
                float cu = lerp(rc.x, rc.y, v.uv.x);
                float cv = v.uv.y * rc.z;
                float mirror = step(0.5, prm2.w);
                float px = lerp(cu, 1 - cu, mirror) - 0.5;
                float w = _CrowdGrid.z * scale, h = _CrowdGrid.w * scale;
                float3 wp = center + right * (px * w + sway * cv) + float3(0, 1, 0) * (cv * h);
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1));

                o.uv = float2((frame + cu) / frames, (pose + cv) / _CrowdGrid.y);
                int cell = (int)(pose * frames + frame);
                o.local = float2(cv, _CrowdNeck[cell]);

                // Colours: skin tones, shorts, hair; sun hats / caps (shirt colour, straw or white) for most of a Jurassic Par crowd.
                float st = prm.w;
                float3 skin = st < 0.25 ? float3(0.28, 0.18, 0.12) : st < 0.5 ? float3(0.52, 0.35, 0.24) : st < 0.75 ? float3(0.78, 0.6, 0.47) : float3(0.95, 0.8, 0.68);
                float pt = prm2.z;
                float3 pants = pt < 0.35 ? float3(0.16, 0.22, 0.38) : pt < 0.55 ? float3(0.85, 0.78, 0.6) : pt < 0.75 ? float3(0.2, 0.42, 0.3) : float3(0.38, 0.38, 0.4);
                float hp = frac(prm2.y * 3.7);
                float3 hair = hp < 0.4 ? float3(0.05, 0.04, 0.03) : hp < 0.7 ? float3(0.25, 0.15, 0.08) : hp < 0.85 ? float3(0.75, 0.62, 0.35) : float3(0.55, 0.53, 0.5);
                float cap = step(prm2.y, 0.55);
                float hc = frac(prm2.y * 17.1);
                hair = lerp(hair, hc < 0.4 ? shirt.rgb : hc < 0.75 ? float3(0.86, 0.74, 0.45) : float3(0.95, 0.95, 0.92), cap);
                o.shirt = shirt.rgb;
                o.pants = pants;
                o.skin = skin;
                o.hair = hair;
                o.light = _CrowdAmbient.rgb * 1.25 + _CrowdLight.rgb * 0.55;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 a = tex2D(_Atlas, i.uv);
                clip(a.a - 0.5);
                float hairPart = step(i.local.y, i.local.x);
                float3 blue = lerp(i.pants, i.hair, hairPart);
                float3 c = a.r * i.shirt + a.g * i.skin + a.b * blue;
                c *= i.light * 1.25;
                fixed4 col = fixed4(c, 1);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
