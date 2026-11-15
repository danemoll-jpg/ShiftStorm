using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 9 item 3b: the grandstands, filled with Gridiron Glory's crowd (World/Crowd.cs). Build 10 had two stands of 104 ball-and-head
    // figures each at the start (Dan: "I didn't even notice a crowd"). Now, close to the road and cartoon-big so they are noticed as you pass
    // (same idea as the dinosaurs):
    //   start / finish   both sides, 60 m long, 6 rows from 9 m past the road's edge (behind the barrier and Barker's booth): they roar at GO
    //                    and cheer each racer home (the finish's own crowd sound stays the one sound there)
    //   the loop         on the beach side, facing the loop, 64 m long, 6 rows: they rise and cheer as each racer goes round
    //   waterfall landing on the inland side of the finishing road just past the landing, 56 m, 5 rows: a cheer you hear as you land
    // Stepped stone / timber stands with a striped awning; the fans are on the steps' tops (no colliders: looks only).
    public static partial class ParadisePlunge
    {
        static void Grandstands()
        {
            // start / finish: both sides of the grid and the line (the old ones' place)
            foreach (float s in new[] { -1f, 1f })
                Grandstand(s < 0 ? "Start W" : "Start E", new Vector3(s * 22f, RoadY, -18f), Vector3.forward, Vector3.right * s, 60f, 6, false, true, s < 0 ? 11 : 12);
            // the loop: beach side (west, -LoopRt), clear of the loop's track (its exit is shifted LoopShift to the east)
            {
                var front = LoopE - LoopRt * (LoopTrackHalf + 15f); front.y = RoadY;
                Grandstand("Loop", front, LoopF, -LoopRt, 64f, 6, false, false, 13);
            }
            // the waterfall landing: inland of the finishing road, 40 m past the landing point
            {
                float p = M("landing") + 45f; var q = At(p); var r = Right(p); var f = Flat(Dir(p));
                var front = new Vector3(q.x, RoadTop(p), q.z) + r * (HalfWidth(p) + 9f);
                Grandstand("Waterfall landing", front, f, r, 56f, 5, true, false, 14);
            }
        }

        static void Grandstand(string name, Vector3 front, Vector3 along, Vector3 back, float length, int rows, bool passSound, bool goSound, int seed)
        {
            along = new Vector3(along.x, 0, along.z).normalized; back = new Vector3(back.x, 0, back.z).normalized;
            var face = Quaternion.LookRotation(-back);   // the stand faces the road
            var rnd = new System.Random(seed * 97 + 5);
            var b = new MeshKit.Batch(); var seats = new List<Vector3>();
            const float depth = 2.2f, rise = 1.2f, spacing = 1.15f;
            for (int row = 0; row < rows; row++)
            {
                float h = (row + 1) * rise, d = row * depth + depth * .5f;
                b.Add(MeshKit.Cube, front + back * d + Vector3.up * (h * .5f), face, new Vector3(length, h, depth), row % 2 == 0 ? JurassicLook.Stone : new Color(.62f, .5f, .36f));
                b.Add(MeshKit.Cube, front + back * (d - depth * .5f + .06f) + Vector3.up * (h + .04f), face, new Vector3(length, .08f, .12f), new Color(.98f, .92f, .78f));   // a cream step edge
                for (float x = -length * .5f + .8f; x <= length * .5f - .8f; x += spacing)
                    seats.Add(front + back * (d + .15f) + along * (x + (float)(rnd.NextDouble() - .5) * .3f) + Vector3.up * h);
            }
            // back wall, posts and a striped awning (orange / cream) over the top rows
            float top = rows * rise, deep = rows * depth;
            b.Add(MeshKit.Cube, front + back * (deep + .25f) + Vector3.up * (top + 2.2f) * .5f, face, new Vector3(length, top + 2.2f, .5f), JurassicLook.Stone);
            for (float x = -length * .5f; x <= length * .5f + .01f; x += length / 6f)
                b.Add(MeshKit.LowCylinder, front + back * (deep * .45f) + along * x + Vector3.up * (top + 4.5f) * .5f, Quaternion.identity, new Vector3(.3f, (top + 4.5f) * .5f, .3f), JurassicLook.FernTrunk);
            int stripes = Mathf.RoundToInt(length / 4f);
            for (int i = 0; i < stripes; i++)
                b.Add(MeshKit.Cube, front + back * (deep * .55f) + along * (-length * .5f + (i + .5f) * length / stripes) + Vector3.up * (top + 4.6f), face * Quaternion.Euler(-12, 0, 0), new Vector3(length / stripes, .25f, deep * 1.05f), i % 2 == 0 ? new Color(.95f, .45f, .12f) : new Color(.98f, .92f, .78f));
            b.Object(scen, "Grandstand " + name, JurassicLook.VertexColored(true), false, true);
            Crowd.Build(scen, name, seats, along, Mathf.Max(30f, length * .5f + 12f) * .9f, passSound, goSound, seed);
        }
    }
}
