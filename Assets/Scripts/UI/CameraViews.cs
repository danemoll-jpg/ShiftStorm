using UnityEngine;

namespace ShiftStorm
{
    // Round 6 item 2 (Dan: "they have it so you change your pov, can you do that here as well"): chastain_rush's CameraViews (Racer's, with
    // its FreeLook) ported and trimmed to the four forms. Four views: Chase (the Round 5 ChaseCam, unchanged), Far chase (ChaseCam's offset
    // 1.5x back and 1.55x up, as Chastain Rush), First person (from Abi's eyes: the cockpit rim, the glass bubble and the nose in view) and
    // Front (bumper: just ahead of the nose). X (pad) / V (keyboard) cycles them, per player in split screen (that player's own device);
    // the choice is remembered per player (settings.json "view" / "view2"; a check run neither reads nor writes it). The right stick looks
    // around (pulled straight back = look behind) and eases back when let go. Each view works in all four forms and through the change: the
    // head views follow the vehicle's own body (not the change's squash and twirl on the model pivot), so the swap and its burst play in
    // front of you instead of spinning the camera. Changes from Chastain Rush: no mouse look, no trailer / winner / helicopter views; the
    // views keep clear of walls, the cave roof and the water surface (a sphere cast from the vehicle, and a band around the surface).
    [DefaultExecutionOrder(50)]
    public sealed class CameraViews : MonoBehaviour
    {
        public enum View { Chase, FarChase, FirstPerson, Front }
        public static readonly string[] Names = { "Chase", "Far chase", "First person", "Front" };
        struct Pose
        {
            public Vector3 position; public Quaternion rotation; public float fov, near;
            public Pose(Vector3 p, Quaternion r, float f, float n) { position = p; rotation = r; fov = f; near = n; }
            public static Pose Lerp(Pose a, Pose b, float t) => new Pose(Vector3.Lerp(a.position, b.position, t), Quaternion.Slerp(a.rotation, b.rotation, t), Mathf.Lerp(a.fov, b.fov, t), Mathf.Lerp(a.near, b.near, t));
        }
        Camera cam; ChaseCam chase; float baseNear = .2f;
        int player = 1;
        public View Current { get; private set; }
        public string CurrentName => Names[(int)Current];
        View lastView; Pose last, blendFrom; float blend = 1; bool touched;
        public int Clips { get; private set; }   // checks: frames the camera was pulled in (a wall, the roof, the water surface)

        void Awake() { cam = GetComponent<Camera>(); chase = GetComponent<ChaseCam>(); baseNear = cam.nearClipPlane; SetPlayer(1); }
        public void SetPlayer(int p) { player = p; Current = lastView = (View)Mathf.Clamp(Settings.View(p), 0, Names.Length - 1); }
        public void SetView(View v) { if (v == Current) return; Current = v; Settings.SetView(player, (int)v); Debug.Log($"[View] player {player}: {CurrentName}"); }
        public void NextView() => SetView((View)(((int)Current + 1) % Names.Length));
        static bool CanDrive => !(Game.Instance && Game.Instance.MenuOpen) && !Garage.Active;

        void Update()
        {
            var v = chase ? chase.target : null; if (!v) return;
            if (v.Input.ConsumeView() && CanDrive) NextView();
        }

