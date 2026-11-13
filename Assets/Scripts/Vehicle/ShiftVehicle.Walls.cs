using System;
using UnityEngine;

namespace ShiftStorm
{
    // Round 4 item 1 (Dan: "Brushing up against the wall shouldn't stop you. Should slow you a tad and bounce you off."): walls glance you
    // off in every form. On a hit the velocity into the wall is removed, the velocity along it is kept minus a loss scaled by the angle
    // (ShiftTuning.wallLoss*: about 5 % at 10 deg, 9 % at 25 deg, 17 % at 45 deg, 40 % head-on), and a short nudge pushes the racer away from
    // the wall. A car's nose is turned most of the way along the wall and its yaw spin is taken out (never spun round); a plane or a
    // submarine takes the new direction as its heading. Pressing on along a wall costs only a slow slide loss, so holding against it keeps
    // you moving. The hull has no friction (VehicleFactory), so seams, corners and mesh edges cannot catch it. The Round 3 scrape rule
    // (ShiftTuning.scrape*: speed cut, throttle ignored, no updraft for 2 s) stays for the Sky Arch slit's buttress only, so a scrape there
    // still costs more than the shortcut saves. Feedback: WallHit (the chase camera's bump, the hit sound).
    public sealed partial class ShiftVehicle
    {
        public event Action<Collision, bool> Contact;   // (contact, first touch): checks
        public event Action<float, float, string> WallHit;   // (angle into the wall in deg, share of the speed kept, what was hit)
        public int Scrapes { get; private set; }         // fresh hits in the air or underwater
        public int WallHits { get; private set; }
        public float LastWallAt { get; private set; } = -1;
        public float LastWallKeep { get; private set; } = 1;
        public float LastWallAngle { get; private set; }
        float scrapeUntil = -1, lastScrape = -1, lastGlanceAt = -1;
        public bool Scraped => Time.time < scrapeUntil;
        public bool ScrapedRecently => lastScrape > 0 && Time.time - lastScrape < 2f;   // the Sky Arch slit's updraft does not lift a plane that just scraped

        void OnCollisionEnter(Collision c) { Contact?.Invoke(c, true); Wall(c, true); }
        void OnCollisionStay(Collision c) { Contact?.Invoke(c, false); Wall(c, false); }

        // the share of the speed kept by a fresh hit at this angle into the wall (0 = along it, 90 = head-on)
        public float GlanceKeep(float angleDeg) => 1f - (shared.wallLossMin + (shared.wallLossHeadOn - shared.wallLossMin) * Mathf.Pow(Mathf.Clamp01(angleDeg / 90f), shared.wallLossExponent));

