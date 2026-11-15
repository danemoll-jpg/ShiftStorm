# ShiftStorm Round 9 item 3c (the dinosaur stand-ins replaced): COPIED from Tee'd Off's Tools/Characters/rig_dino.py (read-only; with its
# helpers glb_load.py and rig_biscuit.py, copied unchanged). Changed here: the creatures (Dan's Tripo raptor and plesiosaur, converted to
# one-mesh GLBs by Tools/Blender/dino_to_glb.py into local/dinos/<Name>/<Name>_meshy.glb + _meshy_basecolor.png), and where the rigs go
# (Assets/Resources/Dinos/<style>_rig.bytes + <style>_basecolor.png, the game loads them at start-up: World/Dinos.cs DinoRig) and the plots
# (local/dinos/plots). Docs/REUSE-AUDIT.md. Tee'd Off's own header follows.
"""Rigs Jurassic Par's dinosaurs (Round 17) from their static Meshy meshes.

Meshy's auto-rig was tried on all four (Art/Characters/incoming/<Name>/<Name>_meshy_rigged.*, 5 credits each): it is a
humanoid skeleton, so on a dinosaur its "head" sits at the shoulders and its "arms" float outside the body (the
brontosaurus's Head bone is 4 m below its real head). As with Biscuit (rig_biscuit.py), this script instead:
  1. straightens each mesh: the feet found as clusters at ground level (4 for the brontosaurus and triceratops, 2 for
     the T. rex; the pterodactyl by its wingspan), turned to face +Z (the head end is the higher end) with its feet at
     y = 0; the feet clusters are checked to be clearly apart (the concepts are 3/4 views: a merged far leg would show
     here as a missing or oversized cluster);
  2. builds a skeleton from the mesh: a leg chain per foot (centres measured slice by slice), hips / spine / chest along
     the back, a neck chain and head to the far end of the front (the brontosaurus's long neck gets four links), a jaw,
     a four-link tail; for the pterodactyl the wings (two links each side), body, neck and head;
  3. weights every vertex (legs by their own column, the rest by distance to each bone, masked by region);
  4. drops stray stretched faces;
  5. writes Assets/_Project/Art/Characters/Dinos/<style>/<style>_rig.bytes (Biscuit's format: Unity space, x mirrored,
     winding flipped) + the base colour, for the editor step TeedOff.EditorTools.DinoImport, and a weight plot in
     Builds/screens/dinos/.

Round 18: Backyard Giants' cat (style "cat") the same way. Loose mesh bits (drawn motion lines turned into geometry) are
dropped first; the head end is the wider raised end (the cat's curled tail is higher than its head); its tail chain
follows the curl along the surface; its whiskers go with the head. Every creature also gets <style>_eyes.json: its two
painted eyes (centre, normal, radius, lid colour, in the rig's space) for the blink lids, with a check plot
Builds/screens/dinos/<style>_eyes.png.

Round 20: Lunar Links' two bipeds (styles "alien" and "robot", rig_biped below; Meshy image-to-3D from Dan's concepts in
concepts/lunar/, animated in code, no Mixamo). Both are drawn front-on with their arms out, so the skeleton is placed from
the mesh with a few per-style numbers (BIPEDS: crotch, neck, shoulder): Root, Hips, Spine, Chest, Head, per side
ArmUpper / ArmLower / Hand, LegUpper / LegLower / Foot, the alien's two-link antennae (they wobble), the robot's side
antennae (from its ear balls) and its claws' lower jaws (Pincer, for the clipping). The robot's baked glass dome is cut
away first (robot_dome.py) and its numbers written to robot_dome.json beside the rig for the game's real glass dome.
They're written to Assets/_Project/Art/Characters/Lunar/<style>/ (the same rig format). Check renders of each in poses
(arms up, a step, head turned) go to Builds/screens/lunar/<style>_poses.png (software renderer, mesh_preview.py).

Round 22: Bogey Manor's ghosts (styles "mortimer" and "ghost", from concepts/haunted/): Mortimer has no legs, only a wispy
tail, so Mixamo's auto-rigger (it needs knees) can't take him. The biped rig with tail=True: no feet to stand him up by
(he's left facing +Z as Meshy made him, centred on his torso, the tail tip at y = 0), and a three-link Tail chain hangs
from the hips through everything below the waistcoat in place of legs. The little ghost the same (its hem is the
"crotch"). Written to Assets/_Project/Art/Characters/Haunted/<style>/, poses in Builds/screens/haunted/.

python Tools/Characters/rig_dino.py [--only TRex]     # rig (+ eyes)
python Tools/Characters/rig_dino.py --eyes [--only X]  # only the eyes, from the rigs already written
"""
import argparse
import os
import struct
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from glb_load import load  # noqa: E402
from rig_biscuit import read_normals, seg_dist, smooth  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
INCOMING = os.path.join(ROOT, 'local', 'dinos')   # ShiftStorm: the converted GLBs
OUT = os.path.join(ROOT, 'local', 'dinos', 'rigs')   # ShiftStorm: copied to Assets/Resources/Dinos by Tools/Characters/import_dinos.py
PLOTS = os.path.join(ROOT, 'local', 'dinos', 'plots')

# Name -> (game style, feet, neck links)
DINOS = {
    'Raptor': ('raptor', 2, 1),       # ShiftStorm Round 9: Dan's Tripo raptor (two legs, like the T. rex)
    'Plesiosaur': ('plesio', 4, 4),   # ShiftStorm Round 9: Dan's Tripo plesiosaur (four flippers, the long neck in four links)
}
# Round 20: Lunar Links' bipeds. y numbers are fractions of the height, x of the half width (both measured on the mesh).
#   crotch: where the legs join; neck: the head's bottom; shoulder: where the arm leaves the body (x) and its height;
#   arm_lo / arm_hi: the arm's height band (root_lo: its bottom within root_w of the shoulder line); antenna: from this height up (the alien), eyes: painted eyes for lids.
BIPEDS = {
    'Alien': dict(style='alien', crotch=0.12, neck=0.42, shoulder=0.585, shoulder_y=0.33, arm_lo=0.2, arm_hi=0.39,
                  root_lo=0.27, root_w=0.12,
                  antenna=0.855, eyes=3),
    'Robot': dict(style='robot', crotch=0.185, neck=None, shoulder=0.55, shoulder_y=0.47, arm_lo=0.15, arm_hi=0.49,
                  root_lo=0.15, root_w=0.0,
                  antenna=None, eyes=0),
    # Round 22: Bogey Manor's ghosts: a tail instead of legs.
    'Mortimer': dict(style='mortimer', crotch=0.31, neck=0.66, shoulder=0.40, shoulder_y=0.60, arm_lo=0.53, arm_hi=0.69,
                     root_lo=0.55, root_w=0.04, antenna=None, eyes=0, tail=True),
    'LittleGhost': dict(style='ghost', crotch=0.30, neck=0.62, shoulder=0.62, shoulder_y=0.60, arm_lo=0.48, arm_hi=0.72,
                        root_lo=0.50, root_w=0.03, antenna=None, eyes=0, tail=True),
}
HAUNTED_OUT = os.path.join(ROOT, 'Assets', '_Project', 'Art', 'Characters', 'Haunted')
HAUNTED_PLOTS = os.path.join(ROOT, 'Builds', 'screens', 'haunted')
HAUNTED_STYLES = ('mortimer', 'ghost')
LUNAR_OUT = os.path.join(ROOT, 'Assets', '_Project', 'Art', 'Characters', 'Lunar')
LUNAR_PLOTS = os.path.join(ROOT, 'Builds', 'screens', 'lunar')


def out_dir(style):
    return os.path.join(LUNAR_OUT if style in ('alien', 'robot') else HAUNTED_OUT if style in HAUNTED_STYLES else OUT, style)


def plots_dir(style):
    return LUNAR_PLOTS if style in ('alien', 'robot') else HAUNTED_PLOTS if style in HAUNTED_STYLES else PLOTS


def kmeans(X, k, iters=40, seed=1):
    rng = np.random.default_rng(seed)
    # Farthest-point start: well-spread centres.
    c = [X[rng.integers(len(X))]]
    for _ in range(k - 1):
        d = np.min(np.stack([np.linalg.norm(X - ci, axis=1) for ci in c], 1), 1)
        c.append(X[np.argmax(d)])
    c = np.array(c)
    for _ in range(iters):
        lab = np.argmin(np.stack([np.linalg.norm(X - ci, axis=1) for ci in c], 1), 1)
        c = np.array([X[lab == i].mean(0) if np.any(lab == i) else c[i] for i in range(k)])
    return c, lab