        void LateUpdate()
        {
            var v = chase ? chase.target : null;
            if (!cam || !v || Garage.Active || !chase.enabled) { Release(); return; }
            if (chase.FinishCamOn) { Release(); return; }   // Round 14 item 4: after the finish the steady finish camera, in every view
            if (hidHead && Current != View.FirstPerson) HideHead(null, false);
            bool far = Current == View.FarChase;
            chase.distanceScale = far ? 1.5f : 1f; chase.heightScale = far ? 1.55f : 1f;
            if (lastView != Current) { lastView = Current; blendFrom = last; blend = 0; }
            var chasePose = new Pose(transform.position, transform.rotation, cam.fieldOfView, baseNear);
            bool head = Current == View.FirstPerson || Current == View.Front;
            var pose = Current == View.FirstPerson ? FirstPerson(v, chasePose) : Current == View.Front ? FrontView(v, chasePose) : chasePose;
            HideHead(v, Current == View.FirstPerson);
            UpdateLook(v);
            pose = Looked(pose, v, head);
            // Far chase sits high and far back, and underwater the cave is tight: keep the chase views out of the roof and walls (the Round 5
            // Chase camera left the Coral Vault cave in 2 % of the sub's frames: Round 6 views check)
            if (!head && (far || v.Mode == Mode.Dive || v.Changing)) pose.position = Clear(v, pose.position);
            pose.position = OffSurface(v, pose.position);
            bool adjusted = (pose.position - chasePose.position).sqrMagnitude > 1e-6f;
            if (blend < 1) { blend = Mathf.Min(1, blend + Time.unscaledDeltaTime / .35f); pose = Pose.Lerp(blendFrom, pose, Mathf.SmoothStep(0, 1, blend)); }
            if (Current == View.Chase && blend >= 1 && !Looking && !adjusted) { last = chasePose; touched = false; cam.nearClipPlane = baseNear; return; }   // the Round 5 chase camera, untouched
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            cam.fieldOfView = pose.fov; cam.nearClipPlane = pose.near; last = pose; touched = true;
        }
        // Abi's head (one skinned mesh with her body, so it cannot be switched off alone) is folded to nothing at the neck while it is her
        // own first-person view: the camera sits where her eyes are and would otherwise look out through her hair. Her shadow loses its
        // head meanwhile; a player beside her in split screen would see it too (only in first person).
        Transform hidHead; Vector3 hidScale;
        void HideHead(ShiftVehicle v, bool hide)
        {
            var d = hide && v ? v.GetComponent<Driver>() : null; var h = d ? d.Head : null;
            if (hidHead && hidHead != h) { hidHead.localScale = hidScale; hidHead = null; }
            if (!h) return;
            if (!hidHead) { hidHead = h; hidScale = h.localScale; }
            h.localScale = hidScale * .001f;
        }
        void OnDisable() => HideHead(null, false);
        void Release() { HideHead(null, false); if (!touched) return; touched = false; cam.nearClipPlane = baseNear; if (chase) { chase.distanceScale = chase.heightScale = 1; } }

        // ---------- first person: Abi's eyes ----------
        Vector3 eyeLocal = new Vector3(0, 1.05f, -.05f);
        Quaternion fpRot; float fpY; bool fpFresh = true; Transform fpFor; View fpView;
        public static float FirstPersonFov(float chaseFov) => 70f + (chaseFov - ChaseCam.BaseFov) * .3f;
        Pose FirstPerson(ShiftVehicle v, Pose fallback)
        {
            var d = v.GetComponent<Driver>();
            var t = v.transform;
            Vector3 eye;
            if (d && d.Head && v.Forms && v.Forms.ShowDriver)
            {
                float s = Mathf.Max(.5f, d.Scale);
                // the head bone is at the base of the skull: the eyes ~10 cm above it and a little ahead (her head itself is folded away
                // in her own first-person view: HideHead)
                eye = d.Head.position + t.up * (.1f * s) + t.forward * (.05f * s);
                eye += t.up * (.25f * s);   // Round 9 item 1: 17 cm over her eyes (her head is folded away): the steering mechanism sits low in the view
            }
            else eye = t.TransformPoint(new Vector3(0, 1.05f, -.05f));
            // through the change the eye holds still in the vehicle's own frame, 12 cm higher, and the model squashes and twirls beneath it
            // (following her twirling head looked across her shoulder)
            if (!v.Changing && !v.TrickBusy) eyeLocal = t.InverseTransformPoint(eye);   // Round 17: a trick turns the model under a still eye
            else eye = t.TransformPoint(eyeLocal + Vector3.up * (v.Changing ? .12f : 0f));
            // Round 9 item 1: looking ahead (5 deg down, 3 in flight; Round 8 tipped it 12 deg down to show its oversized wheel), and a narrower
            // field-of-view swing than the chase camera's (70 at rest -> 77 at top speed, 80 on boost; chase +4 was 69 -> 102): the steering
            // mechanism stays a steady ~15-20 % of the screen height
            return Mounted(v, eye, Quaternion.Euler(v.Mode == Mode.Fly ? 3 : 5,
                 Mathf.Clamp(v.Body.angularVelocity.y * Mathf.Rad2Deg * .2f, -10, 10), 0), FirstPersonFov(fallback.fov), View.FirstPerson);
        }
        // ---------- front: just ahead of the nose, bumper level (a little higher on the water) ----------
        Pose FrontView(ShiftVehicle v, Pose fallback)
        {
            var t = v.transform;
            var from = t.TransformPoint(new Vector3(0, .45f, 0));
            var at = t.TransformPoint(new Vector3(0, v.Mode == Mode.Boat ? .75f : .45f, 2.2f));
            var dir = at - from; float len = dir.magnitude;
            if (Physics.SphereCast(from, .15f, dir / len, out var hit, len, ShiftVehicle.GroundMask, QueryTriggerInteraction.Ignore)) { at = from + dir / len * Mathf.Max(0, hit.distance - .1f); Clips++; }
            return Mounted(v, at, Quaternion.Euler(0, Mathf.Clamp(v.Body.angularVelocity.y * Mathf.Rad2Deg * .12f, -6, 6), 0), fallback.fov + 3f, View.Front);
        }
        Pose Mounted(ShiftVehicle v, Vector3 at, Quaternion extra, float fov, View which)
        {
            var target = v.transform.rotation * extra;
            bool snap = fpFresh || fpFor != v.transform || fpView != which || (at - last.position).sqrMagnitude > 30 * 30;   // a reset or a restart: no swing
            if (snap) { fpRot = target; fpY = at.y; fpFresh = false; fpFor = v.transform; fpView = which; }
            float dt = Time.deltaTime;
            fpRot = Quaternion.Slerp(fpRot, target, 1 - Mathf.Exp(-12 * dt));
            fpY = Mathf.Lerp(fpY, at.y, 1 - Mathf.Exp(-30 * dt));
            at.y += Mathf.Clamp(fpY - at.y, -.08f, .08f);
            return new Pose(at, fpRot, fov, .05f);
        }

