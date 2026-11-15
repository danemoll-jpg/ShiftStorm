using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 11 item 4: choose your racer. Four drivers (Abi, Rae, Chip, Vivi), each with a machine of their own (Abi: Jetstream, the default
    // design; Rae: Grand Prix, Chip: Scrambler, Vivi: Hot Rod: designs.json "rival"). Player 1 picks a driver and a machine (Settings.Driver,
    // Settings.Design), player 2 in split screen too (Settings.Driver2, Settings.Design2); the drivers nobody picked are the AI rivals, each
    // in their own machine (recoloured to their colour only when a player races the same machine: FormSet.TextureTint).
    // The vehicles are made once at start-up (Game: the player's, three rivals'); a pick re-seats a driver (Driver.Swap) and re-styles a
    // machine (FormSet) in place, so every system holding a vehicle keeps it.
    public static class Lineup
    {
        public static readonly Rival Abi = new Rival { name = "Abi", character = "Abigail", color = new Color(.95f, .45f, .1f), lane = -1f, brakeMargin = .9f, boostMeter = .55f, boostStraight = 10f, shortcut = "Coral Vault tunnel|Lighthouse rocks", shortcutRate = .35f, seed = 5 };
        public static readonly Rival[] All = { Abi, Rival.Roster[0], Rival.Roster[1], Rival.Roster[2] };
        public static string[] Names => new[] { "Abi", "Rocket Rae", "Chip \"Showtime\" Sterling", "Vivi" };
        public static string Short(int i) => i == 0 ? "Abi" : i == 1 ? "Rae" : i == 2 ? "Chip" : "Vivi";
        public static int IndexOf(string character) { for (int i = 0; i < All.Length; i++) if (All[i].character == character) return i; return 0; }

        // a driver's own machine
        public static string MachineOf(Rival r)
        {
            if (r == Abi) return VehicleDesigns.Default;
            var d = VehicleDesigns.ForRival(r.character); return d != null ? d.name : VehicleDesigns.Default;
        }
        // the machines a player may pick: Dan's imported designs (not the gray box, not hidden test designs)
        public static List<string> Machines { get { var l = new List<string>(); foreach (var n in VehicleDesigns.Names) if (n != VehicleDesigns.GrayBox) l.Add(n); if (l.Count == 0) l.Add(VehicleDesigns.GrayBox); return l; } }
        public static string NextMachine(string m) { var l = Machines; int i = l.IndexOf(m); return l[(i + 1) % l.Count]; }

        public static Rival P1 => All[Settings.Driver];
        // player 2: never the same driver as player 1 (the next one along)
        public static int P2Index { get { int i = Settings.Driver2; if (i == Settings.Driver) i = (i + 1) % All.Length; return i; } }
        public static Rival P2 => All[P2Index];
        public static string P2Machine => string.IsNullOrEmpty(Settings.Design2) ? MachineOf(P2) : Settings.Design2;
        public static void NextP2() { int i = P2Index; do i = (i + 1) % All.Length; while (i == Settings.Driver); Settings.Driver2 = i; }

        // the AI drivers: everyone no player picked, in roster order
        public static List<Rival> AiRivals(bool split)
        {
            var l = new List<Rival>(); foreach (var r in All) if (r != P1 && !(split && r == P2)) l.Add(r); return l;
        }

        public static string Applied { get; private set; } = "";
        // apply the picks to the vehicles (before a course starts): player 1's driver and machine, and the three rival vehicles as the
        // three drivers player 1 did not pick (in split screen Game.ActiveRivals leaves out the one player 2 drives)
        public static void Apply(Game g)
        {
            var p1 = P1;
            var d = Driver.Swap(g.Player, p1.character, true);
            g.Player.RacerName = p1.name; g.Player.Tint = p1.color;
            g.Player.Forms.Restyle();
            var ai = AiRivals(false);
            for (int k = 0; k < g.Rivals.Count && k < ai.Count; k++)
            {
                var rv = g.Rivals[k]; var r = ai[k];
                rv.RacerName = r.name; rv.Tint = r.color;
                var pilot = rv.GetComponent<RivalPilot>(); if (pilot) pilot.rival = r;
                rv.Forms.SetBody(r.color); rv.Forms.SetOwnDesign(MachineOf(r));
                Driver.Swap(rv, r.character, false);
            }
            Applied = $"P1 {p1.name} in {VehicleDesigns.Title(Settings.Design)}; AI " + string.Join(", ", ai.ConvertAll(r => $"{r.name} in {VehicleDesigns.Title(MachineOf(r))}"));
            Debug.Log("[Lineup] " + Applied);
        }
    }
}