def weld_components(P, T, tol_frac=1e-4):
    """Connected components of the triangles, vertices welded by position (Meshy splits vertices at UV seams)."""
    from scipy.sparse import coo_matrix
    from scipy.sparse.csgraph import connected_components
    tol = tol_frac * np.ptp(P, 0).max()
    _, weld = np.unique(np.round(P / tol).astype(np.int64), axis=0, return_inverse=True)
    weld = weld.ravel()
    nw = weld.max() + 1
    r = np.r_[weld[T[:, 0]], weld[T[:, 1]], weld[T[:, 2]]]
    c = np.r_[weld[T[:, 1]], weld[T[:, 2]], weld[T[:, 0]]]
    n, lab = connected_components(coo_matrix((np.ones(len(r)), (r, c)), shape=(nw, nw)), directed=False)
    return lab[weld[T[:, 0]]], n


def drop_islands(name, P, I, N, UV, keep_frac=0.02):
    """Removes small loose bits (e.g. drawn motion lines turned into geometry): components with under keep_frac of the
    biggest's triangles, or whose box doesn't touch the biggest's box (grown by 10%). Returns the arrays reindexed."""
    T = I.reshape(-1, 3).astype(np.int64)
    lab, n = weld_components(P, T)
    cnt = np.bincount(lab, minlength=n)
    main = np.argmax(cnt)
    mv = P[np.unique(T[lab == main])]
    lo, hi = mv.min(0), mv.max(0)
    pad = 0.1 * (hi - lo)
    keep = np.ones(n, bool)
    for k in range(n):
        if k == main:
            continue
        v = P[np.unique(T[lab == k])]
        far = np.any(v.min(0) > hi + pad) or np.any(v.max(0) < lo - pad)
        if cnt[k] < keep_frac * cnt[main] or far:
            keep[k] = False
            print(f'[{name}] dropped loose bit: {cnt[k]} triangles at {np.round(v.mean(0), 3).tolist()} (size {np.round(np.ptp(v, 0), 3).tolist()})')
    print(f'[{name}] {n} mesh component(s); kept {int(keep.sum())}')
    if keep.all():
        return P, I, N, UV
    T = T[keep[lab]]
    used = np.unique(T)
    remap = np.full(len(P), -1)
    remap[used] = np.arange(len(used))
    return P[used], remap[T].ravel(), N[used], UV[used]


def rot_y(fwd_xz):
    """Rotation turning the xz direction fwd_xz onto +Z."""
    yaw = np.arctan2(fwd_xz[0], fwd_xz[1])
    c, s = np.cos(-yaw), np.sin(-yaw)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]]), np.degrees(yaw)


def straighten(P, feet_n, name):
    y0, y1 = P[:, 1].min(), P[:, 1].max()
    H = y1 - y0
    report = []
    if feet_n == 0:
        # The pterodactyl: the wingspan is the long horizontal axis; forward is across it, toward the beak.
        X = P[:, [0, 2]] - P[:, [0, 2]].mean(0)
        w, v = np.linalg.eigh(np.cov(X.T))
        wing = v[:, np.argmax(w)]
        fwd = np.array([-wing[1], wing[0]])
        upper = P[:, 1] > y0 + 0.6 * H
        proj = X[upper] @ fwd
        if proj.max() < -proj.min():
            fwd = -fwd
        R, yaw = rot_y(fwd)
        Q = P @ R.T
        Q[:, 0] -= Q[:, 0].mean()
        Q[:, 2] -= Q[:, 2].mean()
        Q[:, 1] -= Q[:, 1].min()
        report.append(f'wingspan {np.ptp(Q[:, 0]):.2f}, turned {yaw:.1f} deg')
        return Q, R, report, None
    low = P[:, 1] < y0 + 0.05 * H
    F = P[low][:, [0, 2]]
    centres, lab = kmeans(F, feet_n)
    sizes = [int((lab == i).sum()) for i in range(feet_n)]
    spread = [float(np.linalg.norm(F[lab == i] - centres[i], axis=1).max()) for i in range(feet_n)]
    gaps = [float(np.linalg.norm(centres[i] - centres[j])) for i in range(feet_n) for j in range(i + 1, feet_n)]
    report.append(f'feet {feet_n}: sizes {sizes}, radius {np.round(spread, 2).tolist()}, min gap {min(gaps):.2f}')
    merged = min(gaps) < 1.6 * max(spread) or min(sizes) < 0.25 * max(sizes)
    report.append('FEET LOOK MERGED' if merged else 'feet clearly apart')
    if feet_n == 4 or name == 'Raptor':   # ShiftStorm: Dan's raptor stands with its body turned against its feet: run along the body
        # The body's long axis (head to tail), from every vertex: a wide stance can be wider than the feet are long.
        cc = P[:, [0, 2]] - P[:, [0, 2]].mean(0)
        w, v = np.linalg.eigh(np.cov(cc.T))
        axis = v[:, np.argmax(w)]
    else:
        side = centres[1] - centres[0]
        axis = np.array([-side[1], side[0]])
        axis /= np.linalg.norm(axis)
    X = P[:, [0, 2]] - centres.mean(0)
    proj = X @ axis
    L = np.ptp(proj)
    # The head end: the end whose raised part (above 60% of the height) is wider. A dinosaur's tail stays low (nothing
    # raised there); a cat's curled-up tail can be higher than its head but is far thinner. A tie: the higher end.
    def end(sel):
        if not np.any(sel):
            return 0.0, 0.0
        up = sel & (P[:, 1] > y0 + 0.6 * H)
        lat = X[up] @ np.array([-axis[1], axis[0]])
        w = float(np.percentile(lat, 95) - np.percentile(lat, 5)) if up.sum() > 20 else 0.0
        return w, float(P[sel, 1].max())
    wa, hi_a = end(proj > 0.25 * L * 0.5)
    wb, hi_b = end(proj < -0.25 * L * 0.5)
    if abs(wa - wb) > 0.05 * L:
        flip = wb > wa
        why = f'head = the wider raised end ({max(wa, wb):.2f} vs {min(wa, wb):.2f})'
    else:
        flip = hi_b > hi_a
        why = f'head = the higher end ({max(hi_a, hi_b):.2f} vs {min(hi_a, hi_b):.2f})'
    if flip:
        axis = -axis
    report.append(why)
    R, yaw = rot_y(axis)
    Q = P @ R.T
    c3 = R @ np.array([centres.mean(0)[0], 0, centres.mean(0)[1]])
    Q[:, 0] -= c3[0]
    Q[:, 2] -= c3[2]
    Q[:, 1] -= Q[:, 1].min()
    feet = (np.c_[centres[:, 0], np.zeros(feet_n), centres[:, 1]] @ R.T)
    feet[:, 0] -= c3[0]
    feet[:, 2] -= c3[2]
    report.append(f'turned {yaw:.1f} deg; size {np.round(Q.max(0) - Q.min(0), 2).tolist()}')
    return Q, R, report, feet[:, [0, 2]]


def chain(Q, mask, start, links, frac=1.0):
    """Centres along a limb (neck, tail, wing): the points in mask binned by distance from start; links + 1 joints."""
    pts = Q[mask]
    if len(pts) < 10:
        return [start, start + np.array([0, 0.1, 0.3])] * 1
    d = np.linalg.norm(pts - start, axis=1)
    dmax = np.percentile(d, 99.5) * frac
    joints = [start]
    for k in range(1, links + 1):
        t = dmax * k / links
        sel = np.abs(d - t) < dmax / links * 0.5
        joints.append(pts[sel].mean(0) if np.any(sel) else joints[-1] + (joints[-1] - joints[-2] if len(joints) > 1 else np.array([0, 0, 0.1])))
    return joints


def tex_colors(path, UV):
    """The base colour (0..1) at each UV (glTF: v down from the top)."""
    tex = np.asarray(Image.open(path).convert('RGB')).astype(np.float32) / 255
    h, w, _ = tex.shape
    return tex[np.clip((UV[:, 1] * h).astype(int), 0, h - 1), np.clip((UV[:, 0] * w).astype(int), 0, w - 1)]


