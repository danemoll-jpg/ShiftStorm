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
        void Start()
        {
            v = GetComponent<ShiftVehicle>();
            const int rate = 44100; int n = rate / 2; var data = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)rate; data[i] = .25f * Mathf.Sin(2 * Mathf.PI * 90 * t) + .12f * Mathf.Sin(2 * Mathf.PI * 180 * t) + .06f * Mathf.Sin(2 * Mathf.PI * 270 * t); }
            var clip = AudioClip.Create("Engine hum", n, 1, rate, false); clip.SetData(data, 0);
            source = gameObject.AddComponent<AudioSource>(); source.clip = clip; source.loop = true; source.spatialBlend = spatial ? 1 : 0; source.volume = spatial ? .5f : .35f;
            if (spatial) { source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = 6; source.maxDistance = 90; source.dopplerLevel = .3f; }
            source.Play();
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
            hitSource = gameObject.AddComponent<AudioSource>(); hitSource.playOnAwake = false; hitSource.spatialBlend = source.spatialBlend;
            if (spatial) { hitSource.rolloffMode = AudioRolloffMode.Linear; hitSource.minDistance = 6; hitSource.maxDistance = 90; }
            v.WallHit += (angle, keep, what) => { var c = hits[Random.Range(0, hits.Length)]; if (c) hitSource.PlayOneShot(c, Mathf.Lerp(.25f, .9f, Mathf.Clamp01((1 - keep) * 3f))); };
        }
        AudioSource hitSource; AudioClip[] hits;
        public float Pitch => source ? source.pitch : 0;
        public float WindVolume => wind ? wind.volume : 0;
        void Update()
        {
            if (!source || !v) return;
            float k = v.Mode == Mode.Fly ? 1.15f : v.Mode == Mode.Dive ? .75f : 1f;
            if (Legacy) { source.pitch = Mathf.Lerp(.7f, 1.8f, Mathf.Clamp01(v.Speed / 45f)) * k * (v.Boosting ? 1.15f : 1f); return; }
            // 0 .. 1 over the form's top speed, on to ~1.25 at the boost top speed: the pitch keeps climbing all the way
            float frac = v.Speed / Mathf.Max(1f, v.TopSpeedNow);
            source.pitch = Mathf.Lerp(.7f, 2.0f, Mathf.Clamp01(frac)) * k + Mathf.Max(0, frac - 1) * 1.2f + (v.Boosting ? .12f : 0);
            if (wind)
            {
                float f = Mathf.Clamp01(frac * .9f), want = (v.Mode == Mode.Dive ? .12f : .55f) * f * f + (v.Boosting ? .08f : 0);
                wind.volume = Mathf.MoveTowards(wind.volume, want, Time.deltaTime * 1.5f);
                wind.pitch = (v.Mode == Mode.Dive ? .55f : .8f) + .5f * f;
            }
        }
    }
}
