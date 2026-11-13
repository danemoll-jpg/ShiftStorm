using UnityEngine;

namespace ShiftStorm
{
    // Placeholder engine hum (a generated looping tone, pitch with speed, a little higher in the air, lower underwater) until ElevenLabs SFX
    // arrive. It also keeps a real source playing, so a muted check's measured 0 output level means something (AudioMeter).
    [RequireComponent(typeof(ShiftVehicle))]
    public sealed class VehicleSound : MonoBehaviour
    {
        AudioSource source; ShiftVehicle v;
        public bool spatial;   // Round 3: a rival's hum is heard from where it is
        void Start()
        {
            v = GetComponent<ShiftVehicle>();
            const int rate = 44100; int n = rate / 2; var data = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)rate; data[i] = .25f * Mathf.Sin(2 * Mathf.PI * 90 * t) + .12f * Mathf.Sin(2 * Mathf.PI * 180 * t) + .06f * Mathf.Sin(2 * Mathf.PI * 270 * t); }
            var clip = AudioClip.Create("Engine hum", n, 1, rate, false); clip.SetData(data, 0);
            source = gameObject.AddComponent<AudioSource>(); source.clip = clip; source.loop = true; source.spatialBlend = spatial ? 1 : 0; source.volume = spatial ? .5f : .35f;
            if (spatial) { source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = 6; source.maxDistance = 90; source.dopplerLevel = .3f; }
            source.Play();
            // Round 4: a wall hit (ElevenLabs-made Tee'd Off impact, copied): louder for a harder hit
            hits = new[] { Resources.Load<AudioClip>("Audio/Sfx/wall_hit_1"), Resources.Load<AudioClip>("Audio/Sfx/wall_hit_2") };
            hitSource = gameObject.AddComponent<AudioSource>(); hitSource.playOnAwake = false; hitSource.spatialBlend = source.spatialBlend;
            if (spatial) { hitSource.rolloffMode = AudioRolloffMode.Linear; hitSource.minDistance = 6; hitSource.maxDistance = 90; }
            v.WallHit += (angle, keep, what) => { var c = hits[Random.Range(0, hits.Length)]; if (c) hitSource.PlayOneShot(c, Mathf.Lerp(.25f, .9f, Mathf.Clamp01((1 - keep) * 3f))); };
        }
        AudioSource hitSource; AudioClip[] hits;
        void Update()
        {
            if (!source || !v) return;
            float k = v.Mode == Mode.Fly ? 1.25f : v.Mode == Mode.Dive ? .7f : 1f;
            source.pitch = Mathf.Lerp(.7f, 1.8f, Mathf.Clamp01(v.Speed / 45f)) * k * (v.Boosting ? 1.15f : 1f);
        }
    }
}