def curled_tail(name, Q, I, zh, top, links):
    """A tail that curls up over the back (the cat): the tail is what rises above the back behind the hips; its joints are
    the middles of rings at equal distances along the surface from its base, so the chain follows the curl."""
    from scipy.sparse import coo_matrix
    from scipy.sparse.csgraph import dijkstra
    T = I.reshape(-1, 3).astype(np.int64)
    # Its curl can reach forward past the hips, but the head never comes back behind the body's middle.
    m = ((Q[:, 2] < zh) & (Q[:, 1] > top * 0.92)) | ((Q[:, 2] < 0) & (Q[:, 1] > top * 1.1))
    tm = m[T].all(1)
    e = np.r_[T[tm][:, [0, 1]], T[tm][:, [1, 2]], T[tm][:, [2, 0]]]
    # Weld seam-split vertices so the distance runs across UV seams.
    tol = 1e-4 * np.ptp(Q, 0).max()
    _, weld = np.unique(np.round(Q / tol).astype(np.int64), axis=0, return_inverse=True)
    weld = weld.ravel()
    a, b = weld[e[:, 0]], weld[e[:, 1]]
    w = np.linalg.norm(Q[e[:, 0]] - Q[e[:, 1]], axis=1) + 1e-6
    nw = weld.max() + 1
    G = coo_matrix((np.r_[w, w], (np.r_[a, b], np.r_[b, a])), shape=(nw, nw)).tocsr()
    # The base: the lowest band of the tail region; the distance is measured from all of it at once.
    ys = Q[m, 1]
    base = np.unique(weld[np.nonzero(m)[0][ys < ys.min() + 0.03 * np.ptp(Q[:, 1])]])
    d = dijkstra(G, indices=base, min_only=True)[weld]
    ok = m & np.isfinite(d)
    dmax = np.percentile(d[ok], 99.5)
    # Ring middles at 16 steps, then the joints spaced evenly along that line.
    C = np.array([Q[ok & (np.abs(d - t) < dmax * 0.03)].mean(0) for t in np.linspace(0.03, 0.97, 16) * dmax])
    s = np.r_[0, np.cumsum(np.linalg.norm(np.diff(C, axis=0), axis=1))]
    joints = [np.array([np.interp(t, s, C[:, i]) for i in range(3)]) for t in np.linspace(0, s[-1], links + 1)]
    print(f'[{name}] tail: {int(ok.sum())} vertices, {dmax:.2f} along the surface; joints {np.round(joints, 2).tolist()}')
    return joints, m | ((Q[:, 2] < zh) & (Q[:, 1] > top * 0.8))


