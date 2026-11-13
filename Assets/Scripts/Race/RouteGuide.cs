using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 2b item 1 ("there are no indicators whatsoever telling me where to go"): every progress gate is a big glowing frame (the Beacon
    // shader: unlit, no fog, so it reads from far away through the haze and the cave's murk). The next gate is gold, pulsing, with a light
    // column above it (not underwater); the one after it is a dimmer cyan; later gates are faint; passed gates are hidden. RouteGuide (on the
    // Game) keeps the states in step with the player's gates. The HUD's arrow, marker and route strip are in Hud.
    public sealed class GateMarker : MonoBehaviour
    {
        public static readonly List<GateMarker> All = new List<GateMarker>();
        public ProgressGate gate; public Course course;
        MeshRenderer frame, column; MaterialPropertyBlock block; int state = -1;
        static Material beacon;
        public static Material Beacon { get { if (!beacon) beacon = Resources.Load<Material>("Materials/Beacon"); if (!beacon) Debug.LogWarning("[Route] Beacon material missing"); return beacon; } }
        public static readonly Color Next = new Color(1f, .78f, .18f, 1f), After = new Color(.3f, .85f, 1f, .6f), Later = new Color(1f, 1f, 1f, .16f);

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        // visualHalf: the frame's size when the gate's counting frame is wider than the space it is in (the cave, the river)
        public static GateMarker Make(ProgressGate g, Course c, bool columnAbove, Vector2 visualHalf = default)
        {
            var half = visualHalf == default ? g.halfSize : visualHalf;
            float hx = half.x, hy = half.y, t = Mathf.Clamp(Mathf.Max(hx, hy) * .06f, .8f, 2.4f);
            var b = new MeshKit.Batch(); var w = Color.white;
            b.Add(MeshKit.Cube, new Vector3(-hx, 0, 0), Quaternion.identity, new Vector3(t, 2 * hy + t, t), w);
            b.Add(MeshKit.Cube, new Vector3(hx, 0, 0), Quaternion.identity, new Vector3(t, 2 * hy + t, t), w);
            b.Add(MeshKit.Cube, new Vector3(0, hy, 0), Quaternion.identity, new Vector3(2 * hx + t, t, t), w);
            b.Add(MeshKit.Cube, new Vector3(0, -hy, 0), Quaternion.identity, new Vector3(2 * hx + t, t, t), w);
            // a forward chevron on the top bar: which way through
            foreach (float s in new[] { -1f, 1f }) b.Add(MeshKit.Cube, new Vector3(s * t * 1.6f, hy + t * 1.8f, 0), Quaternion.Euler(0, 0, s * 40), new Vector3(t * .8f, t * 3.2f, t * .8f), w);
            var go = b.Object(g.transform, "gate frame", Beacon, false, false);
            var m = g.gameObject.AddComponent<GateMarker>(); m.gate = g; m.course = c; m.frame = go.GetComponent<MeshRenderer>();
            if (columnAbove)
            {
                var cb = new MeshKit.Batch(); cb.Add(MeshKit.Cube, new Vector3(0, hy + 45, 0), Quaternion.identity, new Vector3(t * 1.4f, 90, t * 1.4f), new Color(1, 1, 1, .45f));
                var cg = cb.Object(g.transform, "gate column", Beacon, false, false); cg.transform.rotation = Quaternion.Euler(0, g.transform.eulerAngles.y, 0);
                m.column = cg.GetComponent<MeshRenderer>();
            }
            m.Set(0);
            return m;
        }

        // 0 later, 1 next, 2 the one after, 3 passed (hidden)
        public void Set(int s)
        {
            if (s == state) return; state = s;
            block ??= new MaterialPropertyBlock();
            var col = s == 1 ? Next : s == 2 ? After : Later;
            block.SetColor("_Color", col); block.SetFloat("_Pulse", s == 1 ? 1 : 0);
            frame.SetPropertyBlock(block); frame.enabled = s != 3;
            if (column) { column.SetPropertyBlock(block); column.enabled = s == 1; }
        }
        public int State => state;
    }

    public sealed class RouteGuide : MonoBehaviour
    {
        public ShiftVehicle v;
        void LateUpdate()
        {
            if (!v || !v.Course) return;
            foreach (var m in GateMarker.All)
            {
                if (m.course != v.Course) { m.Set(3); continue; }
                int i = m.gate.index, passed = v.GatesPassed;
                m.Set(i < passed ? 3 : i == passed ? 1 : i == passed + 1 ? 2 : 0);
            }
        }
    }
}
