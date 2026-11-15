using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Paradise Plunge's sound: the music, the jungle ambience above water and the bubbles ambience under it (cross-faded on the camera
    // being under water), splashes, a far dinosaur call now and then. Everything goes through the AudioListener, so a muted run (QuietRun)
    // stays at 0 output; MusicPeak measures the music source's own signal, so a check can see it plays without unmuting.
    // Round 7 item 5: the music has its own child object with a low-pass filter (a filter on the camera would muffle everything):
    //   race:    Dan's Suno "race_paradise" (+ _intro) when it is in Resources/Audio/Music, else Tee'd Off's Jurassic Par
    //   menu:    "theme" on the main menu page and "menu" in the Garage / Settings when Dan's songs are in; Tee'd Off's title until then
    //            (ph_menu); the race music waits underneath and comes back where it was
    //   results: "results" (else Tee'd Off's round_end, ph_results), once
    // and the concept's mix by form, with filters, not separate songs: in flight a little louder and fully open, on the road and the
    // water as it is, underwater softer and muffled (low-pass 650 Hz), bright again at the waterfall exit (a 2.5 s lift as the car
    // leaves the ramp). All at the Music volume; the ambience and splashes at the Effects volume.
    public sealed class CourseAudio : MonoBehaviour
    {
        public static CourseAudio Instance { get; private set; }
        AudioSource music, musicLoop, alt, altLoop, jungle, bubbles, oneShot; AudioLowPassFilter lp;
        AudioClip intro, loop, menuIntro, menuLoop, themeIntro, themeLoop, resultsClip, splashA, splashB; readonly List<AudioClip> calls = new List<AudioClip>();
        bool on; float nextCall, brightUntil = -1; ShiftVehicle v; string altNow = "";
        public bool MusicPlaying => (music && music.isPlaying) || (musicLoop && musicLoop.isPlaying) || (alt && alt.isPlaying) || (altLoop && altLoop.isPlaying);
        public string MusicClip => altNow != "" && (alt.isPlaying || altLoop.isPlaying) ? (altLoop.isPlaying ? altLoop.clip.name : alt.clip.name) : musicLoop && musicLoop.isPlaying ? musicLoop.clip.name : music && music.isPlaying ? music.clip.name : "";
        public float MusicPeak { get; private set; }
        public const float MusicGain = 1.3f;
        public AudioSource MusicSource => altNow != "" ? (altLoop.isPlaying ? altLoop : alt) : musicLoop.isPlaying ? musicLoop : music;
        public float MusicVolume => musicLoop ? Mathf.Max(musicLoop.volume, music.volume) : 0;
        public float MusicCutoff => lp ? lp.cutoffFrequency : 0;
        public string Track => altNow != "" ? altNow : "race";
        public int Splashes { get; private set; }
        readonly float[] buf = new float[512];

        public static CourseAudio Make(GameObject host, ShiftVehicle v)
        {
            var a = host.AddComponent<CourseAudio>(); a.v = v; Instance = a;
            var mh = new GameObject("Music"); mh.transform.SetParent(host.transform, false);
            AudioSource Src(GameObject on, bool loopIt, float vol) { var s = on.AddComponent<AudioSource>(); s.loop = loopIt; s.playOnAwake = false; s.spatialBlend = 0; s.volume = vol; return s; }
            a.music = Src(mh, false, .5f); a.musicLoop = Src(mh, true, .5f); a.alt = Src(mh, false, 0); a.altLoop = Src(mh, true, 0);
            a.lp = mh.AddComponent<AudioLowPassFilter>(); a.lp.cutoffFrequency = 22000; a.lp.lowpassResonanceQ = 1;   // after its sources (Unity needs one first)
            a.jungle = Src(host, true, 0); a.bubbles = Src(host, true, 0); a.oneShot = Src(host, false, .8f);
            AudioClip M(string n) => Resources.Load<AudioClip>("Audio/Music/" + n);
            a.loop = M("race_paradise"); a.intro = a.loop ? M("race_paradise_intro") : null; bool dan = a.loop;
            if (!a.loop) { a.intro = M("course_jurassic_intro"); a.loop = M("course_jurassic"); }
            a.menuLoop = M("menu"); a.menuIntro = a.menuLoop ? M("menu_intro") : null; if (!a.menuLoop) { a.menuLoop = M("ph_menu"); a.menuIntro = M("ph_menu_intro"); }
            a.themeLoop = M("theme"); a.themeIntro = a.themeLoop ? M("theme_intro") : null; if (!a.themeLoop) { a.themeLoop = a.menuLoop; a.themeIntro = a.menuIntro; }
            a.resultsClip = M("results") ?? M("ph_results");
            a.jungle.clip = Resources.Load<AudioClip>("Audio/Sfx/amb_jungle"); a.bubbles.clip = Resources.Load<AudioClip>("Audio/Sfx/amb_bubbles");
            a.splashA = Resources.Load<AudioClip>("Audio/Sfx/splash_1"); a.splashB = Resources.Load<AudioClip>("Audio/Sfx/splash_2");
            foreach (var n in new[] { "amb_dino_call_1", "amb_dino_call_2", "amb_dino_call_3", "amb_roar_far_1" }) { var c = Resources.Load<AudioClip>("Audio/Sfx/" + n); if (c) a.calls.Add(c); }
            v.Event += a.OnEvent;
            Debug.Log($"[Audio] race music {(a.loop ? a.loop.name : "MISSING")} ({(dan ? "Dan's" : "Tee'd Off placeholder")}, intro {(a.intro ? "yes" : "no")}), menu {(a.menuLoop ? a.menuLoop.name : "-")}, theme {(a.themeLoop ? a.themeLoop.name : "-")}, results {(a.resultsClip ? a.resultsClip.name : "-")}; ambience {(a.jungle.clip ? "jungle" : "MISSING")} / {(a.bubbles.clip ? "bubbles" : "MISSING")}, splashes {(a.splashA ? 2 : 0)}, calls {a.calls.Count}");
            return a;
        }

        public void SetCourse(Course c)
        {
            bool want = c && c.look == "jurassic";
            if (want == on) { if (on) RestartRace(); return; }
            on = want;
            if (on) { RestartRace(); if (jungle.clip) jungle.Play(); if (bubbles.clip) bubbles.Play(); nextCall = Time.time + 12f; }
            else { music.Stop(); musicLoop.Stop(); alt.Stop(); altLoop.Stop(); altNow = ""; jungle.Stop(); bubbles.Stop(); }
        }
        void RestartRace()
        {
            music.Stop(); musicLoop.Stop(); alt.Stop(); altLoop.Stop(); altNow = ""; musicLoop.clip = loop; SoundLog.Add("music", "race " + (loop ? loop.name : "-"));
            double t0 = AudioSettings.dspTime + .1;
            if (intro && loop) { music.clip = intro; music.PlayScheduled(t0); musicLoop.PlayScheduled(t0 + (double)intro.samples / intro.frequency); }
            else if (loop) musicLoop.PlayScheduled(t0);
        }
        void PlayAlt(string which)
        {
            if (altNow == which) return; altNow = which; alt.Stop(); altLoop.Stop();
            SoundLog.Add("music", which == "" ? "race (back)" : which);
            if (which == "") return;
            if (which == "results") { alt.clip = resultsClip; if (resultsClip) alt.Play(); return; }
            var (i, l) = which == "theme" ? (themeIntro, themeLoop) : (menuIntro, menuLoop);
            double t0 = AudioSettings.dspTime + .05; altLoop.clip = l;
            if (i && l) { alt.clip = i; alt.PlayScheduled(t0); altLoop.PlayScheduled(t0 + (double)i.samples / i.frequency); } else if (l) altLoop.PlayScheduled(t0);
        }

        void OnEvent(string kind, string text)
        {
            if (!on || kind != "change") return;
            // Round 9 item 3: the splash only for the waterfall exit (Boat -> Drive); a landing in the water (Fly -> Boat) already has the
            // change's own "hull" splash (GameSounds): build 10 played two splashes at once there
            if (v.ChangeFrom == Mode.Boat && v.Mode == Mode.Drive) Splash();
            if (v.ChangeFrom == Mode.Boat && v.Mode == Mode.Drive) brightUntil = Time.time + 2.5f;   // the waterfall exit
        }
        // Round 6: player 2's splashes too (split screen), in the one shared mix
        public void Follow(ShiftVehicle x) { x.Event += (kind, text) => { if (on && kind == "change" && x.ChangeFrom == Mode.Boat && x.Mode == Mode.Drive) Splash(); }; }
        public void Splash() { if (SoundLog.Busy("splash")) { SoundLog.Merged++; return; } var c = Random.value < .5f ? splashA : splashB; if (c) { oneShot.PlayOneShot(c, .9f * Settings.Effects); Splashes++; SoundLog.Add("splash", c.name); } }

        void Update()
        {
            if (!on) return;
            var g = Game.Instance;
            // which track: the results once, the menu (the theme on its main page), else the race underneath
            string want = g && g.Trial && g.Trial.HasResults && g.Player.Finished ? "results" : g && g.MenuOpen ? (g.MenuPage == Game.Page.Main ? "theme" : "menu") : "";
            if (want != altNow && !(want == "results" && altNow == "results")) PlayAlt(want);
            bool under = ChaseCam.Underwater; float k = 1 - Mathf.Exp(-4f * Time.unscaledDeltaTime);
            jungle.volume = Mathf.Lerp(jungle.volume, (under ? 0f : .38f) * Settings.Effects, k); bubbles.volume = Mathf.Lerp(bubbles.volume, (under ? .5f : 0f) * Settings.Effects, k);
            // the mix by form (the player's own racer): louder and open in flight, muffled underwater, a lift at the waterfall exit
            var m = v ? v.Mode : Mode.Drive; bool bright = Time.time < brightUntil;
            // Round 8 item 6: music louder than build 8 (x1.3, +2.3 dB: road .65, flight .78, underwater .39) and ducked 8 dB under TeedOff Barker
            float vol = ((under || m == Mode.Dive ? .3f : m == Mode.Fly ? .6f : .5f) + (bright ? .12f : 0)) * MusicGain * Caller.Duck;
            float cut = under || m == Mode.Dive ? 650f : 22000f;
            float raceVol = altNow == "" ? vol * Settings.Music : 0, altVol = altNow != "" ? .55f * MusicGain * Settings.Music : 0;
            music.volume = musicLoop.volume = Mathf.Lerp(musicLoop.volume, raceVol, k);
            alt.volume = altLoop.volume = Mathf.Lerp(altLoop.volume, altVol, k);
            lp.cutoffFrequency = Mathf.Exp(Mathf.Lerp(Mathf.Log(lp.cutoffFrequency), Mathf.Log(altNow != "" ? 22000f : cut), k));
            // the race music holds its place under the menu (paused at zero volume would drift its scheduled loop: it keeps running)
            if (calls.Count > 0 && Time.time > nextCall && v && v.Mode == Mode.Fly) { oneShot.PlayOneShot(calls[Random.Range(0, calls.Count)], .45f * Settings.Effects); nextCall = Time.time + Random.Range(14f, 24f); SoundLog.Add("ambience", "far dinosaur call"); }
            else if (Time.time > nextCall + 30f) nextCall = Time.time + 10f;
            var playing = altNow != "" ? (altLoop.isPlaying ? altLoop : alt.isPlaying ? alt : null) : musicLoop.isPlaying ? musicLoop : music.isPlaying ? music : null;
            if (playing) { playing.GetOutputData(buf, 0); float p = 0; foreach (var s in buf) p = Mathf.Max(p, Mathf.Abs(s)); MusicPeak = Mathf.Max(MusicPeak, p); }
        }
    }
}