def rig(name, style, feet_n, neck_links):
    src = os.path.join(INCOMING, name)
    glb = os.path.join(src, f'{name}_meshy.glb')
    j, P, I, UV = load(glb)
    N = read_normals(glb, j)
    P, I, N, UV = drop_islands(name, P, I, N, UV)
    Q, R, report, feet = straighten(P, feet_n, name)
    Nn = N @ R.T
    for r in report:
        print(f'[{name}] {r}')
    H = Q[:, 1].max()
    zmin, zmax = Q[:, 2].min(), Q[:, 2].max()
    bones = []  # name, parent, start, end, radius

    def bone(nm, parent, a, t, r):
        bones.append([nm, parent, np.array(a, float), np.array(t, float), float(r)])
        return len(bones) - 1

    root = bone('Root', -1, [0, 0, 0], [0, 0.3, 0], 0.01)
    legs, tails, necks, wings = [], [], [], []
    jaw = head = None
    allow_extra = {}

    if feet_n == 0:
        # Pterodactyl: body at the middle, wings out to each side, the neck and head forward and up.
        width = np.ptp(Q[:, 0])
        core = np.abs(Q[:, 0]) < width * 0.08
        cy = Q[core, 1].mean()
        body = bone('Hips', root, [0, cy, Q[core, 2].min() * 0.5], [0, cy, 0], 0.2 * H)
        chest = bone('Chest', body, [0, cy, 0], [0, cy + 0.05 * H, Q[core, 2].max() * 0.3], 0.2 * H)
        for sgn, sn in [(1, 'L'), (-1, 'R')]:
            m = (Q[:, 0] * sgn) > width * 0.08
            sh = np.array([sgn * width * 0.09, cy + 0.05 * H, 0.0])
            js = chain(Q, m, sh, 2)
            w0 = bone(f'Wing{sn}0', chest, js[0], js[1], 0.12 * width)
            w1 = bone(f'Wing{sn}1', w0, js[1], js[2], 0.12 * width)
            wings += [w0, w1]
        hm = core & (Q[:, 1] > cy + 0.15 * H)
        js = chain(Q, hm, np.array([0, cy + 0.1 * H, 0.0]), 2)
        nk = bone('Neck0', chest, js[0], js[1], 0.12 * H)
        necks.append(nk)
        head = bone('Head', nk, js[1], js[2], 0.18 * H)
        body_set = [body, chest, nk, head] + wings
        D = np.stack([seg_dist(Q, bones[k][2], bones[k][3]) / bones[k][4] for k in body_set], 1)
        allow = np.ones_like(D, bool)
        for i, k in enumerate(body_set):
            if k in wings:
                sgn = 1 if bones[k][0].startswith('WingL') else -1
                allow[:, i] = Q[:, 0] * sgn > width * 0.06
            if k in (nk, head):
                allow[:, i] = Q[:, 1] > cy
        W = np.where(allow, 1.0 / (D + 0.05) ** 4, 0)
        W = np.pad(W, ((0, 0), (0, 0)))
        Wfull = np.zeros((len(Q), len(bones)))
        Wfull[:, body_set] = W / W.sum(1, keepdims=True)
        return finish(name, style, src, Q, Nn, UV, I, bones, Wfull)

    # Walkers: leg columns from the feet, the belly between them, the back above.
    front = feet[:, 1] > 0
    zF, zB = feet[front, 1].mean(), feet[~front, 1].mean() if np.any(~front) else feet[:, 1].mean()
    if feet_n == 2:
        zF = zB = feet[:, 1].mean()
    width = np.ptp(Q[:, 0])
    leg_r = []
    for f in feet:
        m = (Q[:, 1] < 0.06 * H) & (np.linalg.norm(Q[:, [0, 2]] - f, axis=1) < 0.25 * width)
        leg_r.append(np.linalg.norm(Q[m][:, [0, 2]] - f, axis=1).max() if np.any(m) else 0.15 * width)
    strip = (np.abs(Q[:, 0]) < 0.12 * width) & (Q[:, 2] > min(zB, zF) - 0.05 * (zmax - zmin)) & (Q[:, 2] < max(zB, zF) + 0.05 * (zmax - zmin)) & (Q[:, 1] > 0.08 * H)
    belly = np.percentile(Q[strip, 1], 10) if np.any(strip) else 0.4 * H
    def top_at(z):
        m = (np.abs(Q[:, 0]) < 0.15 * width) & (np.abs(Q[:, 2] - z) < 0.06 * (zmax - zmin))
        if style == 'bronto' and z > 0:  # the neck rises from the chest: take the back just behind it
            m &= Q[:, 1] < belly * 2.6
        return Q[m, 1].max() if np.any(m) else belly * 1.8
    topB, topF = top_at(zB), top_at(zF)
    if style == 'cat':  # the curled tail rises over the back legs and the head over the front: the back's lowest top between
        topB = topF = min(top_at(z) for z in np.linspace(zB, zF, 9))
    report.append(f'belly {belly:.2f}, back {topB:.2f} / chest {topF:.2f}')
    print(f'[{name}] belly {belly:.2f}, back {topB:.2f} / chest {topF:.2f}, leg radius {np.round(leg_r, 2).tolist()}')
    midB, midF = (belly + topB) * 0.5, (belly + topF) * 0.5
    if feet_n == 2:
        zB = zF - 0.25 * (zmax - zmin)
        zF = zF + 0.12 * (zmax - zmin)
    hips = bone('Hips', root, [0, midB, zB - 0.1 * (zmax - zmin) * 0.5], [0, (midB + midF) * 0.5, (zB + zF) * 0.5], 0.4 * (topB - belly))
    spine = bone('Spine', hips, bones[hips][3], [0, midF, zF], 0.4 * (topF - belly))
    chest = bone('Chest', spine, [0, midF, zF], [0, midF + 0.05 * H, zF + 0.08 * (zmax - zmin)], 0.4 * (topF - belly))

    def leg_centre(f, y, band):
        m = (np.abs(Q[:, 1] - y) < band) & (np.linalg.norm(Q[:, [0, 2]] - f, axis=1) < 1.6 * leg_r[li])
        return Q[m].mean(0) if np.any(m) else np.array([f[0], y, f[1]])

    for li, f in enumerate(feet):
        is_front = feet_n == 4 and f[1] > 0
        # The hip / shoulder joint sits inside the body, a third of the way from the belly to the back.
        leg_top = belly + 0.32 * ((topF if is_front else topB) - belly)
        sn = 'L' if f[0] < 0 else 'R'  # glTF -x is Unity +x (mirrored): name by the Unity side, +x = right
        sn = 'R' if f[0] < 0 else 'L'
        nm = 'Front' if is_front else 'Back'
        band = 0.03 * H
        knee = leg_centre(f, min(belly * 0.8, leg_top * 0.5), band)
        ankle = leg_centre(f, leg_top * 0.13, band)
        top = leg_centre(f, leg_top * 0.95, band)
        top[1] = leg_top
        paw = np.array([f[0], 0.02, f[1] + 0.3 * leg_r[li]])
        parent = chest if is_front else hips
        u = bone(f'{nm}Upper{sn}', parent, top, knee, leg_r[li])
        lw = bone(f'{nm}Lower{sn}', u, knee, ankle, leg_r[li] * 0.9)
        ft = bone(f'{nm}Foot{sn}', lw, ankle, paw, leg_r[li] * 0.9)
        legs.append((li, [u, lw, ft], [top, knee, ankle, paw], f, leg_top))

    # Neck and head: the front region beyond the chest, above the legs.
    zc = bones[chest][3][2]
    fr = (Q[:, 2] > zc - 0.02 * (zmax - zmin)) & (Q[:, 1] > belly * 0.9)
    js = chain(Q, fr, bones[chest][3], neck_links + 1)
    if style == 'cat':
        # The head sits up above the chest, tilted up; its whiskers reach far past the nose, so the nose is the front-most
        # point on the middle line (whiskers grow from the sides) and the head's start is behind its middle.
        fr = (Q[:, 2] > zF - 0.05 * (zmax - zmin)) & (Q[:, 1] > topF * 1.05)
        hc = np.median(Q[fr], 0)
        # Its head is turned toward the viewer as drawn, so the nose isn't on the middle line: it's the pink patch below the
        # (also pink) inner ears. The whiskers reach well past it, so the farthest point won't do.
        col = tex_colors(os.path.join(src, f'{name}_meshy_basecolor.png'), UV)
        pink = fr & (col[:, 0] > 0.75) & (col[:, 1] > 0.3) & (col[:, 1] < 0.6) & (col[:, 2] > 0.35) & (col[:, 2] < 0.7) & (Q[:, 1] < hc[1] + 0.1 * H)
        nose = Q[pink].mean(0) if pink.sum() > 5 else Q[fr][np.argmax(Q[fr, 2])]
        hb = hc + (hc - nose) * 0.35
        hb[0] = 0
        js = [bones[chest][3], hb, nose]
        print(f'[{name}] head middle {np.round(hc, 2).tolist()}, nose {np.round(nose, 2).tolist()}')
    prev = chest
    for k in range(neck_links):
        r_ = 0.25 if style == 'cat' else 0.35 if style == 'bronto' else 0.45
        prev = bone(f'Neck{k}', prev, js[k], js[k + 1], r_ * (topF - belly) * (1 - 0.1 * k))
        necks.append(prev)
    hd_start, hd_end = js[neck_links], js[neck_links + 1]
    # The head's end: the farthest point of the front region from the chest (the snout / beak).
    pts = Q[fr]
    tip = pts[np.argmax(np.linalg.norm(pts - bones[chest][3], axis=1))] if style != 'cat' else js[2]
    # The cat's head is round and big: its bone is short (middle to nose), so its reach is half the head's width.
    head_r = 0.3 * width if style == 'cat' else 0.5 * np.linalg.norm(tip - hd_start)
    head = bone('Head', prev, hd_start, tip, head_r)
    hvec = (tip - hd_start) / max(np.linalg.norm(tip - hd_start), 1e-6)
    jaw_root = hd_start + hvec * 0.25 * np.linalg.norm(tip - hd_start) - np.array([0, 0.08, 0]) * np.linalg.norm(tip - hd_start)
    jaw = bone('Jaw', head, jaw_root, tip - np.array([0, 0.25, 0]) * np.linalg.norm(tip - hd_start), 0.2 * np.linalg.norm(tip - hd_start))
    # Tail: the back region behind the hips.
    zh = bones[hips][2][2]
    bk = (Q[:, 2] < zh) & (Q[:, 1] > belly * 0.5)
    tj = chain(Q, bk, bones[hips][2], 4)
    tail_mask = None
    if style == 'cat':
        tj, tail_mask = curled_tail(name, Q, I, zh, topB, 4)
    prev = hips
    for k in range(4):
        prev = bone(f'Tail{k}', prev, tj[k], tj[k + 1], 0.45 * (topB - belly) * (1 - 0.18 * k))
        tails.append(prev)

    nb = len(bones)
    nv = len(Q)
    W = np.zeros((nv, nb))
    body_set = [hips, spine, chest] + necks + [head, jaw] + tails
    D = np.stack([seg_dist(Q, bones[k][2], bones[k][3]) / max(bones[k][4], 1e-3) for k in body_set], 1)
    allow = np.ones_like(D, bool)
    head_len = np.linalg.norm(tip - hd_start)
    in_head = np.linalg.norm(Q - (hd_start + tip) * 0.5, axis=1) < 0.75 * head_len
    for i, k in enumerate(body_set):
        if k in tails:
            allow[:, i] = Q[:, 2] < zh + 0.02 * (zmax - zmin)
            if tail_mask is not None:
                allow[:, i] = tail_mask
        if k in necks or k == head:
            # Above the chest's middle only: the chest and shoulders stay with the body when the neck bends.
            allow[:, i] = (Q[:, 2] > zc - 0.2 * (zmax - zmin)) & (Q[:, 1] > midF + 0.15 * (topF - midF))
            if style == 'bronto':  # its neck rises from the chest: anything above the back belongs to the neck
                allow[:, i] |= Q[:, 1] > topB * 1.05
            if style == 'cat':  # the head: everything above the back at the front; the neck: the ruff below the head
                allow[:, i] = (Q[:, 2] > -0.05 * (zmax - zmin)) & (Q[:, 1] > (topF * 1.05 if k == head else midF))
                if k in necks:
                    allow[:, i] &= Q[:, 1] < hd_start[1] - 0.1 * (topF - belly)
        if k == jaw:
            # The lower jaw: in the head, below the line from the head's start to the snout, its front half.
            rel = Q - hd_start
            along = rel @ hvec
            below = (rel[:, 1] - along * hvec[1]) < -0.05 * head_len
            allow[:, i] = in_head & below & (along > 0.2 * head_len) & (style == 'trex')  # only the T. rex chomps
    zmid = bones[spine][2][2]
    # The cat's whiskers: thin strips reaching out past the body's sides at the front; they go with the head.
    whiskers = (np.abs(Q[:, 0]) > 0.42 * width) & (Q[:, 2] > 0) & (Q[:, 1] > belly + 0.8 * (topF - belly))
    for i, k in enumerate(body_set):
        if k == hips:  # the hips (a wide bone) never reach past the middle of the back
            allow[:, i] &= Q[:, 2] < zmid + 0.05 * (zmax - zmin)
        if k in (hips, spine, chest) and style == 'bronto':
            allow[:, i] &= ~((Q[:, 1] > topB * 1.05) & (Q[:, 2] > zc - 0.2 * (zmax - zmin)))
        if k in (hips, spine, chest) and style == 'cat':  # well above the back is the head or the tail
            allow[:, i] &= (Q[:, 1] < topB * 1.12) & ~whiskers
    if style == 'cat':
        allow[whiskers, body_set.index(head)] = True
    Wb =np.where(allow, 1.0 / (D + 0.05) ** 4, 0)
    jaw_i = body_set.index(jaw)
    Wb[:, jaw_i] *= 3.0
    s = Wb.sum(1, keepdims=True)
    s[s == 0] = 1
    Wb /= s
    for i, k in enumerate(body_set):
        W[:, k] = Wb[:, i]

    best = np.zeros(nv)
    which = np.full(nv, -1)
    legW = []
    for li, ids, pts_, f, leg_top in legs:
        ys = np.array([q[1] for q in pts_])[::-1]
        xs = np.array([q[0] for q in pts_])[::-1]
        zs = np.array([q[2] for q in pts_])[::-1]
        a = np.stack([np.interp(Q[:, 1], ys, xs), np.interp(Q[:, 1], ys, zs)], 1)
        hdist = np.linalg.norm(Q[:, [0, 2]] - a, axis=1)
        r = leg_r[li]
        k = smooth(1.5 * r, 1.0 * r, hdist) * smooth(leg_top * 1.1, leg_top * 0.75, Q[:, 1])
        sel = k > best
        best = np.where(sel, k, best)
        which = np.where(sel, li, which)
        Dl = np.stack([seg_dist(Q, bones[i][2], bones[i][3]) for i in ids], 1)
        Wl = 1.0 / (Dl + 0.01) ** 4
        legW.append(Wl / Wl.sum(1, keepdims=True))
    for n_, (li, ids, pts_, f, _lt) in enumerate(legs):
        m = which == li
        W[m] *= (1 - best[m])[:, None]
        for c_, i in enumerate(ids):
            W[m, i] += best[m] * legW[n_][m, c_]
    return finish(name, style, src, Q, Nn, UV, I, bones, W)


