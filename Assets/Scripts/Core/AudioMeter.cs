using UnityEngine;

namespace ShiftStorm
{
    // Measures what the game actually sends to the speakers: the listener's mixed output (AudioListener.GetOutputData, after the listener's
    // volume), as peak and RMS over both channels. A muted check reports these; both must be 0. The vehicle's engine hum (VehicleSound) keeps
    // a real source playing, so a 0 is a measurement of a muted signal, not of silence.
    public sealed class AudioMeter : MonoBehaviour
    {
        public float Peak { get; private set; }
        public float Rms { get; private set; }
        public float MaxPeakSeen { get; private set; }
        public int Samples { get; private set; }
        int logged;
        readonly float[] buffer = new float[1024];

        void Update()
        {
            float peak = 0, sum = 0; int n = 0;
            for (int ch = 0; ch < 2; ch++)
            {
                AudioListener.GetOutputData(buffer, ch);
                foreach (var s in buffer) { float a = Mathf.Abs(s); if (a > peak) peak = a; sum += s * s; n++; }
            }
            Peak = peak; Rms = n > 0 ? Mathf.Sqrt(sum / n) : 0; Samples++;
            if (peak > MaxPeakSeen) MaxPeakSeen = peak;
            // Round 10: a muted run must read 0; any signal is logged with the sources playing then (the final Coaster Coast soak read 0.44 once)
            if (peak > 1e-4f && logged < 5)
            {
                logged++; var playing = new System.Text.StringBuilder();
                foreach (var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None)) if (a.isPlaying && a.volume > 0) playing.Append($"{a.name}:{(a.clip ? a.clip.name : "-")}@{a.volume:0.00} ");
                Debug.Log($"[Audio] output peak {peak:0.000000} at {Time.time:0.00} s (frame {Time.frameCount}), AudioListener.volume {AudioListener.volume}, paused {AudioListener.pause}; playing: {playing}");
            }
        }
    }
}
