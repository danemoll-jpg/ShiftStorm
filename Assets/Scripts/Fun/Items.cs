using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 17 item 1 (Dan: "power ups / weapons Mario Kart style but I don't want them to be golf based ... a little more creative"): the
    // items. Rows of item boxes on every course, in every form (on the road, floating on the water, in the air on the flight lines, in the
    // caves), on the main line, the shortcuts and the big route splits (ItemField). B (Q on the keyboard) uses the item held; one at a time.
    // Every item works in all four forms (ShiftVehicle.Effects: what each does to each form). Odds by place (better ones further back,
    // Buck's Big Finish only for the last), a grace after any hit (no stun-locks), a warning for anything coming at you, AI use (RivalItems).
    // Off in Time Trial, the rival challenges and when Menu > Settings > Gameplay > Items is Off.
    public enum ItemKind
    {
        None, WrongForm, RainbowBridge, BubbleTrap, SwapSpot, Mirage, Rewind, PocketStorm, HypeHorn, BubbleShield, Slick,
        DinoEgg, FireworkRocket, HumanCannonball, FloodWave, BigFinish
    }

    public static class Items
    {
        public static readonly ItemKind[] All =
        {
            ItemKind.WrongForm, ItemKind.RainbowBridge, ItemKind.BubbleTrap, ItemKind.SwapSpot, ItemKind.Mirage, ItemKind.Rewind, ItemKind.PocketStorm,
            ItemKind.HypeHorn, ItemKind.BubbleShield, ItemKind.Slick, ItemKind.DinoEgg, ItemKind.FireworkRocket, ItemKind.HumanCannonball, ItemKind.FloodWave, ItemKind.BigFinish
        };
        public static string Name(ItemKind k) => k switch
        {
            ItemKind.WrongForm => "Wrong Form", ItemKind.RainbowBridge => "Rainbow Bridge", ItemKind.BubbleTrap => "Bubble Trap", ItemKind.SwapSpot => "Swap Spot",
            ItemKind.Mirage => "Mirage", ItemKind.Rewind => "Rewind", ItemKind.PocketStorm => "Pocket Storm", ItemKind.HypeHorn => "Hype Horn",
            ItemKind.BubbleShield => "Bubble Shield", ItemKind.Slick => "Slick", ItemKind.DinoEgg => "Dino Egg", ItemKind.FireworkRocket => "Firework Rocket",
            ItemKind.HumanCannonball => "Human Cannonball", ItemKind.FloodWave => "Mini Flood", ItemKind.BigFinish => "Buck's Big Finish", _ => ""
        };
        public static string Hint(ItemKind k) => k switch
        {
            ItemKind.WrongForm => "zaps the racer ahead into the wrong form",
            ItemKind.RainbowBridge => "a rainbow speed lane ahead, only for you",
            ItemKind.BubbleTrap => "floats the racer ahead up in a bubble",
            ItemKind.SwapSpot => "trade places with the racer just ahead",
            ItemKind.Mirage => "three fake copies of you soak up attacks",
            ItemKind.Rewind => "undo your last 3 seconds",
            ItemKind.PocketStorm => "a rain cloud soaks the racers ahead",
            ItemKind.HypeHorn => "stunt points x2 for 10 seconds",
            ItemKind.BubbleShield => "held: blocks a hit from behind; B: a bubble round you",
            ItemKind.Slick => "held: blocks a hit from behind; B: drop it",
            ItemKind.DinoEgg => "hatches a baby raptor to chase the racer ahead",
            ItemKind.FireworkRocket => "a rocket after the racer ahead",
            ItemKind.HumanCannonball => "fires a clown at the racer ahead",
            ItemKind.FloodWave => "a wave rolls over the racers ahead",
            ItemKind.BigFinish => "Buck's stunt cannon blasts you up the field",
            _ => ""
        };
        public static Color Tint(ItemKind k) => k switch
        {
            ItemKind.WrongForm => new Color(.75f, .35f, 1f), ItemKind.RainbowBridge => new Color(1f, .5f, .8f), ItemKind.BubbleTrap => new Color(.55f, .85f, 1f),
            ItemKind.SwapSpot => new Color(1f, .85f, .25f), ItemKind.Mirage => new Color(.7f, .95f, 1f), ItemKind.Rewind => new Color(.4f, 1f, .6f),
            ItemKind.PocketStorm => new Color(.45f, .5f, .7f), ItemKind.HypeHorn => new Color(1f, .6f, .15f), ItemKind.BubbleShield => new Color(.5f, .95f, .95f),
            ItemKind.Slick => new Color(.85f, .55f, 1f), ItemKind.DinoEgg => new Color(.95f, .9f, .7f), ItemKind.FireworkRocket => new Color(1f, .3f, .25f),
            ItemKind.HumanCannonball => new Color(1f, .4f, .6f), ItemKind.FloodWave => new Color(.3f, .6f, 1f), ItemKind.BigFinish => new Color(1f, .8f, .2f), _ => Color.white
        };
        public static bool Attack(ItemKind k) => k == ItemKind.WrongForm || k == ItemKind.BubbleTrap || k == ItemKind.PocketStorm || k == ItemKind.DinoEgg
                                                 || k == ItemKind.FireworkRocket || k == ItemKind.HumanCannonball || k == ItemKind.FloodWave || k == ItemKind.SwapSpot;
        public static bool HeldBehind(ItemKind k) => k == ItemKind.BubbleShield || k == ItemKind.Slick;
        // the course's own item
        public static ItemKind Themed(Course c) => c == null ? ItemKind.DinoEgg : c.look switch { "coaster" => ItemKind.FireworkRocket, "bigtop" => ItemKind.HumanCannonball, "dam" => ItemKind.FloodWave, _ => ItemKind.DinoEgg };

        static Texture2D[] icons;
        public static Texture2D Icon(ItemKind k)
        {
            if (icons == null) { icons = new Texture2D[All.Length + 1]; foreach (var i in All) icons[(int)i] = Resources.Load<Texture2D>("UI/Items/" + i); }
            return (int)k < icons.Length ? icons[(int)k] : null;
        }

        // Menu > Settings > Gameplay > Items (default On); a race, Stunt Show, The Tour and 2 Players race have them; a time trial, a rival
        // challenge and a check that turns them off do not
        public static bool ForcedOff;   // checks: "-noItems"
        public static bool OnNow(Game g) => !ForcedOff && Settings.Items && g && g.RaceMode && Challenge.Active == null;

        // ---- odds by place (f: 0 = in the lead, 1 = last): weights from the leader's to the last's ----
        static readonly (ItemKind k, float lead, float last)[] Odds =
        {
            (ItemKind.Slick, 28, 4), (ItemKind.BubbleShield, 24, 4), (ItemKind.Mirage, 14, 6), (ItemKind.HypeHorn, 16, 7), (ItemKind.RainbowBridge, 9, 14),
            (ItemKind.Rewind, 6, 9), (ItemKind.BubbleTrap, 5, 13), (ItemKind.WrongForm, 2, 14), (ItemKind.PocketStorm, 2, 12), (ItemKind.DinoEgg, 4, 14),
            (ItemKind.SwapSpot, 0, 12), (ItemKind.BigFinish, 0, 3),
        };
        public static ItemKind Roll(int place, int field, Course c, System.Random rnd)
        {
            float f = field > 1 ? (place - 1) / (float)(field - 1) : .5f; float sum = 0;
            var w = new float[Odds.Length];
            for (int i = 0; i < Odds.Length; i++)
            {
                var o = Odds[i]; float x = Mathf.Lerp(o.lead, o.last, f);
                if (o.k == ItemKind.BigFinish && (place < field || field < 3)) x = 0;   // Buck's Big Finish: only the back of the pack
                if (o.k == ItemKind.SwapSpot && place <= 1) x = 0;
                w[i] = x; sum += x;
            }
            float r = (float)rnd.NextDouble() * sum;
            for (int i = 0; i < Odds.Length; i++) { r -= w[i]; if (r <= 0) { var k = Odds[i].k; return k == ItemKind.DinoEgg ? Themed(c) : k; } }
            return ItemKind.Slick;
        }

        // every item event, for the checks and the playtest log: when, who, what, at whom, how it went
        public static readonly List<string> Log = new List<string>();
        public static readonly List<(int place, int field, ItemKind k)> Rolls = new List<(int, int, ItemKind)>();
        public static void Note(ShiftVehicle by, string what) { string line = $"{Time.time:0.00} {(by ? by.RacerName : "-")} {what}"; Log.Add(line); Debug.Log("[Items] " + line); if (by) by.Note("item", what); }
    }

    // a row of item boxes across the line (4 boxes, 5.5 m apart), each back 3 s after it was taken
    public sealed class ItemBox : MonoBehaviour
    {
        public static readonly List<ItemBox> All = new List<ItemBox>();
        public Course course; public Transform cube; float backAt = -1; public string where = "";
        public bool Ready => backAt < 0;
        public Mode form;   // the form a racer has here (pictures, the check)
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDestroy() => All.Remove(this);
        public void Take() { backAt = Time.time + 3f; if (cube) cube.gameObject.SetActive(false); }
        void Update()
        {
            if (backAt > 0 && Time.time >= backAt) { backAt = -1; if (cube) cube.gameObject.SetActive(true); }
            if (cube && cube.gameObject.activeSelf) { cube.localRotation = Quaternion.Euler(20f, Time.time * 90f, 35f); float s = 1f + .06f * Mathf.Sin(Time.time * 4f + transform.position.x); cube.localScale = Vector3.one * 1.7f * s; }
        }
    }

    // the item boxes on every course, made once at start-up from each course's line (and its shortcuts' lines), and the pick-ups
    public sealed class ItemField : MonoBehaviour
    {
        public static ItemField Instance { get; private set; }
        Game game; readonly System.Random rnd = new System.Random(1717);
        public int Rows { get; private set; }
        public static ItemField Make(Game g)
        {
            var f = g.gameObject.AddComponent<ItemField>(); Instance = f; f.game = g;
            foreach (var c in g.Courses) f.Place(c);
            Debug.Log($"[Items] {f.Rows} rows of item boxes ({ItemBox.All.Count} boxes) on {g.Courses.Count} courses");
            return f;
        }

        // rows every ~1/9 of the lap, never near a jump's lip, a rail / the loop, a gate or a form change zone; one in the middle of each
        // shortcut long enough (the big route splits have three)
        void Place(Course c)
        {
            var parent = new GameObject("Item boxes").transform; parent.SetParent(c.transform, false);
            float len = c.laps > 1 && c.lapStarts.Count > 1 ? c.lapStarts[1] - c.lapStarts[0] : c.Length;
            float from = c.laps > 1 && c.lapStarts.Count > 0 ? c.lapStarts[0] : 0;
            int n = Mathf.Clamp(Mathf.RoundToInt(len / 650f), 6, 11);
            int laps = 1;   // Dam Shame's line is its lap three times over the same ground: one set of rows serves every lap
            for (int lap = 0; lap < laps; lap++)
                for (int i = 0; i < n; i++)
                {
                    float p0 = from + lap * len + len * (i + .55f) / n;
                    for (float d = 0; d < len / n * .6f; d += 15f)
                    {
                        float p = p0 + (((int)(d / 15f)) % 2 == 0 ? d : -d);
                        if (p < 150 || p > c.Length - 80 || Blocked(c, p)) continue;
                        var q = c.PointAt(p, out var dir); Row(c, parent, q, dir, $"{c.shortName} {p:0} m"); break;
                    }
                }
            foreach (var sc in c.shortcuts)
            {
                if (sc.Length < 220) continue;
                int k = sc.Length > 1200 ? 3 : 1;
                for (int i = 0; i < k; i++) { float s = sc.Length * (i + .5f) / k; if (NearRing(sc, s)) s += 30f; var q = sc.PointAt(s, out var dir); Row(c, parent, q, dir, $"{c.shortName} {sc.label} {s:0} m"); }
            }
        }
        static bool NearRing(Shortcut sc, float s) { foreach (var r in sc.rings) if (Mathf.Abs(r - s) < 25f) return true; return false; }
        static bool Blocked(Course c, float p)
        {
            foreach (var g in c.gates) if (Mathf.Abs(g.progress - p) < 45f) return true;
            var st = c.stunts;
            if (st != null)
            {
                foreach (var j in st.Jumps) if (p > j.lip - 120f && p < j.lip + j.gap + 60f) return true;
                foreach (var r in st.Rails) if (p > r.x - 80f && p < r.y + 40f) return true;
            }
            var q = c.PointAt(p, out _);
            foreach (var z in TransformZone.All) if (z && Vector3.Distance(z.transform.position, q) < 55f) return true;
            foreach (var l in ChoiceLip.All) if (l && Vector3.Distance(l.transform.position, q) < 90f) return true;
            return false;
        }
        void Row(Course c, Transform parent, Vector3 q, Vector3 dir, string where)
        {
            Rows++;
            var flat = Vector3.ProjectOnPlane(dir, Vector3.up); if (flat.sqrMagnitude < 1e-4f) flat = Vector3.forward; flat.Normalize();
            var right = Vector3.Cross(Vector3.up, flat);
            // the line is where a racer aims: the road + 0.9 m, the water + 0.6 m, the flight altitude, the dive depth
            bool water = WaterVolume.SurfaceAt(q, out float surf, 4f);
            Mode form = Physics.Raycast(q + Vector3.up, Vector3.down, 3.5f, ShiftVehicle.GroundMask, QueryTriggerInteraction.Ignore) && !(water && q.y < surf + 1.5f) ? Mode.Drive
                      : water && q.y < surf - 1.5f ? Mode.Dive : water && q.y < surf + 2f ? Mode.Boat : Mode.Fly;
            float y = form == Mode.Drive ? q.y + .5f : form == Mode.Boat ? surf + 1.1f : q.y;
            for (int i = 0; i < 4; i++)
            {
                var g = new GameObject("Item box"); g.transform.SetParent(parent, false);
                g.transform.position = new Vector3(q.x, y, q.z) + right * ((i - 1.5f) * 5.5f);
                var b = g.AddComponent<ItemBox>(); b.course = c; b.where = where; b.form = form;
                b.cube = ItemFx.BoxMesh(g.transform);
            }
        }

        void FixedUpdate()
        {
            if (!game || !game.Race) return;
            bool on = Items.OnNow(game);
            foreach (var v in game.Race.Racers)
            {
                if (!v || !v.gameObject.activeInHierarchy || v.Holding || v.Finished) continue;
                var it = v.Bag; if (!it || !on || it.Held != ItemKind.None || it.Rolling) continue;
                var p = v.Body.position;
                foreach (var b in ItemBox.All)
                {
                    if (b.course != v.Course || !b.Ready) continue;
                    if ((b.transform.position - p).sqrMagnitude > 3.6f * 3.6f) continue;
                    b.Take();
                    int place = game.Race.Place(v), field = game.Race.Racers.Count;
                    var k = Items.Roll(place, field, v.Course, rnd); Items.Rolls.Add((place, field, k));
                    it.Give(k, b.where);
                    break;
                }
            }
        }
    }
}
