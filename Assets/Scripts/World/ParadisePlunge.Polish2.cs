using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace ShiftStorm
{
    // Round 7 item 4: the rest of the lap dressed to the Round 5 standard (Tee'd Off's Jurassic Par look: cycads, tree ferns, mossy
    // boulders and cliffs, swamp, hot springs, waterfalls, dinosaurs and pterodactyls as scenery, toon water and foam; built here from
    // the same shapes as ParadisePlunge.Polish). Looks only: no colliders anywhere near the route; the Detail layer culls the small
    // pieces beyond 800 m. Readability first: nothing on the road or in the loop / jump / landing space, nothing in the flight
    // corridor, the cave's guide lights and gate frames stay the brightest things in the water.
    //   Beach Promenade: a sand beach on the lake side with boulders and driftwood, a jungle wall of cycads and tree ferns inland,
    //     grandstands with a crowd and pennant flags at the start / finish, a brontosaurus wading in the lake, pterodactyls over the loop,
    //     a smoking volcano far off, hot springs by the creeks, Barnaby's booth at the start line
    //   Mangrove Run: lily pads, reeds, fallen logs, hanging vines on bigger tree ferns, a triceratops at the water's edge
    //   Sunken Gateway: two giant carved stone heads and glyph pillars either side of the portal, bubbles rising from the opening
    //   Coral Vault: coral clusters (pink / orange / violet) on the floor and walls, light shafts down from cracks in the roof, more
    //     bubble columns
    //   Waterfall Return: ferns and boulders on the falls' cliffs, mist at the curtain's foot, a rainbow over the pool, a foam ring
    //   finish: the grandstands and flags (shared with the start), chequered flags either side of the line
    public static partial class ParadisePlunge
    {
        static readonly Color Coral1 = new Color(1f, .5f, .58f), Coral2 = new Color(1f, .62f, .25f), Coral3 = new Color(.66f, .42f, .95f), Lily = new Color(.3f, .62f, .26f), Reed = new Color(.55f, .62f, .3f);

        static void Polish2()
        {
            var rnd = new System.Random(707);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var rock = new CellBatch(); var green = new CellBatch(); var bright = new CellBatch();

            // ---------------- Beach Promenade ----------------
            float pa = M("G0") + 10, pb = M("climb0") - 10;
            for (float p = pa; p < pb; p += 14f)
            {
                if (InGap(p) || InGap(p + 8) || InGap(p - 8) || (p > M("loopIn") - 40 && p < M("loopOut") + 30)) continue;
                var q = At(p); var r = Right(p); float hw = HalfWidth(p);
                // the lake side (left): wet sand at the water, boulders and driftwood
                var beach = new Vector3(q.x, -.1f, q.z) - r * (hw + 16f + R(0, 4));
                rock.Add(MeshKit.LowSphere, beach, Facing(p), new Vector3(R(10, 14), 1.2f, R(16, 20)), rnd.NextDouble() < .5 ? JurassicLook.Sand : new Color(.74f, .66f, .48f));
                if (rnd.NextDouble() < .35) Boulder(rock, new Vector3(q.x, .8f, q.z) - r * (hw + R(9, 13)), R(.5f, 1f), R(0, 360));
                if (rnd.NextDouble() < .2) rock.Add(MeshKit.LowCylinder, new Vector3(q.x, 1.3f, q.z) - r * (hw + R(6, 9)), Quaternion.Euler(R(80, 95), R(0, 180), 0), new Vector3(.5f, R(2.5f, 4f), .5f), new Color(.72f, .62f, .48f));
                // the inland side (right): a jungle wall, tall tree ferns behind lower cycads
                if (rnd.NextDouble() < .7) Fern(green, new Vector3(q.x, GTop, q.z) + r * (hw + R(22, 34)), R(1.3f, 1.9f), R(0, 360));
                if (rnd.NextDouble() < .5) Cycad(green, new Vector3(q.x, GTop, q.z) + r * (hw + R(12, 18)), R(1f, 1.4f), R(0, 360));
            }
            // pennant flags along the promenade's barrier posts every 40 m (orange / green / cream), high enough to read past the cars
            var flags = new MeshKit.Batch(); var fc = new[] { new Color(1f, .55f, .12f), new Color(.35f, .75f, .3f), new Color(.98f, .92f, .75f) }; int fi = 0;
            for (float p = 60; p < M("climb0") - 20; p += 40)
            {
                if (OffSurface(p) || NearLip(p) || (p > M("loopIn") - 60 && p < M("loopOut") + 30)) continue;
                var q = At(p); var r = Right(p);
                foreach (float s in new[] { -1f, 1f })
                {
                    var at = new Vector3(q.x, RoadTop(p), q.z) + r * s * (HalfWidth(p) + 2.4f);
                    flags.Add(MeshKit.LowCylinder, at + Vector3.up * 3f, Quaternion.identity, new Vector3(.18f, 3f, .18f), JurassicLook.FernTrunk);
                    flags.Add(MeshKit.LowCone, at + Vector3.up * 5.3f + Flat(Dir(p)) * .7f, Facing(p) * Quaternion.Euler(90, 0, 0), new Vector3(.12f, 1.5f, .9f), fc[fi++ % 3]);
                }
            }
            flags.Object(scen, "Promenade pennants", JurassicLook.VertexColored(true), false, false).layer = DetailLayer;
            // Round 9 item 3b: the grandstands with Gridiron Glory's crowd (ParadisePlunge.Crowd.cs) replace build 10's ball-and-head figures;
            // the random numbers those used are still drawn, so the rest of the scenery stays where it was
            for (int k = 0; k < 2 * 4 * 26 * 4; k++) rnd.NextDouble();
            Grandstands();
            // chequered flags either side of the line, on tall poles
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? -1 : 1; var at = new Vector3(s * 17.5f, RoadY, 4f);
                var fl = new MeshKit.Batch(); fl.Add(MeshKit.LowCylinder, at + Vector3.up * 6f, Quaternion.identity, new Vector3(.25f, 6f, .25f), new Color(.85f, .85f, .85f));
                for (int x = 0; x < 4; x++) for (int y = 0; y < 3; y++) fl.Add(MeshKit.Cube, at + new Vector3(-s * (.5f + x * .8f), 11.4f - y * .8f, 0), Quaternion.identity, new Vector3(.8f, .8f, .05f), (x + y) % 2 == 0 ? Color.white : new Color(.08f, .08f, .08f));
                fl.Object(scen, "Chequered flag", JurassicLook.VertexColored(false), false, false);
            }
            // the lake: a brontosaurus wading, two more pterodactyls circling the loop, the volcano far over the water with its smoke
            Dino("bronto", new Vector3(-260, -3f, 420), 110, 20f);
            Dino("bronto", new Vector3(-330, -3f, 760), 60, 16f);
            for (int i = 0; i < 2; i++)
            {
                var pg = Dino("ptero", LoopE + Vector3.up * 70, 0, 3.2f);
                if (pg) { var cir = pg.AddComponent<Circle>(); cir.center = LoopE + Vector3.up * (68 + i * 10) + LoopRt * 8; cir.radius = 55 + i * 20; cir.speed = .25f + i * .05f; cir.phase = i * 3f; }
            }
            {
                var vb = new MeshKit.Batch(); var vat = new Vector3(-900, -2, 650);
                vb.Add(MeshKit.LowCone, vat + Vector3.up * 70, Quaternion.identity, new Vector3(260, 140, 260), JurassicLook.RockDark);
                vb.Add(MeshKit.LowCone, vat + Vector3.up * 112, Quaternion.identity, new Vector3(120, 56, 120), JurassicLook.Rock);
                vb.Add(MeshKit.LowCylinder, vat + Vector3.up * 141, Quaternion.identity, new Vector3(46, 2, 46), new Color(1f, .45f, .1f));
                for (int k = 0; k < 6; k++) vb.Add(MeshKit.LowSphere, vat + new Vector3(R(-12, 12) + k * 9, 150 + k * 16, R(-8, 8)), Quaternion.identity, Vector3.one * (24 + k * 9), new Color(.72f, .7f, .68f));
                vb.Object(scen, "Volcano", JurassicLook.VertexColored(true, new Color(.04f, .02f, .01f)), false, false);
            }
            // hot springs by the creeks (warm pools with steam puffs: looks only)
            foreach (var j in Jumps)
            {
                if (!j.water) continue; var q = At(j.lip + j.gap * .5f); var r = Right(j.lip);
                foreach (float s in new[] { 1f })
                {
                    var at = new Vector3(q.x, .05f, q.z) + r * s * 42f;
                    bright.Add(MeshKit.LowCylinder, at, Quaternion.identity, new Vector3(9, .1f, 7), JurassicLook.Spring);
                    for (int k = 0; k < 6; k++) Boulder(rock, at + new Vector3(Mathf.Cos(k) * 6f, .4f, Mathf.Sin(k) * 5f), .35f, k * 60);
                    var steam = new GameObject("spring steam"); steam.transform.SetParent(scen, false); steam.transform.position = at; steam.AddComponent<Bubbles>().top = 9f;
                }
            }

            // ---------------- Mangrove Run ----------------
            for (float p = M("lagoonEnd"); p < M("diveZone") - 20; p += 11f)
            {
                var q = At(p); var r = Right(p);
                foreach (float s in new[] { -1f, 1f })
                {
                    // lily pads and reeds close to the banks (outside the 15 m buoy lane)
                    if (rnd.NextDouble() < .7) { var lp = new Vector3(q.x, Water + .03f, q.z) + r * s * R(16.5f, 19.5f); green.Add(MeshKit.LowCylinder, lp, Quaternion.Euler(0, R(0, 360), 0), new Vector3(R(1.4f, 2.4f), .03f, R(1.4f, 2.4f)), Lily); if (rnd.NextDouble() < .25) green.Add(MeshKit.LowSphere, lp + Vector3.up * .25f, Quaternion.identity, Vector3.one * .45f, new Color(1f, .7f, .85f)); }
                    if (rnd.NextDouble() < .5) for (int k = 0; k < 4; k++) green.Add(MeshKit.LowCone, new Vector3(q.x, .9f, q.z) + r * s * (20.5f + R(0, 1.5f)) + Flat(Dir(p)) * R(-3, 3), Quaternion.Euler(R(-8, 8), 0, R(-8, 8)), new Vector3(.18f, R(1.6f, 2.6f), .18f), Reed);
                    // bigger tree ferns with hanging vines over the water's edge, and fallen logs on the banks
                    if (rnd.NextDouble() < .3)
                    {
                        var at = new Vector3(q.x, 1.4f, q.z) + r * s * R(26, 32); Fern(green, at, R(1.6f, 2.2f), R(0, 360));
                        for (int k = 0; k < 3; k++) green.Add(MeshKit.LowCylinder, at + Vector3.up * R(5, 7) - r * s * R(2, 4) + Flat(Dir(p)) * (k - 1) * 1.5f, Quaternion.identity, new Vector3(.12f, R(2, 3.5f), .12f), JurassicLook.Moss);
                    }
                    if (rnd.NextDouble() < .12) rock.Add(MeshKit.LowCylinder, new Vector3(q.x, 1.6f, q.z) + r * s * R(24, 30), Quaternion.Euler(90, R(0, 180), 0), new Vector3(.9f, R(3, 5), .9f), JurassicLook.FernTrunk);
                }
            }
            Dino("trike", At(M("mangMid")) + Right(M("mangMid")) * 36f + Vector3.up * .8f, 200, 6.5f);

            // ---------------- Sunken Gateway: giant stone heads and glyph pillars ----------------
            {
                float pp = M("portal") - 6; var at = At(pp); at.y = 0; var f = Facing(pp); var r = Right(pp);
                foreach (float s in new[] { -1f, 1f })
                {
                    var hp = at + r * s * (CaveHalf + 14f) + f * Vector3.forward * -4f;
                    rock.Add(MeshKit.Cube, hp + Vector3.up * 6f, f, new Vector3(9, 12, 8), JurassicLook.Stone);                    // the head
                    rock.Add(MeshKit.Cube, hp + Vector3.up * 13f, f, new Vector3(10, 2.4f, 9), JurassicLook.RockDark);             // its brow
                    rock.Add(MeshKit.Cube, hp + Vector3.up * 7.5f - f * Vector3.forward * 4.1f, f, new Vector3(2f, 3.5f, .6f), JurassicLook.RockDark);   // the nose
                    foreach (float e in new[] { -1f, 1f }) bright.Add(MeshKit.Cube, hp + Vector3.up * 10f + r * e * 2.2f - f * Vector3.forward * 4.05f, f, new Vector3(1.4f, .9f, .2f), JurassicLook.Crystal);   // glowing eyes
                    rock.Add(MeshKit.LowSphere, hp + Vector3.up * 14.6f, f, new Vector3(11, 2, 10), JurassicLook.Moss);
                    for (int k = 0; k < 2; k++) { var pl = at + r * s * (CaveHalf + 28f + k * 9) + f * Vector3.forward * (6f + k * 7); rock.Add(MeshKit.Cube, pl + Vector3.up * 5f, f * Quaternion.Euler(0, k * 12, 0), new Vector3(2.6f, 10, 2.6f), JurassicLook.Stone); bright.Add(MeshKit.Cube, pl + Vector3.up * 7f - f * Vector3.forward * 1.35f, f, new Vector3(1.2f, 1.2f, .1f), JurassicLook.Amber); }
                }
                var bub = new GameObject("portal bubbles"); bub.transform.SetParent(scen, false); bub.transform.position = At(M("portal") + 4) + Vector3.down * 3; bub.AddComponent<Bubbles>().top = Water - .2f;
            }

            // ---------------- Coral Vault: coral, light shafts, bubble columns ----------------
            for (float p = M("portal") + 14; p < M("poolEdge") - 6; p += 9f)
            {
                var q = At(p); var r = Right(p);
                foreach (float s in new[] { -1f, 1f })
                {
                    if (rnd.NextDouble() > .55) continue;
                    var at = new Vector3(q.x, CaveFloor + .3f, q.z) + r * s * R(CaveHalf - 5.5f, CaveHalf - 1.5f);
                    var col = rnd.NextDouble() < .34 ? Coral1 : rnd.NextDouble() < .5 ? Coral2 : Coral3;
                    for (int k = 0; k < 4; k++) bright.Add(MeshKit.LowCone, at + new Vector3(R(-1, 1), R(.6f, 1.6f), R(-1, 1)), Quaternion.Euler(R(-20, 20), R(0, 360), R(-20, 20)), new Vector3(R(.4f, .8f), R(1.4f, 3f), R(.4f, .8f)), col);
                    bright.Add(MeshKit.LowSphere, at + Vector3.up * .3f, Quaternion.identity, new Vector3(R(1.4f, 2.4f), .8f, R(1.4f, 2.4f)), col * .85f);
                }
            }
            var shafts = new MeshKit.Batch();
            for (float p = M("portal") + 30; p < M("G7"); p += 34f)
            {
                var q = At(p); var r = Right(p); var top = new Vector3(q.x, CaveRoof - .2f, q.z) + r * R(-9, 9);
                shafts.Add(MeshKit.Cube, top + Vector3.down * 7.5f + Flat(Dir(p)) * 2f, Quaternion.Euler(R(-12, -6), R(0, 360), 0), new Vector3(R(2.4f, 3.6f), 15f, .2f), new Color(.75f, 1f, .95f, .16f));
                bright.Add(MeshKit.LowSphere, top + Vector3.up * .1f, Quaternion.identity, new Vector3(3f, .3f, 1.2f), new Color(.85f, 1f, .95f));   // the crack it falls from
            }
            shafts.Object(scen, "Vault light shafts", GateMarker.Beacon, false, false);
            foreach (var n in new[] { "hi1", "hi2", "G7" })
            {
                float p = M(n) - 20; var q = At(p); var at = new Vector3(q.x, CaveFloor + .3f, q.z) - Right(p) * 9f;
                var bub = new GameObject("vault bubbles"); bub.transform.SetParent(scen, false); bub.transform.position = at; bub.AddComponent<Bubbles>().top = CaveRoof - .5f;
            }

            // ---------------- Waterfall Return: ferns and boulders on the cliffs, mist, a rainbow, a foam ring ----------------
            foreach (float s in new[] { -1f, 1f })
            {
                for (int k = 0; k < 6; k++)
                {
                    var at = W(s * 31, 0, -222 + k * 7) + Vector3.up * (26.5f + R(-.3f, .3f)) + Vector3.right * s * R(-4, 4);
                    if (k % 2 == 0) Fern(green, at, R(.9f, 1.3f), R(0, 360)); else Cycad(green, at, R(.8f, 1.1f), R(0, 360));
                }
                for (int k = 0; k < 4; k++) Boulder(rock, W(s * 23.5f, 0, -235 + k * 9) + Vector3.up * .4f, R(.6f, 1f), R(0, 360));
            }
            {
                var mist = new MeshKit.Batch(); var foot = W(0, 0, -187);
                for (int k = 0; k < 14; k++) mist.Add(MeshKit.LowSphere, foot + new Vector3(R(-30, 30), R(.4f, 2.8f), R(-6, 4)), Quaternion.identity, new Vector3(R(5, 9), R(2, 3.5f), R(4, 6)), new Color(.95f, .98f, 1f, .35f));
                mist.Object(scen, "Falls mist", GateMarker.Beacon, false, false);
                var rainbow = new MeshKit.Batch(); var rc = new[] { new Color(1f, .3f, .3f, .3f), new Color(1f, .7f, .25f, .3f), new Color(1f, 1f, .4f, .3f), new Color(.4f, 1f, .45f, .3f), new Color(.4f, .65f, 1f, .3f), new Color(.65f, .4f, 1f, .3f) };
                var c0 = W(0, 0, -232) + Vector3.right * 0 + Vector3.up * 2f;
                for (int band = 0; band < rc.Length; band++)
                    for (int i = 0; i <= 20; i++)
                    {
                        float a = Mathf.PI * i / 20f, rad = 34f - band * 1.1f; var at = c0 + new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad * .8f, 0) + Vector3.back * 12f;
                        rainbow.Add(MeshKit.Cube, at, Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90), new Vector3(1.1f, 5.6f, .1f), rc[band]);
                    }
                rainbow.Object(scen, "Falls rainbow", GateMarker.Beacon, false, false);
                var foamC = new MeshKit.Batch();
                for (int i = 0; i < 18; i++) { float a = i * Mathf.PI * 2 / 18; foamC.Add(MeshKit.LowCylinder, W(0, 0, -246) + new Vector3(Mathf.Cos(a) * 26f, Water + .05f, Mathf.Sin(a) * 40f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0), new Vector3(R(2, 4), .03f, R(1, 1.5f)), Color.white); }
                foamC.Object(scen, "Pool foam", JurassicLook.VertexColored(false, new Color(.2f, .22f, .22f)), false, false);
            }

            rock.Objects(scen, "Polished rock (R7)", JurassicLook.VertexColored(true), false);
            green.Objects(scen, "Polished plants (R7)", JurassicLook.VertexColored(true), false);
            bright.Objects(scen, "Polished glow (R7)", JurassicLook.VertexColored(true, new Color(.18f, .16f, .14f)), false);
            Booth();
        }

        // Barnaby's booth at the start line (west side, behind the barrier): a striped podium, his name board, Barnaby himself
        // (Tee'd Off's model, copied to Resources/Drivers/Barnaby, in his "Standing Arguing" loop: a showman waving his arms)
        static void Booth()
        {
            var at = new Vector3(-18.5f, RoadY, -14f); var face = Quaternion.LookRotation(Vector3.right + Vector3.forward * .3f);
            var b = new MeshKit.Batch();
            b.Add(MeshKit.Cube, at + Vector3.up * .9f, face, new Vector3(4.4f, 1.8f, 3.4f), new Color(.85f, .25f, .2f));
            for (int i = 0; i < 5; i++) b.Add(MeshKit.Cube, at + Vector3.up * .9f + face * new Vector3(-1.8f + i * .9f, 0, -1.72f), face, new Vector3(.42f, 1.82f, .05f), new Color(.98f, .92f, .78f));
            b.Add(MeshKit.Cube, at + Vector3.up * 5.4f, face, new Vector3(5f, .3f, 4f), new Color(.98f, .92f, .78f));
            foreach (float x in new[] { -2.2f, 2.2f }) b.Add(MeshKit.LowCylinder, at + face * new Vector3(x, 0, 1.5f) + Vector3.up * 2.7f, Quaternion.identity, new Vector3(.2f, 2.7f, .2f), JurassicLook.FernTrunk);
            b.Object(scen, "Barnaby booth", JurassicLook.VertexColored(true), false, true);
            // his name board (the name players see: Barnaby), amber with dark letters, both faces
            var board = Box(scen, "Barnaby board", at + Vector3.up * 6.7f, new Vector3(5.6f, 1.4f, .2f), Mt(JurassicLook.Amber, true), face, false);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (float side in new[] { -1f, 1f })
            {
                var tg = new GameObject("Barnaby name"); tg.transform.SetParent(scen, false);
                tg.transform.SetPositionAndRotation(board.transform.position + face * Vector3.forward * (side * .12f), face * Quaternion.Euler(0, side < 0 ? 0 : 180, 0));
                var tm = tg.AddComponent<TextMesh>(); tm.font = font; tm.text = Caller.Name; tm.fontSize = 64; tm.characterSize = .07f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = new Color(.18f, .1f, .04f);
                tg.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
            var prefab = Resources.Load<GameObject>("Drivers/Barnaby/Barnaby");
            if (!prefab) { Debug.Log("[Course] Barnaby's model is missing (Resources/Drivers/Barnaby): the booth stands empty"); return; }
            var m = Object.Instantiate(prefab, scen, false); m.name = "Barnaby"; foreach (var c in m.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            var rend = m.GetComponentsInChildren<Renderer>(true); var bb = new Bounds(m.transform.position, Vector3.zero); foreach (var r in rend) bb.Encapsulate(r.bounds);
            float k = 1.85f / Mathf.Max(.1f, bb.size.y); m.transform.localScale = Vector3.one * k;
            m.transform.SetPositionAndRotation(at + Vector3.up * 1.8f, face);
            var tex = Resources.Load<Texture2D>("Drivers/Barnaby/Barnaby_basecolor"); if (tex) { var mat = JurassicLook.Textured(tex, true); foreach (var r in rend) r.sharedMaterial = mat; }
            var anim = m.GetComponentInChildren<Animator>(); var clip = Resources.Load<AnimationClip>("Drivers/Barnaby/Standing Arguing");
            if (anim && clip) m.AddComponent<LoopClip>().Play(anim, clip);
            Debug.Log($"[Course] Barnaby in his booth ({(anim && clip ? "animated" : "standing")})");
        }
    }

    // looks only: a humanoid clip looped on an Animator through a playable graph (no controller asset needed)
    public sealed class LoopClip : MonoBehaviour
    {
        UnityEngine.Playables.PlayableGraph graph; UnityEngine.Animations.AnimationClipPlayable play; float len;
        public void Play(Animator a, AnimationClip c)
        {
            graph = UnityEngine.Playables.PlayableGraph.Create("Barnaby"); var o = UnityEngine.Animations.AnimationPlayableOutput.Create(graph, "anim", a);
            play = UnityEngine.Animations.AnimationClipPlayable.Create(graph, c); o.SetSourcePlayable(play); graph.Play(); len = c.length;
        }
        void Update() { if (graph.IsValid() && len > 0 && play.GetTime() > len) play.SetTime(play.GetTime() % len); }
        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
    }
}