def finish(name, style, src, Q, Nn, UV, I, bones, W, keep_soles=False):
    nb = len(bones)
    nv = len(Q)
    W = np.where(np.isfinite(W), W, 0)
    zero = W.sum(1) <= 1e-9
    W[zero, 1] = 1.0  # anything left over follows the hips / body
    idx = np.argsort(-W, 1)[:, :4]
    wt = np.take_along_axis(W, idx, 1)
    wt /= np.maximum(wt.sum(1, keepdims=True), 1e-9)
    T = I.reshape(-1, 3).astype(np.int64)
    e = np.stack([np.linalg.norm(Q[T[:, a]] - Q[T[:, b]], axis=1) for a, b in [(0, 1), (1, 2), (2, 0)]], 1).max(1)
    med = np.median(e)
    stray = e > 8 * med
    if keep_soles:  # Round 20: a biped's flat soles are big triangles on the ground, not flaps
        stray &= Q[T][:, :, 1].max(1) > 0.01 * np.ptp(Q[:, 1])
    print(f'[{name}] {nb} bones; triangles {len(T)}; edge median {med:.3f}, longest {e.max():.3f}; stray (> 8x median): {stray.sum()}')
    T = T[~stray]
    os.makedirs(plots_dir(style), exist_ok=True)
    try:
        import matplotlib
        matplotlib.use('Agg')
        import matplotlib.pyplot as plt
        dom = idx[:, 0]
        pal = np.random.default_rng(3).random((nb, 3)) * 0.8 + 0.1
        fig, axs = plt.subplots(1, 3, figsize=(21, 7))
        for a, (u, v, t, dep) in zip(axs, [(2, 1, 'side', 0), (0, 1, 'front', 2), (0, 2, 'top', 1)]):
            o = np.argsort(Q[:, dep])
            a.scatter(Q[o, u], Q[o, v], s=0.6, c=pal[dom[o]])
            for bn in bones:
                a.plot([bn[2][u], bn[3][u]], [bn[2][v], bn[3][v]], 'k-', lw=2)
            a.set_aspect('equal')
            a.set_title(f'{name} {t}')
        plt.savefig(os.path.join(plots_dir(style), f'{style}_weights.png'), dpi=60)
        plt.close()
    except ImportError:
        pass

    def U(v):
        return np.array([-v[0], v[1], v[2]], np.float32)
    Qu = Q.astype(np.float32).copy()
    Qu[:, 0] *= -1
    Nu = Nn.astype(np.float32).copy()
    Nu[:, 0] *= -1
    Tu = T[:, [0, 2, 1]].astype(np.int32)
    out = out_dir(style)
    os.makedirs(out, exist_ok=True)
    with open(os.path.join(out, f'{style}_rig.bytes'), 'wb') as f:
        f.write(b'BDOG')
        f.write(struct.pack('<iiii', 1, nv, len(Tu), nb))
        for nm, parent, a, t, r in bones:
            nmb = nm.encode()[:31]
            f.write(nmb + b'\0' * (32 - len(nmb)))
            f.write(struct.pack('<i', parent))
            f.write(U(a).tobytes())
            f.write(U(t).tobytes())
        f.write(np.ascontiguousarray(Qu).tobytes())
        f.write(np.ascontiguousarray(Nu).tobytes())
        f.write(np.ascontiguousarray(UV.astype(np.float32)).tobytes())
        f.write(np.ascontiguousarray(idx.astype(np.int32)).tobytes())
        f.write(np.ascontiguousarray(wt.astype(np.float32)).tobytes())
        f.write(np.ascontiguousarray(Tu).tobytes())
    Image.open(os.path.join(src, f'{name}_meshy_basecolor.png')).save(os.path.join(out, f'{style}_basecolor.png'))
    print(f'[{name}] wrote {nv} vertices, {len(Tu)} triangles, {nb} bones, height {Q[:, 1].max():.2f}, length {np.ptp(Q[:, 2]):.2f} -> {os.path.relpath(out, ROOT)}')
    return T


def read_rig(style):
    """Reads a written <style>_rig.bytes back (Unity space): bones {name: (parent, start, end)}, vertices, normals, UVs,
    bone indices / weights, triangles."""
    with open(os.path.join(out_dir(style), f'{style}_rig.bytes'), 'rb') as f:
        b = f.read()
    assert b[:4] == b'BDOG'
    _, nv, nt, nb = struct.unpack_from('<iiii', b, 4)
    o = 20
    bones = {}
    for _ in range(nb):
        nm = b[o:o + 32].split(b'\0')[0].decode()
        parent = struct.unpack_from('<i', b, o + 32)[0]
        a = np.frombuffer(b, np.float32, 3, o + 36)
        t = np.frombuffer(b, np.float32, 3, o + 48)
        bones[nm] = (parent, a.astype(float), t.astype(float))
        o += 60

    def take(dt, n, k):
        nonlocal o
        arr = np.frombuffer(b, dt, n * k, o).reshape(n, k)
        o += arr.nbytes
        return arr
    V, N, UV = take(np.float32, nv, 3), take(np.float32, nv, 3), take(np.float32, nv, 2)
    idx, wt, T = take(np.int32, nv, 4), take(np.float32, nv, 4), take(np.int32, nt, 3)
    return bones, V.astype(float), N.astype(float), UV.astype(float), idx, wt, T


