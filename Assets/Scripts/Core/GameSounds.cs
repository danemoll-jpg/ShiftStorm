using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 7 item 5: the sound pass. One-shot effects for a people's racer (player 1, player 2 in the same shared mix), chosen by Code
    // from the three read-only libraries and copied into Resources/Audio/Sfx (Docs/SOUNDS.md lists each one and where it came from):
    //   Tee'd Off (to_*: ElevenLabs-made): whooshes, chimes, splashes, thumps, the countdown beep, cheers, the win jingle, the menu clicks
    //   Hostile Horizon (hh_*: its spaceship sounds): the launch sting (wings out), the canopy seal (dive), gear down (wheels), the
    //     jump-drive kick (boost), the canopy scrape (walls), its water splash
    //   Chastain Rush: its synthesised engines (VehicleSoundSynthesis, ported into VehicleSound), no sample files
    // Every effect plays at the Effects volume through the AudioListener (so a muted run stays at 0); Played counts each for the checks.
    public sealed class GameSounds : MonoBehaviour
    {
        public static GameSounds Instance { get; private set; }
        AudioSource src; readonly Dictionary<string, AudioClip[]> set = new Dictionary<string, AudioClip[]>();
        public readonly Dictionary<string, int> Played = new Dictionary<string, int>();
        public readonly List<string> Missing = new List<string>();
        readonly HashSet<ShiftVehicle> watched = new HashSet<ShiftVehicle>();
        int lastCount = -1; Game game; bool wasMenu; int menuIndex = -1;
        public static float LastFinishSound = -99;   // the grandstand crowd (Crowd) stays quiet over the finish's own crowd sound

        // name -> clips (Resources/Audio/Sfx); several: one at random
        static readonly (string name, string[] files)[] Table =
        {
            ("whoosh", new[] { "to_swing_whoosh_1", "to_swing_whoosh_2" }),          // every form change (the swap's twirl)
            ("wings", new[] { "hh_sting_launch" }),                                    // into the plane
            ("seal", new[] { "hh_canopy_slam" }),                                      // into the sub (the canopy seals)
            ("wheels", new[] { "hh_gear_down" }),                                      // into the car
            ("hull", new[] { "to_sea_splash_1", "to_sea_splash_2" }),                  // into the boat (landing in water)
            ("boost", new[] { "to_power_whoosh" }), ("boostkick", new[] { "hh_jump_drive" }),
            ("gate", new[] { "to_fun_ding_1", "to_fun_ding_2" }), ("ring", new[] { "to_amb_chimes_1", "to_amb_chimes_2" }),
            ("miss", new[] { "to_crowd_aww" }),
            ("pad", new[] { "to_rail_zip_1", "to_rail_zip_2" }), ("loop", new[] { "to_slide_whoosh_1", "to_slide_whoosh_2" }), ("loopcrowd", new[] { "to_crowd_ooh" }),
            ("air", new[] { "to_cloud_whoosh_1", "to_cloud_whoosh_2" }), ("land", new[] { "to_soccer_thump_1", "to_soccer_thump_2" }),
            ("scrape", new[] { "hh_canopy_scrape" }), ("splash", new[] { "hh_water_splash", "to_pool_splash_1" }),
            ("beep", new[] { "to_rocket_beep" }), ("go", new[] { "to_fun_horn_1" }),
            // Round 9 item 3: Tee'd Off's win jingle (to_match_win) is gone: it played on top of ShiftStorm's own finish (Dan's "Victory" song)
            ("finish", new[] { "to_cheer_big" }), ("applause", new[] { "to_applause" }), ("lost", new[] { "to_crowd_clap_small" }),
            ("reset", new[] { "to_void_bloop_1", "to_void_bloop_2" }),
            ("ui_move", new[] { "to_ui_click" }), ("ui_select", new[] { "to_ui_select" }), ("ui_back", new[] { "to_ui_back" }),
        };

        public static GameSounds Make(GameObject host, Game g)
        {
            var s = host.AddComponent<GameSounds>(); Instance = s; s.game = g;
            s.src = host.AddComponent<AudioSource>(); s.src.spatialBlend = 0; s.src.playOnAwake = false;
            int n = 0;
            foreach (var (name, files) in Table)
            {
                var l = new List<AudioClip>(); foreach (var f in files) { var c = Resources.Load<AudioClip>("Audio/Sfx/" + f); if (c) l.Add(c); else s.Missing.Add(f); }
                s.set[name] = l.ToArray(); n += l.Count;
            }
            Debug.Log($"[Sounds] {n} effect clips for {Table.Length} sounds{(s.Missing.Count > 0 ? "; missing: " + string.Join(", ", s.Missing) : "")}");
            return s;
        }

        public void Play(string name, float vol = 1f, float pitch = 1f)
        {
            if (!set.TryGetValue(name, out var cs) || cs.Length == 0) return;
            if (SoundLog.Busy(KindOf(name))) { SoundLog.Merged++; return; }   // Round 9 item 3: never two of a kind at once (a change's whoosh on a jump's)
            var c = cs[Random.Range(0, cs.Length)];
            src.pitch = pitch; src.PlayOneShot(c, vol * Settings.Effects);
            Played[name] = (Played.TryGetValue(name, out var k) ? k : 0) + 1;
            SoundLog.Add(KindOf(name), c.name);
        }
        // Round 9: the kind of each effect (SoundLog)
        public static string KindOf(string name)
        {
            switch (name)
            {
                case "whoosh": case "boost": case "loop": case "air": return "whoosh";
                case "hull": case "splash": return "splash";
                case "gate": case "ring": return "chime";
                case "miss": case "loopcrowd": case "finish": case "applause": case "lost": return "crowd";
                case "land": case "scrape": return "impact";
                default: return name.StartsWith("ui_") ? "ui" : "mech";
            }
        }

        public void Watch(ShiftVehicle v)
        {
            if (!v || !watched.Add(v)) return;
            bool boosting = false;
            v.Event += (kind, text) =>
            {
                if (!v.Course || !v.Course.timeTrial && kind != "change") return;
                switch (kind)
                {
                    case "change":
                        Play("whoosh", .55f);
                        Play(v.Mode == Mode.Fly ? "wings" : v.Mode == Mode.Dive ? "seal" : v.Mode == Mode.Drive ? "wheels" : "hull", v.Mode == Mode.Boat ? .5f : .6f);
                        break;
                    // Round 9: no chime on the finish gate (G10): the finish has its own crowd sound and Dan's finish music
                    case "gate": if (!text.Contains("G10")) Play(text.Contains("near miss") ? "miss" : "gate", text.Contains("near miss") ? .6f : .45f); break;
                    case "shortcut": Play("ring", .5f); break;
                    case "go": Play("go", .55f); Crowd.OnRace("go", v, 0); break;
                    case "finish":
                        // Round 8 item 5: by place; Round 9 item 3: one crowd sound per finish (build 10 played the cheer, the applause and Tee'd Off's
                        // win jingle together, under Dan's finish song): 1st (or a time trial) the big cheer, a middle place a small clap, last an "aww"
                        { var race = game.Race; int place = race && race.Active ? race.Finishers.Count : 1, field = race && race.Active ? race.Racers.Count : 1; bool last = place == field && field > 1;
                          Play(place <= 1 ? "finish" : last ? "miss" : "lost", place <= 1 ? .6f : .7f); LastFinishSound = Time.time; Crowd.OnRace("finish", v, place); }
                        break;
                    case "reset": if (text.Contains("water")) Play("splash", .6f); Play("reset", .35f); break;
                }
            };
            v.Stunt += s =>
            {
                switch (s)
                {
                    case "pad": Play("pad", .6f); break;
                    case "loopin": Play("loop", .6f); break;
                    case "loop": Play("loopcrowd", .35f); break;
                    case "air": Play("air", .5f); break;
                    case "jump": Play("land", .7f); break;
                    case "wings": break;   // the change's own sounds
                }
            };
            v.WallHit += (angle, keep, what) => { if (keep > .85f) Play("scrape", .35f); };
            // the boost press: a whoosh and a short kick
            StartCoroutine(BoostWatch(v));
            System.Collections.IEnumerator BoostWatch(ShiftVehicle x)
            {
                while (x) { if (x.Boosting && !boosting && !x.Holding) { Play("boost", .45f); Play("boostkick", .25f); } boosting = x.Boosting; yield return null; }
            }
        }

        void Update()
        {
            // the countdown beeps (3, 2, 1) for player 1's start
            var p = game ? game.Player : null;
            if (p && p.Holding && p.Course && p.Course.timeTrial)
            {
                int left = Mathf.CeilToInt(p.HoldUntil - Time.time);
                if (left != lastCount && left >= 1 && left <= 3) Play("beep", .5f, 1f);
                lastCount = left;
            }
            else lastCount = -1;
            // the menu: a click on a move, a select on a choice, a back
            if (game && game.MenuOpen)
            {
                if (!wasMenu) { menuIndex = game.MenuIndex; Play("ui_select", .5f); }
                else if (game.MenuIndex != menuIndex) { menuIndex = game.MenuIndex; Play("ui_move", .4f); }
            }
            else if (wasMenu) Play("ui_back", .4f);
            wasMenu = game && game.MenuOpen;
        }
    }
}
