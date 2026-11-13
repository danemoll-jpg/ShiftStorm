using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftStorm
{
    // Round 2b HUD parts (see Hud): the arrow, the next-gate marker, the route strip, the speedometer, speed lines, the stats readout.
    // The guided check pilot (CheckPilot "nav") steers only by what these show: ArrowShown / ArrowYaw / ArrowPitch and MarkerShown / MarkerViewport.
    public sealed partial class Hud
    {
        public bool ArrowShown { get; private set; }
        public float ArrowYaw { get; private set; }     // degrees, + right, from the camera's view direction to the guide target (on the ground plan)
        public float ArrowPitch { get; private set; }   // degrees, + above
        public bool MarkerShown { get; private set; }
        public Vector2 MarkerViewport { get; private set; }
        public static bool StatsOn { get; set; }

        RectTransform arrow, marker, you, stripBar; Image arrowImage, markerImage; Text arrowLabel, markerLabel, climb, speedo, speedUnit, stats;
        Image speedBarFill; readonly List<Image> stripGates = new List<Image>(); readonly List<(RectTransform r, float angle, float phase, float len)> lines = new List<(RectTransform, float, float, float)>();
        Course stripFor; const float StripW = 760;
        readonly List<Image> rivalDots = new List<Image>();   // Round 3: the rivals on the route strip, in their colours
        float fpsTime, worstMs, worstShown; int fpsFrames; float fpsShown; int stepsAtLast; float stepsPerFrame; readonly Queue<(float t, float ms)> recent = new Queue<(float, float)>();
        static Sprite arrowSprite, diamondSprite, dotSprite;

        static Sprite MakeSprite(int size, System.Func<float, float, bool> inside)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                // 4x supersampled edge
                int hits = 0; for (int sy = 0; sy < 2; sy++) for (int sx = 0; sx < 2; sx++) if (inside((x + .25f + sx * .5f) / size * 2 - 1, (y + .25f + sy * .5f) / size * 2 - 1)) hits++;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / 4));
            }
            t.SetPixels32(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
        }
        static Sprite ArrowSprite => arrowSprite ? arrowSprite : (arrowSprite = MakeSprite(96, (x, y) => (y > .05f && Mathf.Abs(x) < (.95f - y) * .95f) || (y <= .05f && y > -.9f && Mathf.Abs(x) < .32f)));
        static Sprite DiamondSprite => diamondSprite ? diamondSprite : (diamondSprite = MakeSprite(64, (x, y) => { float d = Mathf.Abs(x) + Mathf.Abs(y); return d < .95f && d > .55f; }));
        static Sprite DotSprite => dotSprite ? dotSprite : (dotSprite = MakeSprite(32, (x, y) => y < .9f && Mathf.Abs(x) < (.9f - y) * .55f));   // a small down-pointing triangle once flipped

        Image Img(Transform p, Sprite sp, Vector2 anchor, Vector2 pos, Vector2 size, Color c)
        {
            var g = new GameObject("img"); g.transform.SetParent(p, false); var i = g.AddComponent<Image>(); i.sprite = sp; i.color = c; i.raycastTarget = false;
            var r = i.rectTransform; r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = pos; r.sizeDelta = size; return i;
        }

        void BuildGuide(Transform p)
        {
            // speed lines first (behind everything else)
            var rnd = new System.Random(5);
            for (int i = 0; i < 28; i++)
            {
                var im = Img(p, null, new Vector2(.5f, .5f), Vector2.zero, new Vector2(3, 90), new Color(1, 1, 1, 0));
                lines.Add((im.rectTransform, (float)(i * 360.0 / 28 + rnd.NextDouble() * 8), (float)rnd.NextDouble(), 60 + (float)rnd.NextDouble() * 110));
            }
            arrowImage = Img(p, ArrowSprite, new Vector2(.5f, .5f), new Vector2(0, 228), new Vector2(84, 84), new Color(1, .85f, .25f, .95f)); arrow = arrowImage.rectTransform;
            arrowImage.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
            arrowLabel = Label(p, new Vector2(.5f, .5f), new Vector2(0, 168), 20, TextAnchor.MiddleCenter, new Vector2(500, 28));
            climb = Label(p, new Vector2(.5f, .5f), new Vector2(110, 228), 24, TextAnchor.MiddleLeft, new Vector2(160, 30)); climb.color = new Color(1, .85f, .25f);
            markerImage = Img(p, DiamondSprite, new Vector2(0, 0), Vector2.zero, new Vector2(34, 34), new Color(1, .82f, .2f, .95f)); marker = markerImage.rectTransform;
            markerLabel = Label(marker, new Vector2(.5f, 0), new Vector2(0, -4), 18, TextAnchor.UpperCenter, new Vector2(300, 24)); markerLabel.color = new Color(1, .9f, .5f);
            // route strip
            var bg = Img(p, null, new Vector2(.5f, 1), new Vector2(0, -62), new Vector2(StripW + 8, 16), new Color(0, 0, 0, .5f)); stripBar = bg.rectTransform;
            you = Img(p, DotSprite, new Vector2(.5f, 1), new Vector2(0, -46), new Vector2(22, 22), Color.white).rectTransform; you.localEulerAngles = new Vector3(0, 0, 180);
            // speedometer (bottom right)
            speedo = Label(p, new Vector2(1, 0), new Vector2(-96, 40), 64, TextAnchor.LowerRight, new Vector2(300, 70));
            speedUnit = Label(p, new Vector2(1, 0), new Vector2(-24, 48), 22, TextAnchor.LowerRight, new Vector2(70, 30)); speedUnit.text = "MPH";
            var sbg = Img(p, null, new Vector2(1, 0), new Vector2(-144, 28), new Vector2(240, 10), new Color(0, 0, 0, .55f));
            speedBarFill = Img(sbg.transform, null, new Vector2(0, .5f), new Vector2(0, 0), new Vector2(0, 6), new Color(.5f, .9f, 1f));
            speedBarFill.rectTransform.pivot = new Vector2(0, .5f); speedBarFill.rectTransform.anchoredPosition = new Vector2(2, 0);
            // the stats readout (top left)
            stats = Label(p, new Vector2(0, 1), new Vector2(16, -12), 18, TextAnchor.UpperLeft, new Vector2(700, 60)); stats.color = new Color(.8f, 1f, .8f);
        }

        static Color FormColor(string form) => form.StartsWith("Fly") ? new Color(.45f, .75f, 1f) : form.StartsWith("Boat") ? new Color(.2f, .8f, .75f) : form.StartsWith("Div") ? new Color(.25f, .4f, .95f) : new Color(.95f, .62f, .25f);

        void BuildStrip(Course c)
        {
            stripFor = c;
            foreach (Transform t in stripBar) Destroy(t.gameObject);
            stripGates.Clear();
            float L = Mathf.Max(1, c.Length), from = 0;
            if (c.segments.Count == 0) Img(stripBar, null, new Vector2(0, .5f), new Vector2(4 + StripW * .5f, 0), new Vector2(StripW, 8), new Color(.7f, .7f, .7f, .8f));
            foreach (var s in c.segments)
            {
                float a = from / L * StripW, b = Mathf.Min(s.endProgress, L) / L * StripW; from = s.endProgress;
                Img(stripBar, null, new Vector2(0, .5f), new Vector2(4 + (a + b) * .5f, 0), new Vector2(Mathf.Max(2, b - a - 2), 8), FormColor(s.form));
            }
            foreach (var g in c.gates) stripGates.Add(Img(stripBar, null, new Vector2(0, .5f), new Vector2(4 + g.progress / L * StripW, 0), new Vector2(3, 16), new Color(1, 1, 1, .8f)));
        }

        void UpdateGuide()
        {
            var c = v.Course; if (!c || !cam) return;
            if (c != stripFor) BuildStrip(c);
            bool racing = !v.Holding && !v.Finished && !(game && game.MenuOpen);
            // route strip
            float L = Mathf.Max(1, c.Length);
            you.anchoredPosition = new Vector2(-StripW * .5f + Mathf.Clamp01(v.Progress / L) * StripW, -46);
            var race = game ? game.Race : null; int dot = 0;
            if (race && race.Active)
                foreach (var r in race.Racers)
                {
                    if (r == v || !r) continue;
                    if (dot >= rivalDots.Count) { var im = Img(you.parent, DotSprite, new Vector2(.5f, 1), Vector2.zero, new Vector2(16, 16), Color.white); im.rectTransform.localEulerAngles = new Vector3(0, 0, 180); rivalDots.Add(im); you.SetAsLastSibling(); }
                    var d = rivalDots[dot++]; d.enabled = true; d.color = r.Tint;
                    d.rectTransform.anchoredPosition = new Vector2(-StripW * .5f + Mathf.Clamp01(r.Progress / L) * StripW, -46);
                }
            for (int q = dot; q < rivalDots.Count; q++) rivalDots[q].enabled = false;
            for (int i = 0; i < stripGates.Count; i++)
            {
                bool next = i == v.GatesPassed; var r = stripGates[i].rectTransform;
                stripGates[i].color = i < v.GatesPassed ? new Color(1, 1, 1, .25f) : next ? GateMarker.Next : new Color(1, 1, 1, .8f);
                r.sizeDelta = next ? new Vector2(6, 22) : new Vector2(3, 16);
            }
            // the arrow: toward the guide target (the route ahead, or back to a missed gate), when it is off to the side, behind, well above
            // or below, or whenever a direction cue shows
            var local = cam.transform.InverseTransformPoint(v.GuideTarget);
            float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            // straight behind, the arrow keeps the side it already shows instead of flipping between left and right every frame
            if (Mathf.Abs(yaw) > 150f && Mathf.Abs(ArrowYaw) > 100f && Mathf.Sign(yaw) != Mathf.Sign(ArrowYaw)) yaw = Mathf.Sign(ArrowYaw) * 179.9f;
            ArrowYaw = yaw;
            ArrowPitch = Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg;
            bool air = v.Mode == Mode.Fly || v.Mode == Mode.Dive;
            bool cue = Time.time < v.CueUntil && v.Cue != "";
            ArrowShown = racing && (Mathf.Abs(ArrowYaw) > 32f || (air && Mathf.Abs(ArrowPitch) > 22f) || v.OffRoute || v.WrongWay || v.MissedGate || cue);
            arrowImage.enabled = ArrowShown; arrow.localEulerAngles = new Vector3(0, 0, -ArrowYaw);
            var g = v.NextGate;
            arrowLabel.text = ArrowShown && g ? $"{g.label}  {Vector3.Distance(v.Body.position, g.transform.position):0} m" : "";
            climb.text = racing && air && (ArrowShown || Mathf.Abs(ArrowPitch) > 10f) && Mathf.Abs(ArrowPitch) > 8f ? (ArrowPitch > 0 ? (v.Mode == Mode.Dive ? "RISE" : "CLIMB") : "DIVE") : "";
            // the next gate's marker, when it is in view
            MarkerShown = false;
            if (racing && g)
            {
                var vp = cam.WorldToViewportPoint(g.transform.position);
                if (vp.z > 0 && vp.x > .02f && vp.x < .98f && vp.y > .05f && vp.y < .95f)
                {
                    MarkerShown = true; MarkerViewport = vp;
                    var canvasSize = ((RectTransform)transform).sizeDelta;
                    marker.anchoredPosition = new Vector2(vp.x * canvasSize.x, vp.y * canvasSize.y + 26);
                    markerLabel.text = $"{Vector3.Distance(v.Body.position, g.transform.position):0} m";
                }
            }
            markerImage.enabled = MarkerShown; markerLabel.enabled = MarkerShown;
            // speedometer and speed lines
            float top = v.TopSpeedNow, frac = Mathf.Clamp01(v.Speed / Mathf.Max(1, v.BoostTopSpeedNow));
            speedo.text = $"{v.Speed * 2.237f:0}"; speedBarFill.rectTransform.sizeDelta = new Vector2(236 * frac, 6);
            speedBarFill.color = v.Boosting ? new Color(1, .5f, .15f) : new Color(.5f, .9f, 1f);
            float k = Mathf.Clamp01((v.Speed / Mathf.Max(1, top) - .55f) / .45f) * (v.Boosting ? 1f : .7f);
            float rate = .6f + 2.2f * Mathf.Clamp01(v.Speed / Mathf.Max(1, top));
            var size = ((RectTransform)transform).sizeDelta; float rmin = size.y * .42f, rmax = size.y * .9f;
            foreach (var (r, angle, phase, len) in lines)
            {
                float t = (Time.time * rate + phase) % 1f, rad = Mathf.Lerp(rmin, rmax, t);
                r.anchoredPosition = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad) * rad * 1.5f, Mathf.Sin(angle * Mathf.Deg2Rad) * rad);
                r.localEulerAngles = new Vector3(0, 0, angle - 90); r.sizeDelta = new Vector2(2.5f, len * (.5f + k));
                var im = r.GetComponent<Image>(); im.color = new Color(1, 1, 1, racing ? k * .55f * Mathf.Sin(t * Mathf.PI) : 0);
            }
            UpdateStats();
        }

        void UpdateStats()
        {
            float ms = Time.unscaledDeltaTime * 1000f; fpsFrames++; fpsTime += Time.unscaledDeltaTime;
            recent.Enqueue((Time.unscaledTime, ms)); while (recent.Count > 0 && Time.unscaledTime - recent.Peek().t > 2f) recent.Dequeue();
            if (fpsTime >= .5f)
            {
                fpsShown = fpsFrames / fpsTime; worstShown = 0; foreach (var x in recent) worstShown = Mathf.Max(worstShown, x.ms);
                stepsPerFrame = (ShiftVehicle.FixedSteps - stepsAtLast) / (float)fpsFrames; stepsAtLast = ShiftVehicle.FixedSteps; fpsFrames = 0; fpsTime = 0;
            }
            if (!StatsOn) { stats.text = ""; return; }
            stats.text = $"FPS {fpsShown:0.0}  ({1000f / Mathf.Max(1, fpsShown):0.0} ms, worst {worstShown:0.0} ms in 2 s)\n" +
                         $"speed {v.Speed:0.0} m/s  {v.Speed * 3.6f:0} km/h  {v.Speed * 2.237f:0} mph   physics {stepsPerFrame:0.0} steps/frame   {Screen.width}x{Screen.height} {Screen.fullScreenMode}";
        }
    }
}
