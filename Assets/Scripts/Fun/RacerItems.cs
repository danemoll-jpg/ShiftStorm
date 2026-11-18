using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 17 item 1: the item a racer holds (one at a time; a box gives a 1 s roulette, then the item) and what B does with it. People press
    // B (Q on the keyboard); the AI decides (below). What each item does: Items.Hint; to the racer hit: ShiftVehicle.Effects.
    public sealed class RacerItems : MonoBehaviour
    {
        public ShiftVehicle v;
        public ItemKind Held { get; private set; }
        public bool Rolling => rollUntil > 0;
        public ItemKind RollShown => Rolling ? Items.All[(int)(Time.time * 12f) % (Items.All.Length - 1)] : Held;
        public ShiftVehicle LastHitBy;
        public string Status { get; private set; } = "";   // a short line for the HUD ("NO ONE AHEAD", "CAN'T SWAP HERE")
        public float StatusUntil { get; private set; } = -1;
        public readonly List<string> Used = new List<string>();   // "kind form result" (checks)
        float rollUntil = -1, heldAt, thinkAt; ItemKind rolled; System.Random rnd;
        public int Picked { get; private set; }

        void Awake() { if (!v) v = GetComponent<ShiftVehicle>(); rnd = new System.Random(GetHashCode()); }
        public void Clear() { Held = ItemKind.None; rollUntil = -1; Status = ""; }
        public void NewRace() { Clear(); Used.Clear(); Picked = 0; }
        public void Give(ItemKind k, string where)
        {
            rolled = k; rollUntil = Time.time + (v.Human ? 1f : .5f); Picked++; if (v.Human) GameSounds.Instance?.Play("it_pick", .5f);
            Items.Note(v, $"item box at {where}: {Items.Name(k)} (place {(Game.Instance && Game.Instance.Race ? Game.Instance.Race.Place(v) : 0)})");
        }
        // checks: an item straight into the hand
        public void Set(ItemKind k) { Held = k; rollUntil = -1; heldAt = Time.time; thinkAt = Time.time; }
        void Say(string s) { Status = s; StatusUntil = Time.time + 1.6f; }

        void FixedUpdate()
        {
            if (!v) return;
            if (rollUntil > 0 && Time.time >= rollUntil)
            {
                rollUntil = -1; Held = rolled; heldAt = Time.time;
                thinkAt = Time.time + Mathf.Lerp(1f, 4f, (float)rnd.NextDouble()) * (Settings.Difficulty == Difficulty.Easy ? 1.6f : Settings.Difficulty == Difficulty.Hard ? .6f : 1f);
                if (Held == ItemKind.BigFinish) Buck.Instance?.BigFinishLine(v);
            }
            bool pressed = v.Input.ConsumeItem();
            if (Held == ItemKind.None || v.Holding || v.Finished || !Game.Instance || !Items.OnNow(Game.Instance)) return;
            if (v.Human && !v.Input.Scripted) { if (pressed) Use(); return; }
            if (v.Human && v.Input.Scripted) { if (pressed) Use(); else if (AiOnPlayer) Think(); return; }
            Think();
        }
        public bool AiOnPlayer;   // checks: the AI's item use for the check pilot's racer too

        // ---- the AI: sensible, never instant (a 1-4 s think, longer on Easy) ----
        void Think()
        {
            if (Time.time < thinkAt) return;
            var race = Game.Instance.Race; var ahead = Target(); float held = Time.time - heldAt;
            var behind = Behind(); float gapBehind = behind ? (v.RaceDistance - behind.RaceDistance) * GateLen : 1e4f;
            float gapAhead = ahead ? (ahead.RaceDistance - v.RaceDistance) * GateLen : 1e4f;
            bool use = false;
            switch (Held)
            {
                case ItemKind.BigFinish: use = true; break;
                case ItemKind.SwapSpot: use = ahead && gapAhead < 260f && gapAhead > 15f; break;
                case ItemKind.Slick: use = (behind && gapBehind < 35f) || held > 18f; break;
                case ItemKind.BubbleShield: use = Time.time < v.WarningUntil || held > 16f; break;
                case ItemKind.Mirage: use = (behind && gapBehind < 90f) || held > 9f; break;
                case ItemKind.Rewind: use = Time.time - v.LastWallAt < 1.2f && v.LastWallKeep < .55f; break;
                case ItemKind.HypeHorn: use = v.TrickAir || held > 10f; break;
                case ItemKind.RainbowBridge: use = !v.OnLoop && !v.Changing && held > 1.5f; break;
                default: use = Items.Attack(Held) && ((ahead && gapAhead < 320f) || held > 7f); break;
            }
            if (use && !Use()) thinkAt = Time.time + 1.5f;
        }
        float GateLen => v.Course && v.Course.gates.Count > 0 ? v.Course.Length / v.Course.gates.Count : 600f;

        // the racer one place ahead (the leader aims at the one behind); not finished, on the same course
        public ShiftVehicle Target()
        {
            var race = Game.Instance ? Game.Instance.Race : null; if (!race || !race.Active) return null;
            var o = race.Order(); int i = o.IndexOf(v);
            for (int k = i - 1; k >= 0; k--) if (o[k] && !o[k].Finished && o[k].gameObject.activeInHierarchy) return o[k];
            return null;
        }
        public ShiftVehicle Behind()
        {
            var race = Game.Instance ? Game.Instance.Race : null; if (!race || !race.Active) return null;
            var o = race.Order(); int i = o.IndexOf(v);
            for (int k = i + 1; k < o.Count; k++) if (o[k] && !o[k].Finished && o[k].gameObject.activeInHierarchy) return o[k];
            return null;
        }

        // B: use the item held (false: it cannot be used here and now; it is kept)
        public bool Use()
        {
            var k = Held; if (k == ItemKind.None || Rolling) return false;
            var ahead = Target(); var target = ahead ? ahead : Behind();
            string r = "";
            switch (k)
            {
                case ItemKind.WrongForm: case ItemKind.BubbleTrap: case ItemKind.DinoEgg: case ItemKind.FireworkRocket: case ItemKind.HumanCannonball:
                    if (!target) { Say("NO ONE TO AIM AT"); return false; }
                    ItemFx.Fire(k, v, target); r = $"at {target.RacerName}{(ahead ? "" : " (behind)")}"; break;
                case ItemKind.PocketStorm: ItemFx.Storm(v); r = "cloud ahead"; break;
                case ItemKind.FloodWave: ItemFx.Wave(v); r = "wave ahead"; break;
                case ItemKind.SwapSpot:
                    if (!ahead) { Say("NO ONE AHEAD"); return false; }
                    if (!v.SwapWith(ahead)) { Say("CAN'T SWAP HERE"); return false; }
                    r = $"with {ahead.RacerName}"; ahead.Bag.LastHitBy = v; GameSounds.Instance?.At("it_swap", v.Body.position, .7f); break;
                case ItemKind.Mirage: v.Decoys = 3; v.DecoysUntil = Time.time + 9f; ItemFx.Mirage(v, 9f); r = "3 decoys"; break;
                case ItemKind.Rewind: if (!v.Rewind()) { Say("NOTHING TO REWIND"); return false; } r = "3 s back"; GameSounds.Instance?.At("it_rewind", v.Body.position, .6f); break;
                case ItemKind.HypeHorn: v.HypeUntil = Time.time + 10f; ItemFx.Horn(v); r = "stunts x2 for 10 s"; break;
                case ItemKind.BubbleShield: v.ShieldUntil = Time.time + 12f; ItemFx.Shield(v, 12f); r = "shield up"; break;
                case ItemKind.Slick: ItemFx.Slick(v); r = "dropped"; break;
                case ItemKind.RainbowBridge: if (v.OnLoop) { Say("NOT ON A RAIL"); return false; } if (v.Lane) Object.Destroy(v.Lane.gameObject); v.Lane = ItemFx.MakeLane(v); r = "lane laid"; break;
                case ItemKind.BigFinish: if (!v.BigFinish()) { Say("NO ROOM FOR THE CANNON"); return false; } r = "fired"; ItemFx.Puff(v.Body.position, new Color(1f, .8f, .2f), 4f); GameSounds.Instance?.At("it_boom", v.Body.position, .8f); break;
            }
            Held = ItemKind.None; Status = "";
            string line = $"{Items.Name(k)} as {v.Mode}: {r}"; Used.Add($"{k} {v.Mode} {r}");
            Items.Note(v, "uses " + line);
            Event(k.ToString(), target);
            return true;
        }
        // the item's use, for Barnaby, the drivers, the sounds (Caller / GameSounds watch it)
        public static event System.Action<ShiftVehicle, string, ShiftVehicle> Fired;
        void Event(string what, ShiftVehicle at) => Fired?.Invoke(v, what, at);
    }
}