        // ---------- keeping clear ----------
        Vector3 Clear(ShiftVehicle v, Vector3 p)
        {
            var pivot = v.transform.position + Vector3.up * 1.2f; var d = p - pivot; float len = d.magnitude;
            if (len > .01f && Physics.SphereCast(pivot, .3f, d / len, out var hit, len, ShiftVehicle.GroundMask, QueryTriggerInteraction.Ignore)) { Clips++; return pivot + d / len * Mathf.Max(.5f, hit.distance - .2f); }
            return p;
        }
        // never in the water surface's own plane (half the view above, half below): out to 0.22 m on the vehicle's side of it
        Vector3 OffSurface(ShiftVehicle v, Vector3 p)
        {
            if (!WaterVolume.SurfaceAt(p, out float s, 1f)) return p;
            if (Mathf.Abs(p.y - s) < .22f) { bool above = v.Body.position.y + .2f > s; p.y = s + (above ? .22f : -.22f); Clips++; }
            return p;
        }

        // ---------- look around: the right stick (Chastain Rush's FreeLook, without the mouse) ----------
        float lookYaw, lookPitch, yawSpeed, pitchSpeed;
        public bool Looking => Mathf.Abs(lookYaw) > .3f || Mathf.Abs(lookPitch) > .3f;
        public float LookYaw => lookYaw;
        void UpdateLook(ShiftVehicle v)
        {
            float yaw = 0, pitch = 0; bool held = false;
            if (CanDrive)
            {
                var stick = v.TrickAir ? Vector2.zero : v.Input.Look; float amount = Mathf.Clamp01((stick.magnitude - .15f) / .7f);
                // Round 17: in the air the right stick does tricks (no look)
                if (amount > 0) { yaw = Mathf.Atan2(stick.x, stick.y) * Mathf.Rad2Deg * amount; held = true; }
            }
            float smooth = held ? .1f : .22f;
            lookYaw = Mathf.SmoothDampAngle(lookYaw, yaw, ref yawSpeed, smooth, Mathf.Infinity, Time.unscaledDeltaTime);
            lookPitch = Mathf.SmoothDamp(lookPitch, pitch, ref pitchSpeed, smooth, Mathf.Infinity, Time.unscaledDeltaTime);
            if (!held && Mathf.Abs(Mathf.DeltaAngle(lookYaw, 0)) < .2f && Mathf.Abs(lookPitch) < .2f) lookYaw = lookPitch = yawSpeed = pitchSpeed = 0;
            lookYaw = Mathf.DeltaAngle(0, lookYaw);
        }
        Pose Looked(Pose pose, ShiftVehicle v, bool head)
        {
            if (!Looking) return pose;
            var turn = Quaternion.AngleAxis(lookYaw, v.transform.up);
            if (head) { pose.rotation = turn * pose.rotation; return pose; }
            var pivot = v.transform.position + Vector3.up * 1.2f;
            pose.position = Clear(v, pivot + turn * (pose.position - pivot)); pose.rotation = turn * pose.rotation;
            return pose;
        }
    }
}
