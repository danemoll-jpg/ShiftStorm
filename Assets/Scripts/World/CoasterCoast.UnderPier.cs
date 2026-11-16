using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 13 item 5c (Dan: "it's too easy to get stuck under the boardwalk / pier and not know where to go"). His playtest logs are on the Deck,
    // so the places were found with the "underpier" check's sweep (a submarine / boat started off the route in every direction under the
    // decks, steering by the HUD only). Build 14: the plazas either side of the boardwalk are decks on legs with open water under them and
    // the legs had no colliders, so a boat leaving the flume's channel under the boardwalk (between the two plazas), or the sea route beside
    // the south plaza, drifted in under a 250 m wide deck 2.4 m over the water: dark, every way looks the same, nothing to steer by.
    // Now:
    //   - a plank skirt round every plaza edge from the deck down to 3.5 m under the water (a hull glances off it; nothing goes under a deck)
    //   - the flume's channel under the boardwalk lit both sides (lanterns on the skirts), the dive route lit both sides at the line's height
    //     from the dive zone to the splash pool (a lit channel between the fishing pier's legs) and a glowing seabed stripe under the line
    //   - the exit: a glowing frame under the splash pool's curtain (where you come up) and a light column, the sea a little clearer here
    //     (underwater fog density .022 -> .016: about 100 m of sight instead of 73)
    //   - soft walls: currents either side of the dive route (from 16 m off the line) that push back toward it
    //   - lost there (Course.lostRanges): no progress for 6 s (build 14: 15 s) recovers FORWARD to the next anchor on the route that is not
    //     past an unearned gate (Round 12's rule: never back, never a loop), "BACK ON COURSE", no time added
    // "-oldPier": build 14's (before pictures and sweep).
    public static partial class CoasterCoast
    {
        public static bool OldPier => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldPier") >= 0;
        public const float ClearUnderwaterDensity = .016f;

        static void UnderPier()
        {
            if (OldPier) return;
            var plank = CoasterLook.Mat(CoasterLook.PlankDark); var glowA = CoasterLook.Glow(CoasterLook.Lemon);
            var lan = new MeshKit.Batch();
            // the skirts: (x0, z0) -> (x1, z1) along a plaza edge, from just under the deck (y 2.4) to 3.5 m under the water
            void Skirt(float x0, float z0, float x1, float z1, bool lanterns)
            {
                var a = L(x0, 0, z0); var b = L(x1, 0, z1); var d = b - a; var f = Quaternion.LookRotation(d.normalized, Vector3.up);
                float top = DeckY - 1.6f, bottom = -3.5f;
                Box(root, "Plaza skirt", (a + b) * .5f + Vector3.up * ((top + bottom) * .5f), new Vector3(.8f, top - bottom, d.magnitude + .8f), plank, f, true, ShiftVehicle.ObstacleLayer);
                if (lanterns) { int n = Mathf.CeilToInt(d.magnitude / 12f); for (int i = 0; i <= n; i++) lan.Add(MeshKit.Cube, Vector3.Lerp(a, b, i / (float)n) + Vector3.up * 1.2f, f, new Vector3(1f, .5f, 1.6f), i % 2 == 0 ? CoasterLook.Lemon : CoasterLook.Neon); }
            }
            // south plaza (x -150..100, z -362..233): west, north (the flume channel's south side), east, south
            Skirt(-150, -362, -150, 233, false); Skirt(-150, 233, 100, 233, true); Skirt(100, 233, 100, -362, false); Skirt(100, -362, -150, -362, false);
            // north plaza (x -150..100, z 271..840): south (the channel's north side), west, east
            Skirt(-150, 271, 100, 271, true); Skirt(-150, 271, -150, 840, false); Skirt(100, 271, 100, 840, false);
            lan.Object(scen, "Channel lanterns (under the boardwalk)", GateMarker.Beacon, false, false);

            // the dive route's lit channel: lights both sides at the line's height, every 12 m, and an amber stripe on the seabed under it
            float a0 = M("diveZone") + 10, a1 = M("poolEdge") - 4; var lights = new MeshKit.Batch(); var stripe = new MeshKit.Batch(); int k = 0;
            for (float p = a0; p < a1; p += 12, k++)
            {
                var q = At(p); var r = Right(p); var fq = Facing(p);
                foreach (float s in new[] { -1f, 1f }) lights.Add(MeshKit.Cube, q + r * s * 10f, fq, Vector3.one * 1.1f, k % 2 == 0 ? new Color(.35f, 1f, .9f) : new Color(1f, .78f, .35f));
                var fl = new Vector3(q.x, SeaFloor + .15f, q.z); stripe.Add(MeshKit.Cube, fl, fq, new Vector3(2.2f, .2f, 7f), new Color(1f, .62f, .15f));
            }
            lights.Object(scen, "Dive route guide lights", GateMarker.Beacon, false, false);
            stripe.Object(scen, "Dive route seabed stripe", GateMarker.Beacon, false, false);

            // the exit: a glowing frame under the splash pool's curtain (the sub comes up through it) and a light column over the pool
            const float zs = -497, hx = 24;
            var exit = new MeshKit.Batch(); var gold = new Color(1f, .85f, .35f);
            exit.Add(MeshKit.Cube, L(0, -4.9f, zs - 1.8f), Quaternion.identity, new Vector3(hx * 2, .7f, .7f), gold);
            foreach (float s in new[] { -1f, 1f }) exit.Add(MeshKit.Cube, L(s * (hx - 1), -11f, zs - 1.8f), Quaternion.identity, new Vector3(.7f, 12.5f, .7f), gold);
            exit.Add(MeshKit.Cube, L(0, -17f, zs - 1.8f), Quaternion.identity, new Vector3(hx * 2, .7f, .7f), gold);
            exit.Object(scen, "Splash pool exit frame", GateMarker.Beacon, false, false);
            var col = new GameObject("Splash pool light column"); col.transform.SetParent(scen, false); col.transform.position = L(0, 14, -460);
            foreach (float yaw in new[] { 0f, 90f })
            {
                var qd = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(qd.GetComponent<Collider>()); qd.transform.SetParent(col.transform, false);
                qd.transform.localRotation = Quaternion.Euler(0, yaw, 0); qd.transform.localScale = new Vector3(10, 40, 1);
                var mr = qd.GetComponent<MeshRenderer>(); mr.sharedMaterial = JurassicLook.FallMat(.22f); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // soft walls: currents either side of the dive route that push a sub back toward the line (from 16 m out to 46 m)
            for (float p = M("diveZone") + 30; p < M("poolEdge") - 20; p += 20)
            {
                var q = At(p); q.y = Mathf.Min(q.y, -3f) - 3f; var r = Right(p); var fq = Facing(p);
                foreach (float s in new[] { -1f, 1f }) { var cz = Vol<CurrentZone>("Dive route side current", q + r * s * 31f, fq, new Vector3(30, 22, 22)); cz.push = -r * s * 7f; }
            }

            // lost under there: forward recovery after 6 s with no progress (the Lighthouse Sea's end, the dive, the Sunken Carnival)
            c.lostRanges.Add(new Vector2(M("seaOut") - 160, M("seaOut") + 60));
            c.lostRanges.Add(new Vector2(M("diveZone") - 30, M("poolEdge") + 10));
        }
    }
}