        void Wall(Collision c, bool first)
        {
            if (Holding || Body.isKinematic || !c.collider) return;
            if (c.rigidbody && c.rigidbody.GetComponent<ShiftVehicle>()) return;   // a bump with another racer is physics only
            bool air = Mode == Mode.Fly || Mode == Mode.Dive;
            if (air && Changing && Time.time - ChangeStartedAt < .5f) return;
            int k = c.contactCount; if (k == 0) return;
            Vector3 n = Vector3.zero; for (int i = 0; i < k; i++) n += c.GetContact(i).normal;
            if (n.sqrMagnitude < 1e-6f) return; n.Normalize();
            if (!air)
            {
                if (Mathf.Abs(n.y) > .55f) return;   // the ground, a ramp, a beach: the suspension's job
                n.y = 0; if (n.sqrMagnitude < 1e-4f) return; n.Normalize();
            }
            // what the racer was trying to do this step: in the air and underwater the form sets the velocity itself every step
            float own = Mode == Mode.Fly ? Airspeed : Mode == Mode.Dive ? DiveSpeed : 0;
            var vel = air ? transform.forward * own : stepVel;
            float sp = vel.magnitude;
            if (sp < (Mode == Mode.Dive ? 5f : 3f)) return;   // creeping or reversing along a floor is not a hit (Round 1 E5)
            float into = -Vector3.Dot(vel, n); if (into < .25f) return;   // already moving along or away from it
            float angle = Mathf.Asin(Mathf.Clamp01(into / sp)) * Mathf.Rad2Deg;
            // a new hit only after wallRepeatSeconds (by time, not by contact: in a corner the contacts alternate between two surfaces)
            bool fresh = Time.time - lastGlanceAt > shared.wallRepeatSeconds;
            bool slit = c.collider.name.StartsWith("Slit buttress");
            float keep = fresh ? GlanceKeep(angle) : 1f - shared.wallSlideLossPerSecond * Time.fixedDeltaTime;
            if (fresh && slit && air) { keep = Mathf.Min(keep, shared.scrapeSpeedKeep); scrapeUntil = Time.time + shared.scrapeSeconds; lastScrape = Time.time; }
            // along the wall: the velocity's own direction on the wall's plane; head-on, the route's direction there (you keep going)
            // a steep hit never sends you back the way you came: the route's way along the wall then (in a tunnel a backward slide made the
            // submarine bounce from wall to wall trying to turn round)
            var t = Vector3.ProjectOnPlane(vel, n); var rt = Vector3.ProjectOnPlane(RouteDir, n); if (!air) rt.y = 0;
            bool backing = air ? own < 0 : Vector3.Dot(vel, transform.forward) < 0;   // reversing on purpose: slide the way it is going
            if (!backing && (t.sqrMagnitude < (.25f * sp) * (.25f * sp) || (angle > 40f && Vector3.Dot(t, RouteDir) < 0 && rt.sqrMagnitude > .04f))) t = rt;
            if (t.sqrMagnitude < 1e-4f) t = Vector3.ProjectOnPlane(transform.forward, n);
            if (t.sqrMagnitude < 1e-4f) return;
            var along = t.normalized; float newSp = sp * keep;
            float nudge = fresh ? shared.wallNudge : shared.wallNudge * .3f;
            switch (Mode)
            {
                case Mode.Fly:
                case Mode.Dive:
                {
                    // a small angle away from the wall is the nudge (the form flies along its heading)
                    var d = (along + n * Mathf.Clamp(nudge / Mathf.Max(newSp, 1f), 0, .2f)).normalized;
                    if (backing) d = -d;   // the nose points against the motion when reversing (Round 1 E5: a sub backing along the floor)
                    yawDeg = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                    float pitch = Mathf.Asin(Mathf.Clamp(d.y, -1, 1)) * Mathf.Rad2Deg;
                    if (Mode == Mode.Fly) { Airspeed = Mathf.Max(fly.minSpeed, newSp); PitchDeg = Mathf.Clamp(pitch, -fly.maxPitchDown, fly.maxPitchUp); }
                    else { DiveSpeed = Mathf.Sign(DiveSpeed) * Mathf.Max(Mathf.Min(Mathf.Abs(DiveSpeed), dive.cruiseSpeed * .4f), newSp); PitchDeg = Mathf.Clamp(pitch, -dive.maxPitch, dive.maxPitch); diveYawRate = 0; }
                    var rot = Quaternion.Euler(-PitchDeg, yawDeg, -RollDeg);
                    Body.linearVelocity = rot * Vector3.forward * (Mode == Mode.Fly ? Airspeed : DiveSpeed); Body.angularVelocity = Vector3.zero; Body.MoveRotation(rot);
                    break;
                }
                default:
                {
                    var nv = along * newSp + n * nudge; nv.y = Body.linearVelocity.y;
                    Body.linearVelocity = nv;
                    var av = Body.angularVelocity; av.y = 0; Body.angularVelocity = av;   // never spun round by a wall
                    // the nose turned most of the way along the wall when it points into it (driving forward), so the grip does not carry it
                    // straight back in; steering stays live
                    var fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized; var fl = Vector3.ProjectOnPlane(along, Vector3.up).normalized;
                    if (Vector3.Dot(fwd, n) < 0 && Vector3.Dot(fwd, fl) > 0 && fl.sqrMagnitude > .5f)
                    {
                        float turn = Vector3.SignedAngle(fwd, fl, Vector3.up) * shared.wallNoseAlign;
                        Body.rotation = Quaternion.AngleAxis(turn, Vector3.up) * Body.rotation;
                    }
                    break;
                }
            }
            if (!fresh) return;
            lastGlanceAt = Time.time; WallHits++; if (air) Scrapes++;
            LastWallAt = Time.time; LastWallKeep = keep; LastWallAngle = angle;
            Note("wall", $"wall {c.collider.name} as {Mode}: {angle:0} deg, {sp:0.0} -> {newSp:0.0} m/s ({keep * 100:0} % kept){(slit && air ? ", slit scrape (Round 3 rule)" : "")}");
            WallHit?.Invoke(angle, keep, c.collider.name);
        }
    }
}