def find_eyes(name, style, n_eyes=2):
    """Finds the two painted eyes on a written rig (for the blink lids) and writes <style>_eyes.json in the rig's own space
    (Unity space, relative to the model root, model units): per eye its centre, outward normal, radius and the lid colour
    (the skin just above it), plus the bone the lids follow. An eye is a patch of near-black (pupil, lid line) touching
    near-white (the white of the eye) on the head: white alone (teeth) or black alone (nostrils, mouth) doesn't count."""
    import json
    from scipy.sparse import coo_matrix
    from scipy.sparse.csgraph import connected_components
    from scipy.spatial import cKDTree
    from rig_biscuit import seg_dist
    bones, V, N, UV, idx, wt, T = read_rig(style)
    tex_path = os.path.join(out_dir(style), f'{style}_basecolor.png')
    a, t = bones['Head'][1], bones['Head'][2]
    H = np.ptp(V[:, 1])
    R = max(1.2 * np.linalg.norm(t - a), 0.15 * H)
    near = seg_dist(V, a, t) < R
    tri = T[near[T].all(1)]
    # Dense samples over the head's triangles (the texture's eyes are far finer than the vertices).
    b = np.random.default_rng(0).dirichlet([1, 1, 1], size=(len(tri), 24))
    S = np.einsum('tkc,tcd->tkd', b, V[tri]).reshape(-1, 3)
    SN = np.einsum('tkc,tcd->tkd', b, N[tri]).reshape(-1, 3)
    C = tex_colors(tex_path, np.einsum('tkc,tcd->tkd', b, UV[tri]).reshape(-1, 2))
    mx, mn = C.max(1), C.min(1)
    dark = mx < 0.13
    white = (mn > 0.82) & (mx - mn < 0.12)
    eye_px = dark | white
    E = np.nonzero(eye_px)[0]
    pairs = cKDTree(S[E]).query_pairs(0.012 * H, output_type='ndarray')
    n, lab = connected_components(coo_matrix((np.ones(len(pairs)), (pairs[:, 0], pairs[:, 1])), shape=(len(E), len(E))), directed=False)
    cands = []
    for k in range(n):
        m = E[lab == k]
        nd, nw = int(dark[m].sum()), int(white[m].sum())
        if nd >= 10 and nw >= 20:
            cands.append((nd + nw, nd, nw, m))
    cands.sort(key=lambda c: -c[0])
    if len(cands) == 1 and n_eyes == 2:
        # Eyes close together (the brontosaurus): their whites touch. Split the patch by its two pupils.
        m = cands[0][3]
        cen, _ = kmeans(S[m][dark[m]], 2)
        side = np.argmin(np.stack([np.linalg.norm(S[m] - c, axis=1) for c in cen], 1), 1)
        print(f'[{name}] one eye patch: split by its two pupils {np.round(cen, 3).tolist()}')
        cands = [(len(m[side == i]), int(dark[m[side == i]].sum()), int(white[m[side == i]].sum()), m[side == i]) for i in range(2)]
    for c in cands[:6]:
        print(f'[{name}] eye candidate: {c[0]} samples ({c[1]} dark, {c[2]} white) at {np.round(S[c[3]].mean(0), 3).tolist()}')
    eyes = []
    for tot, nd, nw, m in cands:
        p = S[m].mean(0)
        r = float(np.percentile(np.linalg.norm(S[m] - p, axis=1), 90))
        if any(np.linalg.norm(p - e['c']) < 1.2 * max(r, e['r']) for e in eyes):  # a second patch of an eye found already
            continue
        eyes.append({'c': p, 'r': r, 'm': m, 'n': tot})
        if len(eyes) == n_eyes:
            break
    if len(eyes) < n_eyes:
        print(f'[{name}] EYES NOT FOUND ({len(eyes)} of {n_eyes})')
    elif min(e['n'] for e in eyes) < 0.3 * max(e['n'] for e in eyes):
        print(f'[{name}] eyes differ a lot in size: {[e["n"] for e in eyes]} samples (check the plot)')
    out = []
    for e in eyes:
        p, r, m = e['c'], e['r'], e['m']
        nrm = SN[m].mean(0)
        nrm /= max(np.linalg.norm(nrm), 1e-9)
        e['nrm'] = nrm
    # Left / right as the creature sees it: across the way the face looks (a turned head can have both eyes at +x).
    face = np.mean([e['nrm'] for e in eyes], 0) if eyes else np.array([0, 0, 1.0])
    right = np.cross([0, 1.0, 0], face)  # Unity: up x forward = right (+x for a face looking along +z)
    mid = np.mean([e['c'] for e in eyes], 0) if eyes else np.zeros(3)
    order = sorted(eyes, key=lambda e: (e['c'] - mid) @ right)
    for e in order:
        p, r, m, nrm = e['c'], e['r'], e['m'], e['nrm']
        # The lid colour: the skin in a ring just around the eye, its upper half (no black or white); its most common
        # colour, so a brow line drawn above the eye (the T. rex's) doesn't tint it.
        d = np.linalg.norm(S - p, axis=1)
        ring = (d > 1.15 * r) & (d < 1.8 * r) & (S[:, 1] > p[1]) & ~eye_px & (np.abs((S - p) @ nrm) < r) & (mx > 0.4)
        if ring.sum() < 10:
            ring = (d < 2.5 * r) & ~eye_px
        q = np.floor(C[ring] * 8).astype(int)
        keys, inv, cnt = np.unique(q, axis=0, return_inverse=True, return_counts=True)
        lid = np.median(C[ring][inv.ravel() == np.argmax(cnt)], 0).astype(float)
        side = 'R' if (p - mid) @ right > 0 else 'L'
        if len(order) == 3 and e is order[1]:
            side = 'M'  # Round 20: the alien's third eye, in the middle of its forehead
        out.append({'side': side, 'centre': np.round(p, 5).tolist(), 'normal': np.round(nrm, 4).tolist(),
                    'radius': round(r, 5), 'lid': np.round(lid, 3).tolist()})
        print(f'[{name}] eye {side}: centre {np.round(p, 3).tolist()}, normal {np.round(nrm, 2).tolist()}, radius {r:.3f}, lid {np.round(lid, 2).tolist()} ({int(ring.sum())} skin samples)')
    with open(os.path.join(out_dir(style), f'{style}_eyes.json'), 'w', encoding='utf-8') as f:
        json.dump({'bone': 'Head', 'units': 'model (as the rig bytes: Unity space, relative to the root)', 'eyes': out}, f, indent=2)
    try:
        import matplotlib
        matplotlib.use('Agg')
        import matplotlib.pyplot as plt
        sub = np.random.default_rng(1).choice(len(S), min(len(S), 60000), replace=False)
        # The face view: looking along the eyes' mean normal (the cat's head is turned).
        fn = np.mean([o['normal'] for o in out], 0) if out else np.array([0, 0, 1.0])
        fn /= max(np.linalg.norm(fn), 1e-9)
        up = np.array([0, 1.0, 0]) - fn[1] * fn
        up /= max(np.linalg.norm(up), 1e-9)
        rt = np.cross(up, fn)
        views = [('side (+x)', lambda P: P[:, [2, 1]], lambda P: P[:, 0]),
                 ('front', lambda P: P[:, [0, 1]], lambda P: P[:, 2]),
                 ('face (along the eye normals)', lambda P: np.c_[P @ rt, P @ up], lambda P: P @ fn)]
        fig, axs = plt.subplots(1, 3, figsize=(21, 7))
        for ax, (title, proj, depth) in zip(axs, views):
            o = np.argsort(depth(S[sub]))
            xy = proj(S[sub][o])
            ax.scatter(xy[:, 0], xy[:, 1], c=C[sub][o], s=2)
            for e in out:
                c2 = proj(np.array([e['centre']]))[0]
                ax.add_patch(plt.Circle(c2, e['radius'], fill=False, color='cyan', lw=2))
                ax.plot(*c2, 'c+')
            ax.set_aspect('equal')
            ax.set_facecolor('#4060a0')
            ax.set_title(f'{name} {title}')
        plt.savefig(os.path.join(plots_dir(style), f'{style}_eyes.png'), dpi=60)
        plt.close()
    except ImportError:
        pass
    return out


# ---------------------------------------------------------------------------------------------------- Round 20: bipeds

def straighten_biped(P, name):
    """Feet at y = 0, centred between them, facing +Z (Meshy's front is the concept's front, +Z): the line between the
    two feet is turned onto x. Returns Q, R, off with Q = P @ R.T - off."""
    y0, H = P[:, 1].min(), np.ptp(P[:, 1])
    low = P[:, 1] < y0 + 0.04 * H
    centres, lab = kmeans(P[low][:, [0, 2]], 2)
    if centres[0, 0] > centres[1, 0]:
        centres = centres[::-1]
    side = centres[1] - centres[0]
    yaw = np.arctan2(-side[1], side[0])  # turns the left-to-right line onto +x
    c, s_ = np.cos(yaw), np.sin(yaw)
    R = np.array([[c, 0, -s_], [0, 1, 0], [s_, 0, c]])
    mid = R @ np.array([centres.mean(0)[0], 0, centres.mean(0)[1]])
    off = np.array([mid[0], y0, mid[2]])
    Q = P @ R.T - off
    print(f'[{name}] feet at x {np.round(centres[:, 0], 3).tolist()}, turned {np.degrees(yaw):.1f} deg; size {np.round(np.ptp(Q, 0), 3).tolist()}')
    return Q, R, off


