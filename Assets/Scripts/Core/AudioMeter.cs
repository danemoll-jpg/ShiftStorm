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
        }
    }
}
