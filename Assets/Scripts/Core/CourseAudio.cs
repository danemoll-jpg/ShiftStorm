using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Paradise Plunge's sound, first pass (all copied from Tee'd Off, Resources/Audio, read-only there): the Jurassic Par course music
    // (course_jurassic_intro, then course_jurassic looping), the jungle ambience above water and the bubbles ambience under it (cross-faded on
    // the camera being under water), a splash on landing in water and on the boat leaving the ramp through the waterfall, a far dinosaur call
    // now and then. The test courses keep the Round 1 sound (the engine hum only). Everything goes through the AudioListener, so a muted run
    // (QuietRun) stays at 0 output; MusicPeak measures the music source's own signal, so a check can see it plays without unmuting.
    public sealed class CourseAudio : MonoBehaviour
    {
        public static CourseAudio Instance { get; private set; }
        AudioSource music, musicLoop, jungle, bubbles, oneShot; AudioClip intro, loop, splashA, splashB; readonly List<AudioClip> calls = new List<AudioClip>();
        bool on; float nextCall; ShiftVehicle v;
        public bool MusicPlaying => (music && music.isPlaying) || (musicLoop && musicLoop.isPlaying);
        public string MusicClip => musicLoop && musicLoop.isPlaying ? musicLoop.clip.name : music && music.isPlaying ? music.clip.name : "";
        public float MusicPeak { get; private set; }
        public int Splashes { get; private set; }
        readonly float[] buf = new float[512];

        public static CourseAudio Make(GameObject host, ShiftVehicle v)
        {
            var a = host.AddComponent<CourseAudio>(); a.v = v; Instance = a;
            AudioSource Src(bool loopIt, float vol) { var s = host.AddComponent<AudioSource>(); s.loop = loopIt; s.playOnAwake = false; s.spatialBlend = 0; s.volume = vol; return s; }
            a.music = Src(false, .5f); a.musicLoop = Src(true, .5f); a.jungle = Src(true, 0); a.bubbles = Src(true, 0); a.oneShot = Src(false, .8f);
            a.intro = Resources.Load<AudioClip>("Audio/Music/course_jurassic_intro"); a.loop = Resources.Load<AudioClip>("Audio/Music/course_jurassic");
            a.jungle.clip = Resources.Load<AudioClip>("Audio/Sfx/amb_jungle"); a.bubbles.clip = Resources.Load<AudioClip>("Audio/Sfx/amb_bubbles");
            a.splashA = Resources.Load<AudioClip>("Audio/Sfx/splash_1"); a.splashB = Resources.Load<AudioClip>("Audio/Sfx/splash_2");
            foreach (var n in new[] { "amb_dino_call_1", "amb_dino_call_2", "amb_dino_call_3", "amb_roar_far_1" }) { var c = Resources.Load<AudioClip>("Audio/Sfx/" + n); if (c) a.calls.Add(c); }
            v.Event += a.OnEvent;
            Debug.Log($"[Audio] music {(a.loop ? a.loop.name : "MISSING")} (+ intro {(a.intro ? "yes" : "no")}), ambience {(a.jungle.clip ? "jungle" : "MISSING")} / {(a.bubbles.clip ? "bubbles" : "MISSING")}, splashes {(a.splashA ? 2 : 0)}, calls {a.calls.Count}");
            return a;
        }

        public void SetCourse(Course c)
        {
            bool want = c && c.look == "jurassic";
            if (want == on) return; on = want;
            if (on)
            {
                music.Stop(); musicLoop.Stop(); musicLoop.clip = loop;
                double t0 = AudioSettings.dspTime + .1;
                if (intro && loop) { music.clip = intro; music.PlayScheduled(t0); musicLoop.PlayScheduled(t0 + (double)intro.samples / intro.frequency); }
                else if (loop) musicLoop.PlayScheduled(t0);
                if (jungle.clip) jungle.Play(); if (bubbles.clip) bubbles.Play();
                nextCall = Time.time + 12f;
            }
            else { music.Stop(); musicLoop.Stop(); jungle.Stop(); bubbles.Stop(); }
        }

        void OnEvent(string kind, string text)
        {
            if (!on || kind != "change") return;
            if ((v.ChangeFrom == Mode.Fly && v.Mode == Mode.Boat) || (v.ChangeFrom == Mode.Boat && v.Mode == Mode.Drive)) Splash();
        }
        // Round 6: player 2's splashes too (split screen), in the one shared mix
        public void Follow(ShiftVehicle x) { x.Event += (kind, text) => { if (on && kind == "change" && ((x.ChangeFrom == Mode.Fly && x.Mode == Mode.Boat) || (x.ChangeFrom == Mode.Boat && x.Mode == Mode.Drive))) Splash(); }; }
        public void Splash() { var c = Random.value < .5f ? splashA : splashB; if (c) { oneShot.PlayOneShot(c, .9f); Splashes++; } }

        void Update()
        {
            if (!on) return;
            bool under = ChaseCam.Underwater; float k = 1 - Mathf.Exp(-4f * Time.unscaledDeltaTime);
            jungle.volume = Mathf.Lerp(jungle.volume, under ? 0f : .38f, k); bubbles.volume = Mathf.Lerp(bubbles.volume, under ? .5f : 0f, k);
            music.volume = musicLoop.volume = Mathf.Lerp(musicLoop.volume, under ? .32f : .5f, k);   // a little quieter under water
            if (calls.Count > 0 && Time.time > nextCall && v && v.Mode == Mode.Fly) { oneShot.PlayOneShot(calls[Random.Range(0, calls.Count)], .45f); nextCall = Time.time + Random.Range(14f, 24f); }
            else if (Time.time > nextCall + 30f) nextCall = Time.time + 10f;
            var playing = musicLoop.isPlaying ? musicLoop : music.isPlaying ? music : null;
            if (playing) { playing.GetOutputData(buf, 0); float p = 0; foreach (var s in buf) p = Mathf.Max(p, Mathf.Abs(s)); MusicPeak = Mathf.Max(MusicPeak, p); }
        }
    }
}