def rig_biped(name, cfg):
    """A standing biped drawn front-on, arms out (the Lunar Links alien and robot)."""
    import json
    style = cfg['style']
    src = os.path.join(INCOMING, name)
    glb = os.path.join(src, f'{name}_meshy.glb')
    j, P, I, UV = load(glb)
    N = read_normals(glb, j)
    P, I, N, UV = drop_islands(name, P, I, N, UV)
    dome = None
    if style == 'robot':
        from robot_dome import cut
        tex = np.asarray(Image.open(os.path.join(src, f'{name}_meshy_basecolor.png')).convert('RGB')).astype(np.float32) / 255
        P, I, N, UV, dome = cut(P, I, N, UV, tex, name)
    if cfg.get('tail'):
        # Round 22: no feet to stand on: as Meshy made it (facing +Z), centred on the torso, the tail tip at y = 0.
        y0, H0 = P[:, 1].min(), np.ptp(P[:, 1])
        band = (P[:, 1] > y0 + 0.45 * H0) & (P[:, 1] < y0 + 0.6 * H0)
        R = np.eye(3)
        off = np.array([float(np.median(P[band, 0])), y0, float(np.median(P[band, 2]))])
        Q = P - off
        print(f'[{name}] tail (no feet): centred on the torso, size {np.round(np.ptp(Q, 0), 3).tolist()}')
    else:
        Q, R, off = straighten_biped(P, name)
    Nn = N @ R.T
    H = Q[:, 1].max()
    hw = np.abs(Q[:, 0]).max()
    ax = np.abs(Q[:, 0])
    bones = []

    def bone(nm, parent, a, t, r):
        bones.append([nm, parent, np.array(a, float), np.array(t, float), float(r)])
        return len(bones) - 1

    def mid_z(sel):  # the middle of the box (the face's dense detail would pull a median forward)
        return float(0.5 * (Q[sel, 2].min() + Q[sel, 2].max())) if np.any(sel) else 0.0

    yc = cfg['crotch'] * H
    if dome is not None:
        yn = (dome['floorY'] - off[1]) + 0.004  # the head (dome) starts at the collar's top
    else:
        yn = cfg['neck'] * H
    ys = cfg['shoulder_y'] * H
    sx = cfg['shoulder'] * hw
    torso = (ax < sx) & (Q[:, 1] > yc) & (Q[:, 1] < yn)
    zt = mid_z(torso)
    head_sel = Q[:, 1] > yn
    if cfg.get('antenna'):
        head_sel &= Q[:, 1] < cfg['antenna'] * H
    zh = mid_z(head_sel & (ax < 0.3 * hw))
    head_top = Q[head_sel, 1].max()
    body_r = np.percentile(ax[torso], 90)

    root = bone('Root', -1, [0, 0, 0], [0, 0.1 * H, 0], 0.01)
    y_mid = yc + 0.45 * (yn - yc)
    y_chest = yc + 0.75 * (yn - yc)
    hips = bone('Hips', root, [0, yc + 0.08 * (yn - yc), zt], [0, y_mid, zt], body_r)
    spine = bone('Spine', hips, [0, y_mid, zt], [0, y_chest, zt], body_r)
    chest = bone('Chest', spine, [0, y_chest, zt], [0, yn, zt], body_r)
    head = bone('Head', chest, [0, yn, zh], [0, head_top, zh], 0.5 * (head_top - yn))

    # Arms: what's past the shoulder line in the arm band; a chain of three from the shoulder out to the fingertips.
    arms = {}
    for sg in (-1, 1):
        sn = 'R' if sg < 0 else 'L'  # glTF -x is Unity +x (mirrored): +x in Unity is the creature's right
        band = (Q[:, 1] > cfg['arm_lo'] * H) & (Q[:, 1] < cfg['arm_hi'] * H)
        m = band & (sg * Q[:, 0] > sx) & ((sg * Q[:, 0] > sx + cfg['root_w'] * hw) | (Q[:, 1] > cfg['root_lo'] * H))
        near = m & (sg * Q[:, 0] < sx + 0.08 * hw)
        sh = Q[near].mean(0) if np.any(near) else np.array([sg * sx, ys, zt])
        sh[0] = sg * (sx - 0.04 * hw)  # the joint sits just inside the body's edge
        js = chain(Q, m, sh, 3)
        r = 0.6 * np.percentile(np.linalg.norm(Q[near] - sh, axis=1), 90) if np.any(near) else 0.08 * H
        u = bone(f'ArmUpper{sn}', chest, js[0], js[1], r)
        lo = bone(f'ArmLower{sn}', u, js[1], js[2], r * 0.9)
        hd = bone(f'Hand{sn}', lo, js[2], js[3], r * 0.9)
        arms[sg] = (m, [u, lo, hd], js)
    # Legs: below the crotch, each side; hip, knee, ankle from slice centres; the foot forward to the toe.
    legs = {}
    tail = None
    if cfg.get('tail'):
        # Round 22: a three-link tail from the hips down through everything below the waist (it curls to one side).
        m = Q[:, 1] < yc + 0.04 * H
        top = m & (np.abs(Q[:, 1] - yc) < 0.03 * H)
        start = Q[top].mean(0) if np.any(top) else np.array([0, yc, zt])
        js = chain(Q, m, start, 3)
        r = 0.5 * np.ptp(Q[top, 0]) if np.any(top) else 0.12 * H
        t0 = bone('Tail0', hips, js[0], js[1], r)
        t1 = bone('Tail1', t0, js[1], js[2], r * 0.7)
        t2 = bone('Tail2', t1, js[2], js[3], r * 0.45)
        tail = (m, [t0, t1, t2])
    for sg in ((-1, 1) if not cfg.get('tail') else ()):
        sn = 'R' if sg < 0 else 'L'
        m = (sg * Q[:, 0] > 0) & (Q[:, 1] < yc + 0.04 * H)

        def ctr(y, band=0.025 * H):
            sel = m & (np.abs(Q[:, 1] - y) < band)
            return Q[sel].mean(0) if np.any(sel) else np.array([sg * 0.3 * hw, y, 0.0])
        hip = ctr(yc)
        hip[1] = yc + 0.05 * H
        knee = ctr(0.55 * yc)
        ankle = ctr(0.22 * yc)
        foot = m & (Q[:, 1] < 0.15 * yc)
        toe = np.array([ankle[0], 0.05 * yc, Q[foot, 2].max() if np.any(foot) else ankle[2] + 0.1 * H])
        sl = m & (np.abs(Q[:, 1] - 0.55 * yc) < 0.03 * H)
        r = 0.5 * np.ptp(Q[sl, 0]) if np.any(sl) else 0.1 * H
        u = bone(f'LegUpper{sn}', hips, hip, knee, r)
        lw = bone(f'LegLower{sn}', u, knee, ankle, r)
        ft = bone(f'Foot{sn}', lw, ankle, toe, r)
        legs[sg] = (m, [u, lw, ft])
    # The alien's antennae: two links each from the head's top; the robot's side antennae: from its ear balls.
    ants = {}
    if cfg.get('antenna'):
        ya = cfg['antenna'] * H
        for sg in (-1, 1):
            sn = 'R' if sg < 0 else 'L'
            m = (Q[:, 1] > ya) & (sg * Q[:, 0] > 0.05 * hw)
            base = Q[m][np.argsort(Q[m, 1])[:20]].mean(0)
            js = chain(Q, m, base, 2)
            a0 = bone(f'Antenna{sn}0', head, js[0], js[1], 0.05 * H)
            a1 = bone(f'Antenna{sn}1', a0, js[1], js[2], 0.06 * H)
            ants[sg] = (m, [a0, a1])
    if dome is not None:
        for e in dome['ears']:
            c = np.array(e['centre']) @ R.T - off
            sg = 1 if c[0] > 0 else -1
            sn = 'R' if sg < 0 else 'L'
            m = (sg * Q[:, 0] > abs(c[0]) - 0.2 * e['radius']) & (Q[:, 1] > c[1] + 0.6 * e['radius']) & (Q[:, 1] < head_top)
            tip = Q[m][np.argmax(Q[m, 1])] if np.any(m) else c + np.array([0, 0.2 * H, 0])
            a0 = bone(f'Antenna{sn}0', head, c, tip, 0.03 * H)
            ants[sg] = (m, [a0])
    # The robot's claws: the lower jaw of each C (below the hand's middle) hinges at the back of the C.
    pincers, hands = {}, {}
    if style == 'robot':
        for sg in (-1, 1):
            m, ids, js = arms[sg]
            hand = m & (np.linalg.norm(Q - js[2], axis=1) > 0.3 * np.linalg.norm(js[3] - js[2])) & (sg * Q[:, 0] > sg * js[2][0] - 0.02 * hw)
            hc = Q[hand].mean(0)
            back = hand & (sg * Q[:, 0] > sg * hc[0] + 0.25 * np.ptp(Q[hand, 0]))
            hinge = np.array([Q[back, 0].mean() if np.any(back) else hc[0], hc[1], hc[2]])
            lowj = hand & (Q[:, 1] < hc[1] - 0.01 * H)
            jaw_tip = Q[lowj][np.argmin(Q[lowj, 1])] if np.any(lowj) else hc - np.array([0, 0.05 * H, 0])
            sn = 'R' if sg < 0 else 'L'
            pc = bone(f'Pincer{sn}', ids[2], hinge, jaw_tip, 0.05 * H)
            pincers[sg] = (lowj, pc, hc)
            hands[sg] = hand
            print(f'[{name}] claw {sn}: {int(hand.sum())} vertices, hinge {np.round(hinge, 3).tolist()}, lower jaw {int(lowj.sum())}')

    # Weights: the trunk by distance along Hips / Spine / Chest, then each part blended in by its own membership.
    nb, nv = len(bones), len(Q)
    W = np.zeros((nv, nb))

    def inv(ids, pw=4, eps=0.01):
        D = np.stack([seg_dist(Q, bones[k][2], bones[k][3]) for k in ids], 1)
        w = 1.0 / (D + eps * H) ** pw
        return w / w.sum(1, keepdims=True)

    trunk = [hips, spine, chest]
    W[:, trunk] = inv(trunk)
    hb = 0.004 * H if style == 'robot' else 0.035 * H  # the robot is mechanical: a sharp collar; the alien a soft neck
    hk = smooth(yn - hb, yn + hb, Q[:, 1])
    W *= (1 - hk)[:, None]
    W[:, head] += hk
    for sg, (m, ids) in legs.items():
        # Shared across the middle (the crotch), so a step bends it instead of tearing it.
        k = smooth(yc + 0.05 * H, yc - 0.01 * H, Q[:, 1]) * smooth(-0.06 * hw, 0.06 * hw, sg * Q[:, 0])
        W *= (1 - k)[:, None]
        W[:, ids] += k[:, None] * inv(ids)
    if tail is not None:
        m, ids = tail
        k = smooth(yc + 0.06 * H, yc - 0.02 * H, Q[:, 1])
        W *= (1 - k)[:, None]
        W[:, ids] += k[:, None] * inv(ids, pw=3)
    for sg, (m, ids, js) in arms.items():
        # Near the body the arm starts at its armhole (root_lo up); farther out the hand hangs lower (arm_lo up).
        lo = np.where(sg * Q[:, 0] > sx + cfg['root_w'] * hw, cfg['arm_lo'], cfg['root_lo']) * H
        band = smooth(lo - 0.015 * H, lo, Q[:, 1]) * smooth(cfg['arm_hi'] * H + 0.02 * H, cfg['arm_hi'] * H, Q[:, 1])
        k = smooth(sx - 0.03 * hw, sx + 0.03 * hw, sg * Q[:, 0]) * band
        W *= (1 - k)[:, None]
        W[:, ids] += k[:, None] * inv(ids)
    for sg, (m, ids) in ants.items():
        if style == 'alien':
            k = smooth(cfg['antenna'] * H - 0.01 * H, cfg['antenna'] * H + 0.02 * H, Q[:, 1]) * (sg * Q[:, 0] > 0.05 * hw)
        else:
            k = m.astype(float)
        W *= (1 - k)[:, None]
        W[:, ids] += k[:, None] * (inv(ids) if len(ids) > 1 else 1.0)
    for sg, (lowj, pc, hc) in pincers.items():
        hd = arms[sg][1][2]
        # Only what the hand carries; blended over the middle of the C's back so it bends there instead of tearing.
        k = smooth(hc[1] + 0.005 * H, hc[1] - 0.025 * H, Q[:, 1]) * hands[sg] * W[:, hd]
        W[:, hd] -= k
        W[:, pc] += k

    T = finish(name, style, src, Q, Nn, UV, I, bones, W, keep_soles=True)
    out = out_dir(style)
    if dome is not None:
        def U(v):
            v = np.asarray(v, float) @ R.T - off
            return [round(-float(v[0]), 5), round(float(v[1]), 5), round(float(v[2]), 5)]
        hd0 = bones[head][2]
        info = {
            'units': 'model (as the rig bytes: Unity space, relative to the root); the game scales with the model',
            'bone': 'Head', 'headStart': [round(-float(hd0[0]), 5), round(float(hd0[1]), 5), round(float(hd0[2]), 5)],
            'centre': U(dome['centre']), 'radius': round(dome['radius'], 5), 'radiusY': round(dome['radiusY'], 5),
            'floorY': round(dome['floorY'] - off[1], 5), 'floorRadius': round(dome['floorRadius'], 5),
            'ornamentY': round(dome['ornamentY'] - off[1], 5),
            'faceMin': U(dome['faceMin']), 'faceMax': U(dome['faceMax']),
            'ears': [{'centre': U(e['centre']), 'radius': round(e['radius'], 5)} for e in dome['ears']],
        }
        fm, fx = info['faceMin'], info['faceMax']
        info['faceMin'], info['faceMax'] = [min(fm[0], fx[0]), fm[1], fm[2]], [max(fm[0], fx[0]), fx[1], fx[2]]
        with open(os.path.join(out, f'{style}_dome.json'), 'w', encoding='utf-8') as f:
            json.dump(info, f, indent=2)
        print(f'[{name}] dome -> {style}_dome.json: centre {info["centre"]}, radius {info["radius"]} / {info["radiusY"]}, floor {info["floorY"]}')
    pose_check(name, style, Q, T, UV, bones, W)


