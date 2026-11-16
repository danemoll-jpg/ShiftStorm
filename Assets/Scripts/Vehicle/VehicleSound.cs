using UnityEngine;

namespace ShiftStorm
{
    // Placeholder engine hum (a generated looping tone, pitch with speed, a little higher in the air, lower underwater) until ElevenLabs SFX
    // arrive. It also keeps a real source playing, so a muted check's measured 0 output level means something (AudioMeter).
    // Round 5 item 1: build 5's pitch topped out at 45 m/s, so above that (Drive 65, Fly 80) speed had no sound cue. The pitch now runs
    // against the form's own top speed (and on past it on boost), and the player hears a wind layer (generated rushing noise) that
    // rises with the square of the speed's share of the top speed (muffled underwater).
    [RequireComponent(typeof(ShiftVehicle))]
    public sealed class VehicleSound : MonoBehaviour
    {
        AudioSource source, wind; ShiftVehicle v;
        public bool spatial;   // Round 3: a rival's hum is heard from where it is
        public static bool Legacy;   // build 5's pitch (checks)
        // Round 6 (split screen): both players' engines and wind are heard (one shared mix, both non-spatial), each at this share so two
        // are not twice as loud: 0.7 each (two equal sounds add up to about 1.4x in level, ~ +3 dB, not +6)
        public static float Mix = 1; float baseVol;
        // Round 7 item 5: an engine per form, from Chastain Rush's synthesis (EngineSynth): the car "six", the boat's outboard "flat6", the
        // plane "v12" with Hostile Horizon's engine_cruise under it, the sub's low rumble (kind 3) with Hostile Horizon's engine_idle;
        // cross-faded over the change; each one's pitch runs over its own form's top speed (and past it on boost).
        readonly AudioSource[] forms = new AudioSource[4], layers = new AudioSource[4]; readonly float[] formVol = { .42f, .38f, .34f, .4f }, layerVol = { 0, 0, .22f, .3f };
        void Start()
        {
            v = GetComponent<ShiftVehicle>();
            AudioSource Src(AudioClip c, float vol)
            {
                var a = gameObject.AddComponent<AudioSource>(); a.clip = c; a.loop = true; a.spatialBlend = spatial ? 1 : 0; a.volume = 0; a.playOnAwake = false;
                if (spatial) { a.rolloffMode = AudioRolloffMode.Linear; a.minDistance = 6; a.maxDistance = 90; a.dopplerLevel = .3f; }
                if (c) { a.timeSamples = Random.Range(0, Mathf.Max(1, c.samples - 1)); a.Play(); } return a;
            }
            forms[0] = Src(EngineSynth.Voiced("Car engine", 1, "six"), 0); forms[1] = Src(EngineSynth.Voiced("Boat motor", 1, "flat6"), 0);
            forms[2] = Src(EngineSynth.Voiced("Plane engine", 1, "v12"), 0); forms[3] = Src(EngineSynth.Shared("Sub rumble", 3), 0);
            if (!spatial) { layers[2] = Src(Resources.Load<AudioClip>("Audio/Sfx/hh_engine_cruise"), 0); layers[3] = Src(Resources.Load<AudioClip>("Audio/Sfx/hh_engine_idle"), 0); }
            source = forms[0]; baseVol = spatial ? .5f : .35f; const int rate = 44100;
            if (!spatial)
            {
                // wind: 2 s of low-passed noise with a slow swell, looped (the loop point is cross-faded)
                int wn = rate * 2; var w = new float[wn]; var rnd = new System.Random(7); float lp = 0, lp2 = 0;
                for (int i = 0; i < wn; i++) { float x = (float)(rnd.NextDouble() * 2 - 1); lp += (x - lp) * .08f; lp2 += (lp - lp2) * .25f; w[i] = lp2 * (1.6f + .5f * Mathf.Sin(i * 2 * Mathf.PI / wn * 3)); }
                int fade = rate / 10; for (int i = 0; i < fade; i++) { float a = i / (float)fade; w[i] = w[i] * a + w[wn - fade + i] * (1 - a); }
                var wc = AudioClip.Create("Wind", wn - fade, 1, rate, false); var trimmed = new float[wn - fade]; System.Array.Copy(w, trimmed, wn - fade); wc.SetData(trimmed, 0);
                wind = gameObject.AddComponent<AudioSource>(); wind.clip = wc; wind.loop = true; wind.spatialBlend = 0; wind.volume = 0; wind.Play();
            }
            // Round 4: a wall hit (ElevenLabs-made Tee'd Off impact, copied): louder for a harder hit
            hits = new[] { Resources.Load<AudioClip>("Audio/Sfx/wall_hit_1"), Resources.Load<AudioClip>("Audio/Sfx/wall_hit_2") };
            hitSource = gameObject.AddComponent<AudioSource>(); hitSource.playOnAwake = false; hitSource.spatialBlend = spatial ? 1 : 0;
            if (spatial) { hitSource.rolloffMode = AudioRolloffMode.Linear; hitSource.minDistance = 6; hitSource.maxDistance = 90; }
            // Round 9 item 3: a shallow touch (speed kept > 85 %) is the scrape alone (GameSounds); build 10 played the hit under it as well
            v.WallHit += (angle, keep, what) => { if (keep > .85f && !spatial) return; if (!spatial && SoundLog.Busy("impact", .3f)) { SoundLog.Merged++; return; } var c = hits[Random.Range(0, hits.Length)]; if (c) { hitSource.PlayOneShot(c, Mathf.Lerp(.25f, .9f, Mathf.Clamp01((1 - keep) * 3f)) * Settings.Effects); if (!spatial) SoundLog.Add("impact", c.name); } };
        }
        AudioSource hitSource; AudioClip[] hits;
        public float Pitch => source ? source.pitch : 0;
        public float EngineVolume => source ? source.volume : 0;
        public float WindVolume => wind ? wind.volume : 0;
        public AudioSource EngineSource => source;   // Round 8: the check's mix meter
        void Update()
        {
            if (!source || !v) return;
            int m = (int)v.Mode; source = forms[m];
            float k = v.Mode == Mode.Fly ? 1.15f : v.Mode == Mode.Dive ? .75f : 1f, pitch;
            float frac = v.Speed / Mathf.Max(1f, v.TopSpeedNow);
            if (Legacy) pitch = Mathf.Lerp(.7f, 1.8f, Mathf.Clamp01(v.Speed / 45f)) * k * (v.Boosting ? 1.15f : 1f);
            // 0 .. 1 over the form's top speed, on to ~1.25 at the boost top speed: the pitch keeps climbing all the way
            else pitch = Mathf.Lerp(.7f, 2.0f, Mathf.Clamp01(frac)) * k + Mathf.Max(0, frac - 1) * 1.2f + (v.Boosting ? .12f : 0);
            float load = Mathf.Clamp01(.55f + .45f * v.Input.Throttle + (v.Boosting ? .2f : 0));
            // Round 8 item 6: the engines (and the wind) have their own volume (Settings.Engines, default 60 %) and duck under TeedOff Barker
            // Round 14: silent behind the front menus (the title, the main menu): the race there is frozen
            var gm = Game.Instance; bool front = gm && gm.MenuOpen && gm.InFront;
            float fx = front ? 0 : (spatial ? 1 : Mix) * Settings.Engines * Caller.Duck * (spatial ? 1.3f : 1f), step = (front ? Time.unscaledDeltaTime : Time.deltaTime) / .35f;
            for (int i = 0; i < 4; i++)
            {
                float want = (i == m ? formVol[i] * load : 0) * fx;
                forms[i].volume = Mathf.MoveTowards(forms[i].volume, want, step * formVol[i]); forms[i].pitch = i == m ? pitch : forms[i].pitch;
                if (layers[i]) { layers[i].volume = Mathf.MoveTowards(layers[i].volume, (i == m ? layerVol[i] * (.5f + .5f * Mathf.Clamp01(frac)) : 0) * fx, step * .3f); layers[i].pitch = .8f + .45f * Mathf.Clamp01(frac); }
            }
            if (wind)
            {
                float f = Mathf.Clamp01(frac * .9f), want = (v.Mode == Mode.Dive ? .12f : .55f) * f * f + (v.Boosting ? .08f : 0);
                wind.volume = Mathf.MoveTowards(wind.volume, front ? 0 : want * Mix * Settings.Engines * Caller.Duck, (front ? Time.unscaledDeltaTime : Time.deltaTime) * 1.5f);
                wind.pitch = (v.Mode == Mode.Dive ? .55f : .8f) + .5f * f;
            }
        }
    }
}
