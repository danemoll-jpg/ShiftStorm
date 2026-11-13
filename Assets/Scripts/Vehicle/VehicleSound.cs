using UnityEngine;

namespace ShiftStorm
{
    // Placeholder engine hum (a generated looping tone, pitch with speed, a little higher in the air, lower underwater) until ElevenLabs SFX
    // arrive. It also keeps a real source playing, so a muted check's measured 0 output level means something (AudioMeter).
    [RequireComponent(typeof(ShiftVehicle))]
    public sealed class VehicleSound : MonoBehaviour
    {
        AudioSource source; ShiftVehicle v;
        void Start()
        {
            v = GetComponent<ShiftVehicle>();
            const int rate = 44100; int n = rate / 2; var data = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)rate; data[i] = .25f * Mathf.Sin(2 * Mathf.PI * 90 * t) + .12f * Mathf.Sin(2 * Mathf.PI * 180 * t) + .06f * Mathf.Sin(2 * Mathf.PI * 270 * t); }
            var clip = AudioClip.Create("Engine hum", n, 1, rate, false); clip.SetData(data, 0);
            source = gameObject.AddComponent<AudioSource>(); source.clip = clip; source.loop = true; source.spatialBlend = 0; source.volume = .35f; source.Play();
        }
        void Update()
        {
            if (!source || !v) return;
            float k = v.Mode == Mode.Fly ? 1.25f : v.Mode == Mode.Dive ? .7f : 1f;
            source.pitch = Mathf.Lerp(.7f, 1.8f, Mathf.Clamp01(v.Speed / 45f)) * k * (v.Boosting ? 1.15f : 1f);
        }
    }
}