def pose_check(name, style, Q, T, UV, bones, W):
    """Renders the rig in a few poses (linear blend skinning, as the game does) -> Builds/screens/lunar/<style>_poses.png,
    and reports the most stretched triangle in each pose (a stray face shows as a big ratio)."""
    from mesh_preview import render, sheet
    names = [b[0] for b in bones]
    idx = {n: i for i, n in enumerate(names)}
    tex = np.asarray(Image.open(os.path.join(out_dir(style), f'{style}_basecolor.png')).convert('RGB')).astype(np.float32) / 255

    def rot(axis, deg):
        a = np.radians(deg)
        x, y, z = np.asarray(axis, float) / np.linalg.norm(axis)
        c, s_ = np.cos(a), np.sin(a)
        return np.array([[c + x * x * (1 - c), x * y * (1 - c) - z * s_, x * z * (1 - c) + y * s_],
                         [y * x * (1 - c) + z * s_, c + y * y * (1 - c), y * z * (1 - c) - x * s_],
                         [z * x * (1 - c) - y * s_, z * y * (1 - c) + x * s_, c + z * z * (1 - c)]])

    def pose(rots):
        G = [None] * len(bones)
        for i, (nm, parent, a, t, r) in enumerate(bones):
            Rl = rots.get(nm, np.eye(3))
            if parent < 0:
                G[i] = (Rl, a.copy())
            else:
                Rp, tp = G[parent]
                pa = bones[parent][2]
                G[i] = (Rp @ Rl, Rp @ (a - pa) + tp)
        out = np.zeros_like(Q)
        for i, (nm, parent, a, t, r) in enumerate(bones):
            w = W[:, i]
            if not np.any(w > 1e-6):
                continue
            Rg, tg = G[i]
            out += w[:, None] * ((Q - a) @ Rg.T + tg)
        return out

    def side(sn):
        return 1 if sn == 'L' else -1  # glTF x: the creature's left is +x here
    poses = [('bind', {}),
             ('arms up', {**{f'ArmUpper{s}': rot([0, 0, 1], side(s) * 70) for s in 'LR'}, **{f'ArmLower{s}': rot([0, 0, 1], side(s) * 30) for s in 'LR'}}),
             ('step', {'LegUpperL': rot([1, 0, 0], -35), 'LegLowerL': rot([1, 0, 0], 30), 'LegUpperR': rot([1, 0, 0], 25), 'Hips': rot([0, 1, 0], 8)}),
             ('head turn', {'Head': rot([0, 1, 0], 40) @ rot([1, 0, 0], 10), 'Chest': rot([0, 1, 0], 8)}),
             ('tail swish', {'Tail0': rot([0, 0, 1], 20), 'Tail1': rot([0, 0, 1], 25), 'Tail2': rot([0, 0, 1], 30)}),
             ('wave', {'ArmUpperL': rot([0, 0, 1], 100), 'ArmLowerL': rot([0, 0, 1], 35), 'HandL': rot([0, 0, 1], 20),
                       'AntennaL0': rot([0, 0, 1], 25), 'AntennaR0': rot([0, 0, 1], -20), 'AntennaL1': rot([1, 0, 0], 30),
                       'PincerR': rot([0, 0, 1], 35), 'PincerL': rot([0, 0, 1], -35)})]
    ims, labels = [], []
    e0 = np.stack([np.linalg.norm(Q[T[:, a]] - Q[T[:, b]], axis=1) for a, b in [(0, 1), (1, 2), (2, 0)]], 1).max(1)
    H = np.ptp(Q[:, 1])
    cen = np.array([0, H * 0.5, 0])
    for label, rots in poses:
        rots = {k: v for k, v in rots.items() if k in idx}
        V = pose(rots)
        e = np.stack([np.linalg.norm(V[T[:, a]] - V[T[:, b]], axis=1) for a, b in [(0, 1), (1, 2), (2, 0)]], 1).max(1)
        ratio = e / np.maximum(e0, 1e-9)
        worst = np.argsort(-ratio)[:3]
        where = '; '.join(f'x{ratio[w]:.1f} at {np.round(Q[T[w]].mean(0), 2).tolist()} ({names[int(np.argmax(W[T[w][0]]))]})' for w in worst)
        print(f'[{name}] pose {label}: most stretched triangle x{ratio.max():.2f}; over x2: {int((ratio > 2).sum())}; worst {where}')
        for y in (0, 35):
            ims.append(render(V, T, UV, tex, yaw=y, pitch=8, size=300, centre=cen, extent=1.25 * H))
            labels.append(f'{label} {y}')
    os.makedirs(plots_dir(style), exist_ok=True)
    path = os.path.join(plots_dir(style), f'{style}_poses.png')
    sheet(ims, labels, path, cols=len(poses))
    print(f'[{name}] poses -> {os.path.relpath(path, ROOT)}')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--only')
    ap.add_argument('--eyes', action='store_true', help='only find the eyes on the rigs already written (no re-rig)')
    a = ap.parse_args()
    for name, cfg in {}.items():   # ShiftStorm: no bipeds (Tee'd Off's BIPEDS)
        if a.only and a.only.lower() not in (name.lower(), cfg['style']):
            continue  # Round 20: --only Alien / Robot (or alien / robot)
        if not a.eyes:
            rig_biped(name, cfg)
        if cfg['eyes']:
            find_eyes(name, cfg['style'], cfg['eyes'])
    for name, (style, feet, neck) in DINOS.items():
        if a.only and a.only.lower() != name.lower():
            continue
        if not a.eyes:
            rig(name, style, feet, neck)
        try:
            find_eyes(name, style)
        except Exception as e:   # ShiftStorm: the eyes (blink lids) are not used here
            print(f'[{name}] eyes not found ({type(e).__name__}); not needed in ShiftStorm')


if __name__ == '__main__':
    main()
